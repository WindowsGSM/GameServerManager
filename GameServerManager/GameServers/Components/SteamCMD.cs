using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GameServerManager.GameServers.Configs;
using GameServerManager.Services;
using GameServerManager.Utilities;
using static GameServerManager.Services.GameServerService;
using System.Text.Json;
using static GameServerManager.Services.ContentVersionService;
using System.Xml.Linq;

namespace GameServerManager.GameServers.Components
{
    public static class SteamCMD
    {
        public class Branch
        {
            [JsonPropertyName("buildid")]
            public string BuildId { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("pwdrequired")]
            public string PwdRequired { get; set; } = string.Empty;

            [JsonPropertyName("timeupdated")]
            public string TimeUpdated { get; set; } = string.Empty;

            public bool PasswordRequired => PwdRequired == "1";
        }

        public static Dictionary<string, Dictionary<string, Branch>> Branches { get; set; } = new();

        /// <summary>
        /// Represents errors that occur when build-id mismatch
        /// </summary>
        public class BuildIdMismatchException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the BuildIdMismatchException class.
            /// </summary>
            /// <param name="message">build id</param>
            public BuildIdMismatchException(string message) : base(message) { }
        }

        /// <summary>
        /// SteamCMD start path
        /// </summary>
        public static readonly string FileName = Path.Combine(ProgramDataService.ProgramDataPath, "steamcmd", "steamcmd.exe");

        /// <summary>
        /// Get SteamCMD Command-Line Parameter
        /// </summary>
        /// <param name="gameServer"></param>
        /// <returns></returns>
        public static async Task<string> GetParameter(IGameServer gameServer)
        {
            StringBuilder @string = new();
            @string.Append($"+force_install_dir \"{gameServer.Config.Basic.Directory}\" ");

            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;
            @string.Append($"+login {(steamCMD.Username == "anonymous" ? "anonymous" : $"\"{steamCMD.Username}\" \"{steamCMD.Password}\"")} ");

            if (!string.IsNullOrWhiteSpace(steamCMD.Secret))
            {
                string code = await GenerateSteamGuardCode(steamCMD.Secret);
                @string.Append($"\"{code}\" ");
            }

            if (steamCMD.AppId == "90")
            {
                @string.Append($"+app_set_config \"{steamCMD.AppId}\" mod \"{steamCMD.Game}\" ");
            }

            // Install 4 more times if hlds.exe (steamCMD.AppId = 90)
            int count = steamCMD.AppId == "90" ? 4 : 1;

            for (int i = 0; i < count; i++)
            {
                @string.Append($"+app_update \"{steamCMD.AppId}\" ");

                if (!string.IsNullOrWhiteSpace(steamCMD.BetaName))
                {
                    @string.Append($"-beta \"{steamCMD.BetaName}\" ");

                    if (!string.IsNullOrWhiteSpace(steamCMD.BetaPassword))
                    {
                        @string.Append($"-betapassword \"{steamCMD.BetaPassword}\" ");
                    }
                }

                if ((gameServer.Status == Status.Installing && steamCMD.ValidateOnInstall) || (gameServer.Status == Status.Updating && steamCMD.ValidateOnUpdate))
                {
                    @string.Append("validate ");
                }
            }

            @string.Append("+quit");

