﻿using MudBlazor;

namespace GameServerManager.Attributes
{
    /// <summary>
    /// https://mudblazor.com/components/textfield#api
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TextFieldAttribute : Attribute
    {
        public string Label { get; set; } = string.Empty;

        public string HelperText { get; set; } = string.Empty;

        public bool Required { get; set; }

        public string RequiredError { get; set; } = string.Empty;

        public InputType InputType { get; set; } = InputType.Text;

        public bool ReadOnly { get; set; }

        public string Placeholder { get; set; } = string.Empty;

        public bool FolderBrowser { get; set; }

        public bool ProcessorAffinity { get; set; }
    }
}
