using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using VibeScribe.ViewModels;
using VibeScribe.Views;
using Microsoft.Extensions.DependencyInjection;

namespace VibeScribe
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<MainViewModel>();
            if (this.Content is FrameworkElement content)
            {
                content.Loaded += OnLoaded;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.Content is Grid grid && grid.FindName("MainFrame") is Frame frame)
            {
                frame.Navigate(typeof(MainView));
            }
        }

        private async void NewRecordButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RecordingWarningDialog();
            dialog.XamlRoot = this.Content.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.NewRecordCommand.Execute(null);
            }
        }
    }
}