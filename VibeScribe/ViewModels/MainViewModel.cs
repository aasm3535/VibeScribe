using System.Windows.Input;
using Windows.Media.Capture;
using Windows.Storage;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Media.MediaProperties;
using VibeScribe.Models;
using VibeScribe.Services;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace VibeScribe.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly TranscriptionService _transcriptionService;
        private MediaCapture? _mediaCapture;
        private StorageFile? _recordingFile;
        private bool _isRecording;
        private static readonly HttpClient httpClient = new();
        private string _recordButtonText = "New Record";
        private string _recordIconGlyph = "\uE722";

        public MainViewModel()
        {
            _transcriptionService = App.Services.GetRequiredService<TranscriptionService>();
            NewRecordCommand = new RelayCommand(async _ => await NewRecord());
            _ = InitializeMediaCaptureAsync();
        }

        public ICommand NewRecordCommand { get; }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                _isRecording = value;
                OnPropertyChanged();
                RecordButtonText = value ? "Stop" : "New Record";
                RecordIconGlyph = value ? "\uE71A" : "\uE722";
            }
        }

        public string RecordButtonText
        {
            get => _recordButtonText;
            set
            {
                _recordButtonText = value;
                OnPropertyChanged();
            }
        }

        public string RecordIconGlyph
        {
            get => _recordIconGlyph;
            set
            {
                _recordIconGlyph = value;
                OnPropertyChanged();
            }
        }

        private async Task InitializeMediaCaptureAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio,
                    MediaCategory = MediaCategory.Speech
                });
            }
            catch
            {
                // Handle error
            }
        }

        private async Task NewRecord()
        {
            if (IsRecording)
            {
                await StopRecording();
            }
            else
            {
                await StartRecording();
            }
        }

        private async Task StartRecording()
        {
            if (_mediaCapture == null) return;

            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                _recordingFile = await localFolder.CreateFileAsync("recording.mp3", CreationCollisionOption.GenerateUniqueName);
                var encodingProfile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
                await _mediaCapture.StartRecordToStorageFileAsync(encodingProfile, _recordingFile);
                IsRecording = true;
            }
            catch
            {
                // Handle error
            }
        }

        private async Task StopRecording()
        {
            if (_mediaCapture == null) return;

            try
            {
                await _mediaCapture.StopRecordAsync();
                IsRecording = false;

                if (_recordingFile != null)
                {
                    await SendToServerAsync(_recordingFile);
                }
            }
            catch
            {
                // Handle error
            }
        }

        private async Task SendToServerAsync(StorageFile audioFile)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(await audioFile.OpenStreamForReadAsync());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mp3");
                content.Add(fileContent, "audio", audioFile.Name);

                var response = await httpClient.PostAsync("http://localhost:5000/transcribe", content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(jsonString);
                    var text = document.RootElement.GetProperty("text").GetString() ?? "No transcription";

                    var newTranscription = new Transcription
                    {
                        Title = $"Recording - {DateTime.Now:g}",
                        Text = text,
                        AudioFilePath = audioFile.Path,
                        Timestamp = DateTime.Now
                    };

                    await _transcriptionService.SaveTranscriptionAsync(newTranscription);
                }
            }
            catch
            {
                // Handle error
            }
        }
    }
}