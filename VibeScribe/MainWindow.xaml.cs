using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using VibeScribe.ViewModels;
using VibeScribe.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Input;

namespace VibeScribe
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = App.Services.GetRequiredService<MainViewModel>();
            CreateUI();
        }

        private void CreateUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // New Record Button
            var newRecordButton = new Button();
            newRecordButton.Command = ViewModel.NewRecordCommand;
            newRecordButton.HorizontalAlignment = HorizontalAlignment.Right;
            newRecordButton.Margin = new Microsoft.UI.Xaml.Thickness(10);
            Grid.SetRow(newRecordButton, 0);

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var fontIcon = new FontIcon { Glyph = ViewModel.RecordIconGlyph };
            var textBlock = new TextBlock { Text = ViewModel.RecordButtonText };
            stackPanel.Children.Add(fontIcon);
            stackPanel.Children.Add(textBlock);
            newRecordButton.Content = stackPanel;

            var toolTip = new ToolTip { Content = "Start a new recording" };
            ToolTipService.SetToolTip(newRecordButton, toolTip);

            newRecordButton.Click += NewRecordButton_Click;

            // Main Frame
            var mainFrame = new Frame();
            mainFrame.Name = "MainFrame";
            Grid.SetRow(mainFrame, 1);

            // InfoBar
            var infoBar = new InfoBar
            {
                IsOpen = false,
                Severity = InfoBarSeverity.Informational,
                Title = "Transcription Ready",
                Message = "Your new transcription is ready.",
                Name = "TranscriptionInfoBar"
            };
            Grid.SetRow(infoBar, 2);

            grid.Children.Add(newRecordButton);
            grid.Children.Add(mainFrame);
            grid.Children.Add(infoBar);

            this.Content = grid;

            if (this.Content is FrameworkElement content)
            {
                content.Loaded += OnLoaded;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.Content is Grid grid)
            {
                var mainFrame = grid.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(typeof(MainView));
                }
            }
        }

        private async void NewRecordButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RecordingWarningDialog();
            dialog.XamlRoot = (this.Content as FrameworkElement)?.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.NewRecordCommand.Execute(null);
            }
        }
    }
}