namespace GameServerManager.Attributes
{
    /// <summary>
    /// https://mudblazor.com/components/select
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SelectAttribute : Attribute
    {
        public string Label { get; set; } = string.Empty;

        public Type? SelectItemsType { get; set; }

        public string HelperText { get; set; } = string.Empty;

        public bool GameServerBranches { get; set; }
    }

    public interface ISelectItem
    {
        public string[] Values { get; set; }
    }
}
