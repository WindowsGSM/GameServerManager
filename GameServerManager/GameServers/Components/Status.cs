using MudBlazor;

namespace GameServerManager.GameServers.Components
{
    public enum Status
    {
        NotInstalled, Stopped, Started, Starting, Stopping, Restarting, Killing, Installing, Updating, Deleting, Backuping, Restoring, InstallingMod, UpdatingMod, DeletingMod
    }

    public enum Operation
    {
        Start, Stop, Restart, Kill, Install, Update, Delete, Backup, Restore, InstallMod, UpdateMod, DeleteMod
    }

    public static class StatusExtensions
    {
        public static string ToStringEx(this Status status)
        {
            return status switch
            {
                Status.NotInstalled => "⚠️ Not Installed",
                Status.Stopped => "Stopped",
                Status.Started => "Started",
                Status.Starting => "Starting",
                Status.Stopping => "Stopping",
                Status.Restarting => "Restarting",
                Status.Killing => "Killing",
                Status.Installing => "Installing",
                Status.Updating => "Updating",
                Status.Deleting => "Deleting",
                Status.Backuping => "Backuping",
                Status.Restoring => "Restoring",
                Status.InstallingMod => "Installing Mod",
                Status.UpdatingMod => "Updating Mod",
                Status.DeletingMod => "Deleting Mod",
                _ => string.Empty,
            };
        }

        public static Color ToColor(this Status status)
        {
            if (status == Status.Starting || status == Status.Started)
            {
                return Color.Success;
            }
            else if (status == Status.Updating || status == Status.UpdatingMod)
            {
                return Color.Warning;
            }
            else if (status == Status.Restarting || status == Status.Backuping || status == Status.Restoring)
            {
                return Color.Info;
            }
            else if (status == Status.Deleting || status == Status.DeletingMod)
            {
                return Color.Error;
            }
            else if (status == Status.Installing || status == Status.InstallingMod)
            {
                return Color.Info;
            }

            return Color.Default;
        }

        public static bool IsRunning(this Status status)
        {
            return status != Status.NotInstalled && status != Status.Stopped && status != Status.Started;
        }

        public static bool IsDisabled(this Status status, Operation operation)
        {
            return operation switch
            {
                Operation.Start => status != Status.Stopped,
                Operation.Stop => status != Status.Started,
                Operation.Restart => status != Status.Started,
                Operation.Kill => status != Status.Started,
                Operation.Install => status != Status.NotInstalled,
                Operation.Update => status != Status.Stopped,
                Operation.Delete => status != Status.NotInstalled && status != Status.Stopped,
                Operation.Backup => status != Status.Stopped,
                Operation.Restore => status != Status.Stopped,
                Operation.InstallMod => status != Status.Stopped,
                Operation.UpdateMod => status != Status.Stopped,
                Operation.DeleteMod => status != Status.Stopped,
                _ => false,
            };
        }
    }
}
