using Microsoft.UI.Xaml;
using VibeScribe.Views;

namespace VibeScribe.Views
{
    public sealed partial class PopOutWindow : Window
    {
        public PopOutWindow()
        {
            this.InitializeComponent();
            PopOutFrame.Navigate(typeof(TranscriptionDetailView));
        }
    }
}