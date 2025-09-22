using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using VibeScribe.ViewModels;

namespace VibeScribe.Views
{
    public sealed partial class TranscriptionDetailView : Page
    {
        public TranscriptionDetailViewModel ViewModel { get; }

        public TranscriptionDetailView()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<TranscriptionDetailViewModel>();
        }
    }
}