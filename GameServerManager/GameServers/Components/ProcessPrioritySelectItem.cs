﻿using GameServerManager.Attributes;

namespace GameServerManager.GameServers.Components
{
    public class ProcessPrioritySelectItem : ISelectItem
    {
        public string[] Values { get; set; } = new string[]
        {
            "Realtime",
            "High",
            "Above normal",
            "Normal",
            "Below normal",
            "Low",
        };
    }
}
