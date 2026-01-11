using System;
using System.Collections.Generic;

namespace TradingStrategyBuilder.Core.IR
{
    /// <summary>
    /// Intermediate Representation - A small, versioned structure that the LLM translates natural language into.
    /// This is optimized for LLM reasoning, not execution.
    /// </summary>
    public class IntermediateRepresentation
    {
        public string Version { get; set; } = "1.0";
        public StrategyIR Strategy { get; set; } = new();
    }

    public class StrategyIR
    {
        public List<SignalNodeIR> EntrySignals { get; set; } = new();
        public List<SignalNodeIR> ExitSignals { get; set; } = new();
        public StrategySettingsIR? Settings { get; set; }
        public string? ClarificationRequest { get; set; } // If present, indicates missing/ambiguous information
    }

    public class SignalNodeIR
    {
        public string CatalogId { get; set; } = string.Empty; // References CapabilityCatalog
        public Dictionary<string, object> Args { get; set; } = new();
        public List<SignalNodeIR> Children { get; set; } = new();
        public string? Rule1Mode { get; set; } // "Signal" or "Value" (for parametric signals)
        public string? Rule1Operation { get; set; } // ">", "<", ">=", "<=", "=", "!=", "crosses above", "crosses below"
        public string? CrossOp { get; set; } // "OFF", "AND", "OR", "XOR", "IF"
        public string? Rule2Mode { get; set; }
        public string? Rule2Operation { get; set; }
    }

    public class StrategySettingsIR
    {
        public string? Symbol { get; set; }
        public string? Timeframe { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public double? PositionSize { get; set; }
        public int? MaxPositions { get; set; }
        public int? MaxHoldDays { get; set; }
        public string? EntryMode { get; set; } // "Market", "Limit", etc.
        public string? ExitMode { get; set; }
    }
}
