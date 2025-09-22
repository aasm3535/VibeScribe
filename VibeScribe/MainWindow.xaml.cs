using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VibeScribe
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private Grid? _rootGrid;
        private MediaCapture? _mediaCapture;
        private StorageFile? _recordingFile;
        private bool _isRecording = false;
        private static readonly HttpClient httpClient = new HttpClient();
        private string _tempDir;

        public MainWindow()
        {
            InitializeComponent();

            _rootGrid = Content as Grid;

            AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 500));

            //AppWindow.Move(new Windows.Graphics.PointInt32(50, 50));

            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

            _tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vibescribe", "Temp");
            Directory.CreateDirectory(_tempDir);

            _ = InitializeMediaCaptureAsync();
        }

        private async Task InitializeMediaCaptureAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio,
                    AudioDeviceId = "",
                    MediaCategory = MediaCategory.Speech
                });
            }
            catch (Exception ex)
            {
                // Handle initialization error, e.g., no microphone
                System.Diagnostics.Debug.WriteLine($"MediaCapture initialization failed: {ex.Message}");
                SetStatusText("Microphone initialization failed");
            }
        }

        private void SetStatusText(string text)
        {
            if (_rootGrid?.FindName("StatusTextBlock") is TextBlock tb)
            {
                tb.Text = text;
            }
        }

        private async void NewRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaCapture == null)
            {
                SetStatusText("Microphone not initialized");
                return;
            }

            if (!_isRecording)
            {
                try
                {
                    _recordingFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("recording.mp3", CreationCollisionOption.GenerateUniqueName);
                    var encodingProfile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
                    await _mediaCapture.StartRecordToStorageFileAsync(encodingProfile, _recordingFile);
                    _isRecording = true;
                    // Update UI for recording state
                    RecordingIcon.Glyph = "\uE7C8"; // Recording icon
                    RecordingTextBlock.Text = "Stop Recording";
                    SetStatusText("Recording... (tap to stop)");
                }
                catch (Exception ex)
                {
                    SetStatusText($"Start recording failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Start recording failed: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    await _mediaCapture.StopRecordAsync();
                    _isRecording = false;
                    // Update UI for stopped state
                    RecordingIcon.Glyph = "\uEA3F"; // New Recording icon
                    RecordingTextBlock.Text = "New Recording";
                    SetStatusText("Processing transcription...");

                    // Copy recording to Temp folder
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string audioFileName = $"recording_{timestamp}.mp3";
                    string audioPath = Path.Combine(_tempDir, audioFileName);
                    string txtFileName = $"transcript_{timestamp}.txt";
                    string txtPath = Path.Combine(_tempDir, txtFileName);

                    using (var input = File.OpenRead(_recordingFile.Path))
                    {
                        using (var output = File.Create(audioPath))
                        {
                            input.CopyTo(output);
                        }
                    }

                    await SendToServerAsync(audioPath, txtPath);
                    await _recordingFile.DeleteAsync();

                    SetStatusText($"Saved: {audioFileName} and {txtFileName}");
                }
                catch (Exception ex)
                {
                    SetStatusText($"Stop recording failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stop recording failed: {ex.Message}");
                }
            }
        }

        private async Task SendToServerAsync(string audioPath, string txtPath)
        {
            try
            {
                string serverUrl = "http://localhost:5000/transcribe";
                
                using var stream = File.OpenRead(audioPath);
                byte[] audioBytes = new byte[stream.Length];
                await stream.ReadAsync(audioBytes, 0, (int)stream.Length);

                using var content = new MultipartFormDataContent();
                using var byteContent = new ByteArrayContent(audioBytes);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                content.Add(byteContent, "audio", Path.GetFileName(audioPath));

                var response = await httpClient.PostAsync(serverUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    // Предполагаем, что сервер возвращает JSON с полем "text"
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    string transcript = doc.RootElement.GetProperty("text").GetString() ?? "No transcript";
                    File.WriteAllText(txtPath, transcript);
                    SetStatusText($"Transcript saved to file");
                }
                else
                {
                    SetStatusText("Transcription failed: " + response.StatusCode);
                    File.WriteAllText(txtPath, "Transcription failed");
                }
            }
            catch (Exception ex)
            {
                SetStatusText($"Send to server failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Send to server failed: {ex.Message}");
                // Try to write error to txt
                try
                {
                    File.WriteAllText(txtPath, $"Error: {ex.Message}");
                }
                catch { }
            }
        }

        public void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            AppNotification notification = new AppNotificationBuilder()
                .AddText("Началась запись")
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }
}