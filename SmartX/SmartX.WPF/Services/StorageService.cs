using System.IO;
using System.Text.Json;

namespace SmartX.Services
{
    public class StorageService
    {
        private readonly string _dataFolder;

        public StorageService()
        {
            _dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FairShare");

            Directory.CreateDirectory(_dataFolder);
        }

        public void Save<T>(string fileName, List<T> data)
        {
            string path = Path.Combine(_dataFolder, fileName);

            string json = JsonSerializer.Serialize(data,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(
                path,
                json);
        }

        public List<T> Load<T>(string fileName)
        {
            string path =
                Path.Combine(
                    _dataFolder,
                    fileName);

            if (!File.Exists(path)) return new List<T>();
            
            string json =File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(json)) return new List<T>();
            
            try
            {
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch (JsonException)
            {
                //damaged JSON file
                File.WriteAllText(path, "[]");

                return new List<T>();
            }
        }
    }
}