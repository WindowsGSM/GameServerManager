using GameServerManager.Attributes;
using GameServerManager.Services;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static GameServerManager.Services.ProgramDataService.Cache;

namespace GameServerManager.Utilities
{
    public class JsonFile<T>
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public JsonFile(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string GetPath()
        {
            Directory.CreateDirectory(Path);

            return System.IO.Path.Combine(Path, $"{Name}.json");
        }

        public bool TryRead([NotNullWhen(true)] out T? data)
        {
            string path = GetPath();
            data = File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) : default;

            return data != null;
        }

        public void Write(T data)
        {
            string path = GetPath();
            string contents = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            Directory.CreateDirectory(Path);
            File.WriteAllText(path, contents);
        }
    }
}
