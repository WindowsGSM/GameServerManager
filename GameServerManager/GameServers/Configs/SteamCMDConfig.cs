﻿using MudBlazor;
using GameServerManager.Attributes;
using GameServerManager.GameServers.Components;

namespace GameServerManager.GameServers.Configs
{
    public class SteamCMDConfig
    {
        [TextField(Label = "SteamCMD Path", HelperText = "steamcmd.exe will be downloaded if steamcmd.exe does not exist.", Required = true, FolderBrowser = true)]
        public string Path { get; set; } = SteamCMD.FileName;

        [TextField(Label = "Game", HelperText = "Product Name")]
        public string Game { get; set; } = string.Empty;

        [TextField(Label = "App Id", HelperText = "App Id", Required = true)]
        public string AppId { get; set; } = string.Empty;

        [TextField(Label = "Steam Username", HelperText = "Steam account username", Required = true)]
        public string Username { get; set; } = string.Empty;

        [TextField(Label = "Steam Password", HelperText = "Steam account password", InputType = InputType.Password)]
        public string Password { get; set; } = string.Empty;

        [TextField(Label = "Steam 2FA Shared Secret", HelperText = "Steam account 2FA shared secret", InputType = InputType.Password)]
        public string Secret { get; set; } = string.Empty;

        [Select(Label = "Branch Name", HelperText = "Beta branch name", GameServerBranches = true)]
        public string BetaName { get; set; } = string.Empty;

        [TextField(Label = "Branch Password", HelperText = "Beta branch password, some beta branches are protected by a password.")]
        public string BetaPassword { get; set; } = string.Empty;

        [CheckBox(Label = "Validate On Install", HelperText = "Validate will check all the server files to make sure they match the SteamCMD files.", IsSwitch = true)]
        public bool ValidateOnInstall { get; set; } = true;

        [CheckBox(Label = "Validate On Update", HelperText = "Validate will check all the server files to make sure they match the SteamCMD files.", IsSwitch = true)]
        public bool ValidateOnUpdate { get; set; }

        [RadioGroup(Text = "Console Type", HelperText = "steamcmd.exe console mode")]
        [Radio(Option = "Pseudo Console")]
        [Radio(Option = "Redirect Standard Input/Output")]
        [Radio(Option = "Windowed")]
        public string ConsoleMode { get; set; } = "Pseudo Console";
    }
}
