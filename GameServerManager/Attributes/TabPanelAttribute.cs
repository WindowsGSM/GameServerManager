namespace GameServerManager.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TabPanelAttribute : Attribute
    {
        public string Text { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
