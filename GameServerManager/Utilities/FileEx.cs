using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.IO.Compression;
using System.Text;

namespace GameServerManager.Utilities
{
    /// <summary>
    /// File Extra
    /// </summary>
    public static class FileEx
    {
        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Task DeleteAsync(string path)
        {
            return TaskEx.Run(() => File.Delete(path));
        }

        /// <summary>
        /// Delete file if exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Task DeleteIfExistsAsync(string path)
        {
            if (File.Exists(path))
            {
                return TaskEx.Run(() => File.Delete(path));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Extract .zip file
        /// </summary>
        /// <param name="sourceArchiveFileName"></param>
        /// <param name="destinationDirectoryName"></param>
        /// <returns></returns>
        public static Task ExtractZip(string sourceArchiveFileName, string destinationDirectoryName)
        {
            return ExtractZip(sourceArchiveFileName, destinationDirectoryName, false);
        }

        /// <summary>
        /// Extract .zip file
        /// </summary>
        /// <param name="sourceArchiveFileName"></param>
        /// <param name="destinationDirectoryName"></param>
        /// <param name="overwriteFiles"></param>
        /// <returns></returns>
        public static Task ExtractZip(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
        {
            return TaskEx.Run(() => ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, overwriteFiles));
        }

        /// <summary>
        /// Extract .tar.gz file
        /// </summary>
        /// <param name="sourceArchiveFileName"></param>
        /// <param name="destinationDirectoryName"></param>
        /// <returns></returns>
        public static Task ExtractTarGZ(string sourceArchiveFileName, string destinationDirectoryName)
        {
            return TaskEx.Run(() =>
            {
                using Stream fileStream = File.OpenRead(sourceArchiveFileName);
                using Stream gzipStream = new GZipInputStream(fileStream);
                using TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.Default);
                tarArchive.ExtractContents(destinationDirectoryName);
            });
        }

        public static async Task<List<string>> ReadAllLines(string path)
        {
            List<string> lines = new();

            using Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(stream);

            // StreamReader bugs, this fix
            string? line = await reader.ReadLineAsync();

            while (line != null)
            {
                lines.Add(line);
                line = await reader.ReadLineAsync();
            }

            return lines;
        }

        public static async Task<string> ReadAllText(string path)
        {
            using Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(stream);

            // need await here, otherwise StreamReader will be disposed
            return await reader.ReadToEndAsync();
        }
    }
}
