using System.Diagnostics;
using GameServerManager.Attributes;
using GameServerManager.Extensions;
using GameServerManager.GameServers.Components;

namespace GameServerManager.GameServers.Configs
{
    public class AdvancedConfig
    {
        [Select(Label = "Process Priority", HelperText = "Sets the priority class for the specified process.", SelectItemsType = typeof(ProcessPrioritySelectItem))]
        public string ProcessPriority { get; set; } = ProcessPriorityClass.Normal.ToStringEx();

        [TextField(Label = "Processor Affinity", HelperText = "Processor Affinity also called CPU pinning, allows the user to assign a process to use only a few cores.", Required = true, ProcessorAffinity = true)]
        public uint ProcessorAffinity { get; set; } = Utilities.ProcessorAffinity.Default;

        [CheckBox(Label = "Auto Start", HelperText = "Automatically start the server when WindowsGSM started.", IsSwitch = true)]
        public bool AutoStart { get; set; }

        [CheckBox(Label = "Restart on Crash", HelperText = "Automatically restart the server when the server crashes unexpectedly.", IsSwitch = true)]
        public bool RestartOnCrash { get; set; }

        [CheckBox(Label = "Auto Update and Restart", HelperText = "Automatically stop and update the server and start the server when an update is available. This function only runs when the server is started.", IsSwitch = true)]
        public bool AutoUpdateAndRestart { get; set; }
    }
}
