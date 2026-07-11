using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AutoBrowser.ViewModels;
using Serilog;

namespace AutoBrowser;

public partial class App
{
    private async Task CheckAndPromptReRegister()
    {
        Log.Information("CheckAndPromptReRegister called");

        var currentPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            Log.Debug("Cannot determine current process path, skipping re-register check");
            return;
        }

        // Skip check when running via dotnet CLI (development mode)
        var exeName = Path.GetFileNameWithoutExtension(currentPath);
        if (!exeName.Equals("AutoBrowser", StringComparison.OrdinalIgnoreCase))
        {
            Log.Debug("Running via {ProcessName}, skipping re-register check", exeName);
            return;
        }

        var needsReRegister = false;
        var registrationType = string.Empty;

        // Check autobrowser:// protocol registration
        if (_protocolService.IsProtocolRegistered())
        {
            var registeredPath = _protocolService.GetRegisteredPath();
            Log.Debug("Protocol registration: RegisteredPath={Registered}, CurrentPath={Current}",
                registeredPath, currentPath);

            if (!string.IsNullOrEmpty(registeredPath)
                && !registeredPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
            {
                needsReRegister = true;
                registrationType = "autobrowser:// protocol handler";
            }
        }

        // Check default browser registration
        if (_defaultBrowserService.IsDefaultBrowser())
        {
            var registeredPath = _defaultBrowserService.GetRegisteredPath();
            Log.Debug("Default browser registration: RegisteredPath={Registered}, CurrentPath={Current}",
                registeredPath, currentPath);

            if (!string.IsNullOrEmpty(registeredPath)
                && !registeredPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
            {
                needsReRegister = true;
                registrationType = string.IsNullOrEmpty(registrationType)
                    ? "system default browser"
                    : registrationType + " and system default browser";
            }
        }

        if (needsReRegister)
        {
            Log.Information("App path has changed, prompting user to re-register: {Type}", registrationType);

            var oldProtocolPath = _protocolService.IsProtocolRegistered() ? _protocolService.GetRegisteredPath() : null;
            var oldDefaultPath = _defaultBrowserService.IsDefaultBrowser() ? _defaultBrowserService.GetRegisteredPath() : null;
            var oldPath = oldProtocolPath ?? oldDefaultPath ?? "(unknown)";

            var contentPanel = new System.Windows.Controls.StackPanel();

            var descText = new System.Windows.Controls.TextBlock
            {
                Text = $"AutoBrowser has been moved to a new location, but the {registrationType} still points to the old path.",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(0, 0, 0, 16),
                FontSize = 14,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorPrimaryBrush")
            };
            contentPanel.Children.Add(descText);

            var cardBorder = new System.Windows.Controls.Border
            {
                Background = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("ControlFillColorDefaultBrush"),
                BorderBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("ControlElevationBorderBrush"),
                BorderThickness = new System.Windows.Thickness(1),
                CornerRadius = new System.Windows.CornerRadius(6),
                Padding = new System.Windows.Thickness(12),
                Margin = new System.Windows.Thickness(0, 0, 0, 16)
            };

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(8) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var oldLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Old Path:",
                FontWeight = System.Windows.FontWeights.SemiBold,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorSecondaryBrush")
            };
            System.Windows.Controls.Grid.SetColumn(oldLabel, 0);
            System.Windows.Controls.Grid.SetRow(oldLabel, 0);
            grid.Children.Add(oldLabel);

            var oldPathText = new System.Windows.Controls.TextBlock
            {
                Text = oldPath,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorTertiaryBrush")
            };
            System.Windows.Controls.Grid.SetColumn(oldPathText, 1);
            System.Windows.Controls.Grid.SetRow(oldPathText, 0);
            grid.Children.Add(oldPathText);

            var newLabel = new System.Windows.Controls.TextBlock
            {
                Text = "New Path:",
                FontWeight = System.Windows.FontWeights.SemiBold,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorSecondaryBrush")
            };
            System.Windows.Controls.Grid.SetColumn(newLabel, 0);
            System.Windows.Controls.Grid.SetRow(newLabel, 2);
            grid.Children.Add(newLabel);

            var newPathText = new System.Windows.Controls.TextBlock
            {
                Text = currentPath,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorTertiaryBrush")
            };
            System.Windows.Controls.Grid.SetColumn(newPathText, 1);
            System.Windows.Controls.Grid.SetRow(newPathText, 2);
            grid.Children.Add(newPathText);

            cardBorder.Child = grid;
            contentPanel.Children.Add(cardBorder);

            var questionText = new System.Windows.Controls.TextBlock
            {
                Text = "Would you like to re-register now?",
                FontWeight = System.Windows.FontWeights.SemiBold,
                FontSize = 14,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextFillColorPrimaryBrush")
            };
            contentPanel.Children.Add(questionText);

            var dialog = new Wpf.Ui.Controls.MessageBox
            {
                Title = "AutoBrowser — Path Changed",
                Content = contentPanel,
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                IsCloseButtonEnabled = false,
                Width = 550,
                MinWidth = 550
            };
            dialog.Owner = _mainWindow;
            var result = await dialog.ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                if (registrationType.Contains("protocol"))
                {
                    _protocolService.UnregisterProtocolHandler();
                    _protocolService.RegisterProtocolHandler();
                    Log.Information("Protocol handler re-registered");
                }
                if (registrationType.Contains("default browser"))
                {
                    _defaultBrowserService.UnregisterAsDefaultBrowser();
                    _defaultBrowserService.RegisterAsDefaultBrowser();
                    Log.Information("Default browser registration updated");
                }

                ShowNotification("AutoBrowser", "Registration updated successfully.");
            }
            else
            {
                Log.Debug("User declined re-registration");
            }
        }
        else
        {
            Log.Debug("Registration paths are current, no re-registration needed");
        }
    }
}
