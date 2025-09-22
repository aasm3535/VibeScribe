using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VibeScribe.ViewModels;

namespace VibeScribe.Views
{
    public sealed partial class TranscriptionView : Page
    {
        public TranscriptionViewModel ViewModel { get; }

        public TranscriptionView()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<TranscriptionViewModel>();
        }
    }
}