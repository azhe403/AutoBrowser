using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AutoBrowser.Models;
using AutoBrowser.Services;

namespace AutoBrowser.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IConfigurationService _configService;
    private RoutingRule? _selectedRule;

    public ObservableCollection<RoutingRule> Rules { get; } = [];

    public RoutingRule? SelectedRule
    {
        get => _selectedRule;
        set { _selectedRule = value; OnPropertyChanged(); }
    }

    public bool IsProtocolRegistered
    {
        get => _configService.IsProtocolRegistered();
        set
        {
            if (value)
                _configService.RegisterProtocolHandler();
            else
                _configService.UnregisterProtocolHandler();
            OnPropertyChanged();
        }
    }

    public bool IsDefaultBrowser
    {
        get => _configService.IsDefaultBrowser();
        set
        {
            if (value)
            {
                var ok = _configService.RegisterAsDefaultBrowser();
                if (ok)
                    System.Windows.MessageBox.Show(
                        "Now set AutoBrowser as default in:\nSettings → Default Apps → AutoBrowser",
                        "Default Browser", System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                else
                    System.Windows.MessageBox.Show("Failed to register as default browser.",
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            else
            {
                _configService.UnregisterAsDefaultBrowser();
            }
            OnPropertyChanged();
        }
    }

    public ICommand AddRuleCommand { get; }
    public ICommand EditRuleCommand { get; }
    public ICommand DeleteRuleCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand LaunchUrlCommand { get; }

    public MainViewModel()
    {
        _configService = new ConfigurationService();
        LoadRules();

        AddRuleCommand = new RelayCommand(_ => AddRule());
        EditRuleCommand = new RelayCommand(_ => EditRule(), _ => SelectedRule is not null);
        DeleteRuleCommand = new RelayCommand(_ => DeleteRule(), _ => SelectedRule is not null);
        MoveUpCommand = new RelayCommand(_ => MoveUp(), _ => SelectedRule is not null);
        MoveDownCommand = new RelayCommand(_ => MoveDown(), _ => SelectedRule is not null);
        LaunchUrlCommand = new RelayCommand(_ => LaunchUrl());
    }

    private void LoadRules()
    {
        Rules.Clear();
        foreach (var rule in _configService.LoadRules())
            Rules.Add(rule);
    }

    public void SaveRules()
    {
        _configService.SaveRules([..Rules]);
    }

    private void AddRule()
    {
        var dialog = new RuleDialog();
        if (dialog.ShowDialog() == true)
        {
            Rules.Add(dialog.Rule);
            SelectedRule = dialog.Rule;
            SaveRules();
        }
    }

    private void EditRule()
    {
        if (SelectedRule is null) return;

        var index = Rules.IndexOf(SelectedRule);
        var dialog = new RuleDialog(SelectedRule);
        if (dialog.ShowDialog() == true)
        {
            Rules[index] = dialog.Rule;
            SelectedRule = dialog.Rule;
            SaveRules();
        }
    }

    private void DeleteRule()
    {
        if (SelectedRule is null) return;
        Rules.Remove(SelectedRule);
        SaveRules();
    }

    private void MoveUp()
    {
        MoveRule(-1);
    }

    private void MoveDown()
    {
        MoveRule(1);
    }

    private void MoveRule(int direction)
    {
        if (SelectedRule is null) return;

        var index = Rules.IndexOf(SelectedRule);
        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= Rules.Count) return;

        Rules.Move(index, newIndex);
        SaveRules();
    }

    private void LaunchUrl()
    {
        var dialog = new InputDialog("Test URL", "Enter URL to test routing:", "https://");
        dialog.ShowDialog();
        var url = dialog.Result;
        if (string.IsNullOrWhiteSpace(url)) return;

        var interceptor = new UrlInterceptorService(_configService);
        if (!interceptor.TryRoute(url))
        {
            System.Windows.MessageBox.Show("No matching rule found — opened in default browser.",
                "AutoBrowser", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