            return @string.ToString();
        }

        /// <summary>
        /// Start steamcmd.exe
        /// </summary>
        /// <param name="gameServer"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task Start(IGameServer gameServer, string version)
        {
            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;

            string directory = Path.GetDirectoryName(steamCMD.Path)!;
            Directory.CreateDirectory(directory);

            if (!File.Exists(steamCMD.Path))
            {
                using HttpClient httpClient = new();
                using HttpResponseMessage response = await httpClient.GetAsync("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip");
                response.EnsureSuccessStatusCode();

                string zipPath = Path.Combine(directory, "steamcmd.zip");

                using (FileStream fs = new(zipPath, FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }

                await FileEx.ExtractZip(zipPath, directory, true);
                await FileEx.DeleteAsync(zipPath);
            }

            string parameter = await GetParameter(gameServer);

            if (steamCMD.ConsoleMode == "Pseudo Console")
            {
                gameServer.Process.UsePseudoConsole(new()
                {
                    WorkingDirectory = directory,
                    FileName = steamCMD.Path,
                    Arguments = parameter,
                });
            }
            else if (steamCMD.ConsoleMode == "Redirect Standard Input/Output")
            {
                gameServer.Process.UseRedirect(new()
                {
                    WorkingDirectory = directory,
                    FileName = steamCMD.Path,
                    Arguments = parameter,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
            }
            else
            {
                gameServer.Process.UseWindowed(new()
                {
                    WorkingDirectory = directory,
                    FileName = steamCMD.Path,
                    Arguments = parameter,
                });
            }

            await gameServer.Process.Start();

            // Wait until steamcmd.exe exit
            await gameServer.Process.WaitForExit(-1);

            // Exit Code 7 seems also ok
            if (gameServer.Process.ExitCode != 0 && gameServer.Process.ExitCode != 7)
            {
                throw new Exception($"SteamCMD error: Exit Code {gameServer.Process.ExitCode}");
            }

            string buildId = await GetLocalBuildId(gameServer);

            if (buildId != version)
            {
                throw new BuildIdMismatchException(buildId);
            }
        }

        /// <summary>
        /// Get Local Build Id from appmanifest.acf
        /// </summary>
        /// <param name="gameServer"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> GetLocalBuildId(IGameServer gameServer)
        {
            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;

            string content = await File.ReadAllTextAsync(Path.Combine(gameServer.Config.Basic.Directory, "steamapps", $"appmanifest_{steamCMD.AppId}.acf"));
            Regex regex = new("\"buildid\"\\s+\"(\\S*)\"");
            MatchCollection matches = regex.Matches(content);

            if (matches.Count <= 0)
            {
                throw new Exception("Could not find the local build id");
            }

            return matches[0].Groups[1].Value;
        }

        /// <summary>
        /// Get Remote Build Id
        /// </summary>
        /// <param name="gameServer"></param>
        /// <returns></returns>
        public static string GetRemoteBuildId(IGameServer gameServer)
        {
            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;

            if (Branches.TryGetValue(steamCMD.AppId, out Dictionary<string, Branch>? branches))
            {
                string name = string.IsNullOrWhiteSpace(steamCMD.BetaName) ? branches.First().Key : steamCMD.BetaName;

                if (branches.ContainsKey(name))
                {
                    return branches[name].BuildId;
                }
            }

            return "Unknown";
        }

        public static IContentVersion GetContentVersion(IGameServer gameServer)
        {
            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;

            if (Branches.TryGetValue(steamCMD.AppId, out Dictionary<string, Branch>? branches))
            {
                string name = string.IsNullOrWhiteSpace(steamCMD.BetaName) ? branches.First().Key : steamCMD.BetaName;

                if (branches.ContainsKey(name))
                {
                    return new ContentVersion()
                    {
                        Versions = new()
                        {
                            branches[name].BuildId
                        },
                        Branch = name
                    };
                }
            }

            return new ContentVersion();
        }

        public static async Task FetchBranches()
        {
            using HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.GetAsync("https://raw.githubusercontent.com/WindowsGSM/SteamAppInfo/main/branches.json");
            response.EnsureSuccessStatusCode();

            Branches = (await response.Content.ReadFromJsonAsync<Dictionary<string, Dictionary<string, Branch>>>())!;
        }

        public static async Task<string> GenerateSteamGuardCode(string secret)
        {
            return GenerateSteamGuardCodeForTime(secret, await GetSteamTime());
        }

        /// <summary>
        /// https://github.com/geel9/SteamAuth/blob/96ca6af3eb03d1a45f7fbff78851f055ba47c0d4/SteamAuth/SteamGuardAccount.cs#L85
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private static string GenerateSteamGuardCodeForTime(string secret, long time)
        {
            string sharedSecretUnescaped = Regex.Unescape(secret);
            byte[] sharedSecretArray = Convert.FromBase64String(sharedSecretUnescaped);
            byte[] timeArray = new byte[8];

            time /= 30L;

            for (int i = 8; i > 0; i--)
            {
                timeArray[i - 1] = (byte)time;
                time >>= 8;
            }

            HMACSHA1 hmacGenerator = new()
            {
                Key = sharedSecretArray
            };

            byte[] hashedData = hmacGenerator.ComputeHash(timeArray);
            byte[] codeArray = new byte[5];
            byte[] steamGuardCodeTranslations = "23456789BCDFGHJKMNPQRTVWXY"u8.ToArray();

            // May throw error
            byte b = (byte)(hashedData[19] & 0xF);
            int codePoint = (hashedData[b] & 0x7F) << 24 | (hashedData[b + 1] & 0xFF) << 16 | (hashedData[b + 2] & 0xFF) << 8 | (hashedData[b + 3] & 0xFF);

            for (int i = 0; i < 5; ++i)
            {
                codeArray[i] = steamGuardCodeTranslations[codePoint % steamGuardCodeTranslations.Length];
                codePoint /= steamGuardCodeTranslations.Length;
            }
          
            return Encoding.UTF8.GetString(codeArray);
        }

        private static bool _aligned = false;
        private static int _timeDifference = 0;

        private static async Task<long> GetSteamTime()
        {
            if (!_aligned)
            {
                await AlignTimeAsync();
            }

            return GetSystemUnixTime() + _timeDifference;
        }

        private static async Task AlignTimeAsync()
        {
            long currentTime = GetSystemUnixTime();

            using HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.PostAsync("https://api.steampowered.com/ITwoFactorService/QueryTime/v0001", null);
            response.EnsureSuccessStatusCode();

            QueryTime query = (await response.Content.ReadFromJsonAsync<QueryTime>())!;
            _timeDifference = (int)(long.Parse(query.Response.ServerTime) - currentTime);
            _aligned = true;
        }

        private static long GetSystemUnixTime()
        {
            return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// Example Response: 
        /// {
        ///    "response": {
        ///        "server_time": "1676308005",
        ///        "skew_tolerance_seconds": "60",
        ///        "large_time_jink": "86400",
        ///        "probe_frequency_seconds": 3600,
        ///        "adjusted_time_probe_frequency_seconds": 300,
        ///        "hint_probe_frequency_seconds": 60,
        ///        "sync_timeout": 60,
        ///        "try_again_seconds": 900,
        ///        "max_attempts": 3
        ///     }
        /// }
        /// </summary>
        public class QueryTime
        {
            [JsonPropertyName("response")]
            public TimeQueryResponse Response { get; set; } = new();

            public class TimeQueryResponse
            {
                [JsonPropertyName("server_time")]
                public string ServerTime { get; set; } = string.Empty;
            }
        }
    }
}
