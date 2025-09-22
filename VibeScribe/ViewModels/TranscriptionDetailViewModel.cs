using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Windows.Input;
using VibeScribe.Models;
using VibeScribe.Services;
using Windows.Storage;
using System.Text.Json;
using VibeScribe.Views;
using Microsoft.UI.Xaml;

namespace VibeScribe.ViewModels
{
    public class TranscriptionDetailViewModel : ViewModelBase
    {
        private Transcription? _transcription;
        private readonly Messenger _messenger;
        private readonly TranscriptionService _transcriptionService;
        private bool _isDeleteConfirmationOpen;

        public Transcription? Transcription
        {
            get => _transcription;
            set
            {
                _transcription = value;
                OnPropertyChanged();
            }
        }

        public bool IsDeleteConfirmationOpen
        {
            get => _isDeleteConfirmationOpen;
            set
            {
                _isDeleteConfirmationOpen = value;
                OnPropertyChanged();
            }
        }

        public ICommand DeleteCommand { get; }
        public ICommand ShowDeleteConfirmationCommand { get; }
        public ICommand PopOutCommand { get; }

        public TranscriptionDetailViewModel()
        {
            _messenger = App.Services.GetRequiredService<Messenger>();
            _transcriptionService = App.Services.GetRequiredService<TranscriptionService>();
            _messenger.Register<TranscriptionSelectedMessage>(m => Transcription = m.Transcription);
            DeleteCommand = new RelayCommand(async _ => await DeleteTranscription());
            ShowDeleteConfirmationCommand = new RelayCommand(_ => IsDeleteConfirmationOpen = true);
            PopOutCommand = new RelayCommand(_ => PopOut());
        }

        private void PopOut()
        {
            var window = new PopOutWindow();
            window.Activate();
            _messenger.Send(new TranscriptionSelectedMessage(Transcription));
        }

        private async Task DeleteTranscription()
        {
            if (Transcription == null) return;

            var transcriptions = await _transcriptionService.GetTranscriptionsAsync();
            var toDelete = transcriptions.Find(t => t.Timestamp == Transcription.Timestamp);
            if (toDelete != null)
            {
                transcriptions.Remove(toDelete);
                if (toDelete.AudioFilePath != null)
                {
                    var file = await StorageFile.GetFileFromPathAsync(toDelete.AudioFilePath);
                    await file.DeleteAsync();
                }

                var appDataFolder = ApplicationData.Current.LocalFolder;
                var transcriptionsFile = await appDataFolder.CreateFileAsync("transcriptions.json", CreationCollisionOption.ReplaceExisting);
                var json = JsonSerializer.Serialize(transcriptions);
                await FileIO.WriteTextAsync(transcriptionsFile, json);

                _messenger.Send(new TranscriptionDeletedMessage());
            }
            IsDeleteConfirmationOpen = false;
        }
    }
}