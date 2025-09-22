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
        private Grid? _contentGrid;
        private ColumnDefinition? _leftColumn;
        private Rectangle? _columnSplitter;
        private bool _isDragging = false;
        private double _startX;
        private const double MinLeftWidth = 200;
        private MediaCapture? _mediaCapture;
        private StorageFile? _recordingFile;
        private bool _isRecording = false;
        private static readonly HttpClient httpClient = new HttpClient();
        private string _tempDir;

        public MainWindow()
        {
            InitializeComponent();

            _rootGrid = Content as Grid;
            if (_rootGrid != null)
            {
                _contentGrid = _rootGrid.FindName("MainContentGrid") as Grid;
                if (_contentGrid != null)
                {
                    _leftColumn = _contentGrid.ColumnDefinitions[0];
                    _columnSplitter = _contentGrid.FindName("ColumnSplitter") as Rectangle;

                    // Set initial width if needed
                    if (_leftColumn != null)
                    {
                        _leftColumn.Width = new GridLength(300);
                    }
                }
            }

            AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 500));

            //AppWindow.Move(new Windows.Graphics.PointInt32(50, 50));

            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

            _tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VibeScribe", "Temp");
            Directory.CreateDirectory(_tempDir);

            _ = InitializeMediaCaptureAsync();

            // Load existing records into ListView
            LoadRecords();
        }

        private void LoadRecords()
        {
            if (RecordsListView == null) return;

            RecordsListView.Items.Clear();

            string[] transcriptFiles = Directory.GetFiles(_tempDir, "transcript_*.txt");
            foreach (string filePath in transcriptFiles)
            {
                string fileName = Path.GetFileName(filePath);
                var item = new ListViewItem
                {
                    Content = new TextBlock { Text = fileName },
                    Margin = new Microsoft.UI.Xaml.Thickness(5)
                };
                RecordsListView.Items.Add(item);
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
            if (StatusTextBlock != null)
            {
                StatusTextBlock.Text = text;
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
                    if (RecordingIcon != null) RecordingIcon.Glyph = "\\uE7C8"; // Recording icon
                    if (RecordingTextBlock != null) RecordingTextBlock.Text = "Stop Recording";
                    SetStatusText("Recording... (click to stop)");
                }
                catch (Exception ex)
                {
                    SetStatusText($"Failed to start recording: {ex.Message}");
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
                    if (RecordingIcon != null) RecordingIcon.Glyph = "\\uEA3F"; // New Recording icon
                    if (RecordingTextBlock != null) RecordingTextBlock.Text = "New Recording";
                    SetStatusText("Processing transcription...");

                    // Copy recording to Temp folder
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string audioFileName = $"recording_{{timestamp}}.mp3";
                    string audioPath = Path.Combine(_tempDir, audioFileName);
                    string txtFileName = $"transcript_{{timestamp}}.txt";
                    string txtPath = Path.Combine(_tempDir, txtFileName);

                    if (_recordingFile != null)
                    {
                        using (var input = File.OpenRead(_recordingFile.Path))
                        {
                            using (var output = File.Create(audioPath))
                            {
                                input.CopyTo(output);
                            }
                        }

                        await SendToServerAsync(audioPath, txtPath);
                        await _recordingFile.DeleteAsync();
                    }

                    // Reload records list
                    LoadRecords();

                    SetStatusText($"Saved: {{audioFileName}} and {{txtFileName}}");
                }
                catch (Exception ex)
                {
                    SetStatusText($"Failed to stop recording: {ex.Message}");
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
                    // Assume server returns JSON with "text" field
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    string transcript = doc.RootElement.GetProperty("text").GetString() ?? "No transcript";
                    File.WriteAllText(txtPath, transcript);
                    SetStatusText("Transcript saved to file");
                }
                else
                {
                    SetStatusText("Transcription failed: " + response.StatusCode);
                    File.WriteAllText(txtPath, "Transcription failed");
                }
            }
            catch (Exception ex)
            {
                SetStatusText($"Server send error: {ex.Message}");
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
                .AddText("Recording started")
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }

        private void RecordsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ListViewItem item && item.Content is TextBlock textBlock)
            {
                string fileName = textBlock.Text;
                if (fileName.StartsWith("transcript_"))
                {
                    string txtPath = Path.Combine(_tempDir, fileName);
                    if (File.Exists(txtPath))
                    {
                        string transcript = File.ReadAllText(txtPath);
                        // Find corresponding audio file for title
                        string audioFileName = fileName.Replace("transcript_", "recording_");
                        string title = Path.GetFileNameWithoutExtension(audioFileName);
                        if (RecordingTitleTextBlock != null)
                        {
                            RecordingTitleTextBlock.Text = title;
                        }
                        if (TranscriptTitleTextBlock != null)
                        {
                            TranscriptTitleTextBlock.Text = title;
                        }
                        if (TranscriptTextBlock != null)
                        {
                            TranscriptTextBlock.Text = transcript;
                        }
                        SetStatusText($"Loaded: {fileName}");
                    }
                }
            }
        }

        // Splitter Event Handlers
        private void ColumnSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Optional: Change cursor to resize horizontal
            // Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeWestEast, 0);
        }

        private void ColumnSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Optional: Reset cursor
            // Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
        }

        private void ColumnSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_columnSplitter != null && _contentGrid != null)
            {
                _columnSplitter.CapturePointer(e.Pointer);
                _startX = e.GetCurrentPoint(_contentGrid).Position.X;
                _isDragging = true;
                e.Handled = true;
            }
        }

        private void ColumnSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging && _leftColumn != null && _contentGrid != null)
            {
                var currentPoint = e.GetCurrentPoint(_contentGrid);
                double deltaX = currentPoint.Position.X - _startX;
                double currentWidth = _leftColumn.ActualWidth;
                double newWidth = currentWidth + deltaX;
                newWidth = Math.Max(MinLeftWidth, newWidth);
                // Optional: Max width to leave space for right panel
                double maxWidth = _contentGrid.ActualWidth - 205; // splitter 5 + min right 200
                newWidth = Math.Min(newWidth, maxWidth);
                _leftColumn.Width = new GridLength(newWidth);
                _startX = currentPoint.Position.X;
                e.Handled = true;
            }
        }

        private void ColumnSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_columnSplitter != null && _isDragging)
            {
                _columnSplitter.ReleasePointerCapture(e.Pointer);
                _isDragging = false;
                e.Handled = true;
            }
        }
    }
}