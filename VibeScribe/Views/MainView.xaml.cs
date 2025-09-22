using Microsoft.UI.Xaml.Controls;

namespace VibeScribe.Views
{
    public sealed partial class MainView : Page
    {
        public MainView()
        {
            this.InitializeComponent();
            TranscriptionViewFrame.Navigate(typeof(TranscriptionView));
            TranscriptionDetailViewFrame.Navigate(typeof(TranscriptionDetailView));
        }
    }
}