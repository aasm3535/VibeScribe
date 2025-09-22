using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VibeScribe.Models;
using Windows.Storage;

namespace VibeScribe.Services
{
    public class TranscriptionService
    {
        private const string TranscriptionsFileName = "transcriptions.json";
        private static readonly StorageFolder AppDataFolder = ApplicationData.Current.LocalFolder;

        public async Task<List<Transcription>> GetTranscriptionsAsync()
        {
            try
            {
                var file = await AppDataFolder.GetFileAsync(TranscriptionsFileName);
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<List<Transcription>>(json) ?? new List<Transcription>();
            }
            catch (FileNotFoundException)
            {
                return new List<Transcription>();
            }
        }

        public async Task SaveTranscriptionAsync(Transcription transcription)
        {
            var transcriptions = await GetTranscriptionsAsync();
            transcriptions.Add(transcription);
            var file = await AppDataFolder.CreateFileAsync(TranscriptionsFileName, CreationCollisionOption.ReplaceExisting);
            var json = JsonSerializer.Serialize(transcriptions);
            await FileIO.WriteTextAsync(file, json);
        }
    }
}