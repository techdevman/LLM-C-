using System;

namespace TradingStrategyBuilder.Core.Catalog
{
    /// <summary>
    /// Represents a signal capability in the catalog
    /// </summary>
    public class SignalCapability
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string[] Aliases { get; set; } = Array.Empty<string>();
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SignalType { get; set; } = string.Empty;
        public ArgDefinition[] RequiredArgs { get; set; } = Array.Empty<ArgDefinition>();
        public int RequiredChildren { get; set; }
        public string[] ChildDescriptions { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Definition of an argument for a signal
    /// </summary>
    public class ArgDefinition
    {
        public string Key { get; set; } = string.Empty;
        public ArgType Type { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool Optional { get; set; }
    }

    public enum ArgType
    {
        Number,
        String,
        Enum
    }
}
