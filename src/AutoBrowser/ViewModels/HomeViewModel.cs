using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoBrowser.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IRuleService _ruleService;
    private readonly IDefaultBrowserService _defaultBrowserService;
    private readonly ISettingsService _settingsService;
    private readonly UpdateService _updateService = new();

    public ObservableCollection<RoutingRule> Rules { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRule))]
    [NotifyCanExecuteChangedFor(nameof(EditRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    private RoutingRule? _selectedRule;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckForUpdate))]
    private bool _isCheckingUpdate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheckForUpdate))]
    private bool _isDownloadingUpdate;

    [ObservableProperty]
    private string _updateStatus = "";

    public bool HasSelectedRule => SelectedRule is not null;
    public bool CanCheckForUpdate => !IsCheckingUpdate && !IsDownloadingUpdate;

    public HomeViewModel(
        IRuleService ruleService,
        IDefaultBrowserService defaultBrowserService,
        ISettingsService settingsService)
    {
        _ruleService = ruleService;
        _defaultBrowserService = defaultBrowserService;
        _settingsService = settingsService;

        LoadRules();

        Rules.CollectionChanged += Rules_CollectionChanged;
        foreach (var rule in Rules)
        {
            rule.PropertyChanged += Rule_PropertyChanged;
        }
    }

    private void Rules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (RoutingRule rule in e.OldItems)
                rule.PropertyChanged -= Rule_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (RoutingRule rule in e.NewItems)
                rule.PropertyChanged += Rule_PropertyChanged;
        }
    }

    private void Rule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveRules();
    }

    private void LoadRules()
    {
        Rules.Clear();
        foreach (var rule in _ruleService.LoadRules())
            Rules.Add(rule);
    }

    private void SaveRules()
    {
        _ruleService.SaveRules([..Rules]);
    }

    [RelayCommand]
    private void AddRule()
    {
        var dialog = new RuleEditorView();
        if (dialog.ShowDialog() == true)
        {
            Rules.Add(dialog.Rule);
            SelectedRule = dialog.Rule;
            SaveRules();
            Status = $"Rule \"{dialog.Rule.Name}\" added";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void EditRule()
    {
        if (SelectedRule is null) return;
        var index = Rules.IndexOf(SelectedRule);
        var dialog = new RuleEditorView(SelectedRule);
        if (dialog.ShowDialog() == true)
        {
            Rules[index] = dialog.Rule;
            SelectedRule = dialog.Rule;
            SaveRules();
            Status = $"Rule \"{dialog.Rule.Name}\" updated";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void DeleteRule()
    {
        if (SelectedRule is null) return;
        var name = SelectedRule.Name;
        Rules.Remove(SelectedRule);
        SaveRules();
        Status = $"Rule \"{name}\" deleted";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void MoveUp() => MoveRule(-1);

    [RelayCommand(CanExecute = nameof(HasSelectedRule))]
    private void MoveDown() => MoveRule(1);

    private void MoveRule(int direction)
    {
        if (SelectedRule is null) return;
        var index = Rules.IndexOf(SelectedRule);
        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= Rules.Count) return;

        Rules.Move(index, newIndex);
        SaveRules();
        Status = $"Rule \"{SelectedRule.Name}\" moved";
    }

    [RelayCommand]
    private void LaunchUrl()
    {
        var dialog = new RuleTesterView("Test URL", "Enter URL to test routing:");
        dialog.ShowDialog();
        var url = dialog.Result;
        if (string.IsNullOrWhiteSpace(url)) return;

        var settings = _settingsService.LoadSettings();
        var interceptor = new UrlInterceptorService(_ruleService, _defaultBrowserService);
        var browser = interceptor.TryRoute(url, settings.FallbackBrowserPath);
        if (browser is not null)
            Status = $"Routed via {browser}: {url}";
        else
        {
            Status = $"No match: {url}";
            ShowNotification("AutoBrowser", $"No rule matched and no fallback browser configured.\n{url}");
        }
    }

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        if (IsCheckingUpdate || IsDownloadingUpdate) return;
        IsCheckingUpdate = true;
        UpdateStatus = "Checking for updates...";
        Status = UpdateStatus;
        Log.Information("Manual update check starting");

        try
        {
            var release = await _updateService.CheckForUpdateAsync();
            if (release is null)
            {
                UpdateStatus = "No release info available (no releases or offline).";
                Status = UpdateStatus;
                Log.Debug("Manual update check: no release info");
                return;
            }

            if (!release.IsNewer)
            {
                UpdateStatus = $"You're up to date (v{release.Version}).";
                Status = UpdateStatus;
                Log.Debug("Manual update check: up to date (v{Version})", release.Version);
                return;
            }

            Log.Debug("Manual update check: v{Version} available", release.Version);
            await ShowUpdateDialogAsync(release);
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Update failed: {ex.Message}";
            Status = UpdateStatus;
            Log.Error(ex, "Manual update check failed");
        }
        finally
        {
            IsCheckingUpdate = false;
            IsDownloadingUpdate = false;
        }
    }

    private async Task ShowUpdateDialogAsync(ReleaseInfo release)
    {
        var dialog = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Update Available",
            Content = $"Version {release.Version} is available.\n\nCurrent: {typeof(UpdateService).Assembly.GetName().Version}\n\nDownload and install?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            Width = 500,
            MinWidth = 500
        };
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        var result = await dialog.ShowDialogAsync();

        Log.Information("Update dialog result: {Result} for v{Version}", result, release.Version);
        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) return;

        IsDownloadingUpdate = true;
        UpdateStatus = "Downloading update...";
        Status = UpdateStatus;

        var progress = new Progress<double>(p =>
        {
            var pct = (int)(p * 100);
            UpdateStatus = $"Downloading... {pct}%";
            Status = UpdateStatus;
        });

        await _updateService.DownloadAndUpdateAsync(release, progress);
    }

    private static void ShowNotification(string title, string message)
    {
        try
        {
            using var icon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""),
                Visible = true
            };
            icon.ShowBalloonTip(3000, title, message, System.Windows.Forms.ToolTipIcon.Warning);
        }
        catch { }
    }
}