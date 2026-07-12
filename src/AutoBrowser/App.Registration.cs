using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        var registeredDefaultPath = _defaultBrowserService.GetRegisteredPath();
        if (!string.IsNullOrEmpty(registeredDefaultPath))
        {
            Log.Debug("Default browser registration: RegisteredPath={Registered}, CurrentPath={Current}",
                registeredDefaultPath, currentPath);

            if (!registeredDefaultPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
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

            var oldProtocolPath = _protocolService.GetRegisteredPath();
            var oldDefaultPath = _defaultBrowserService.GetRegisteredPath();
            var oldPath = !string.IsNullOrEmpty(oldProtocolPath) ? oldProtocolPath : (!string.IsNullOrEmpty(oldDefaultPath) ? oldDefaultPath : "(unknown)");

            var contentPanel = new StackPanel();

            var descText = new TextBlock
            {
                Text = $"AutoBrowser has been moved to a new location, but the {registrationType} still points to the old path.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16),
                FontSize = 14,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush")
            };
            contentPanel.Children.Add(descText);

            var cardBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("ControlFillColorDefaultBrush"),
                BorderBrush = (Brush)Application.Current.FindResource("ControlElevationBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 16)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var oldLabel = new TextBlock
            {
                Text = "Old Path:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush")
            };
            Grid.SetColumn(oldLabel, 0);
            Grid.SetRow(oldLabel, 0);
            grid.Children.Add(oldLabel);

            var oldPathText = new TextBlock
            {
                Text = oldPath,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorTertiaryBrush")
            };
            Grid.SetColumn(oldPathText, 1);
            Grid.SetRow(oldPathText, 0);
            grid.Children.Add(oldPathText);

            var newLabel = new TextBlock
            {
                Text = "New Path:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush")
            };
            Grid.SetColumn(newLabel, 0);
            Grid.SetRow(newLabel, 2);
            grid.Children.Add(newLabel);

            var newPathText = new TextBlock
            {
                Text = currentPath,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorTertiaryBrush")
            };
            Grid.SetColumn(newPathText, 1);
            Grid.SetRow(newPathText, 2);
            grid.Children.Add(newPathText);

            cardBorder.Child = grid;
            contentPanel.Children.Add(cardBorder);

            var questionText = new TextBlock
            {
                Text = "Would you like to re-register now?",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush")
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
        
        // When the MessageBox closes, WPF may drop Window focus, causing ExtendsContentIntoTitleBar 
        // dragging to fail until user clicks the client area. We force focus back to the MainWindow 
        // to restore standard Windows drag behavior.
        if (_mainWindow != null && _mainWindow.IsLoaded)
        {
            _mainWindow.Focus();
        }

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
