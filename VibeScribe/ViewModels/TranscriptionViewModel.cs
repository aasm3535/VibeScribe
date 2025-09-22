using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VibeScribe.Models;
using VibeScribe.Services;

namespace VibeScribe.ViewModels
{
    public class TranscriptionViewModel : ViewModelBase
    {
        private readonly TranscriptionService _transcriptionService;
        private readonly Messenger _messenger;
        public ObservableCollection<Transcription> Transcriptions { get; } = new();

        private Transcription? _selectedTranscription;
        public Transcription? SelectedTranscription
        {
            get => _selectedTranscription;
            set
            {
                _selectedTranscription = value;
                OnPropertyChanged();
                if (_selectedTranscription != null)
                {
                    _messenger.Send(new TranscriptionSelectedMessage(_selectedTranscription));
                }
            }
        }

        public TranscriptionViewModel()
        {
            _transcriptionService = App.Services.GetRequiredService<TranscriptionService>();
            _messenger = App.Services.GetRequiredService<Messenger>();
            _messenger.Register<TranscriptionDeletedMessage>(async _ => await LoadTranscriptionsAsync());
            _ = LoadTranscriptionsAsync();
        }

        private async Task LoadTranscriptionsAsync()
        {
            Transcriptions.Clear();
            var transcriptions = await _transcriptionService.GetTranscriptionsAsync();
            foreach (var transcription in transcriptions)
            {
                Transcriptions.Add(transcription);
            }
        }
    }
}