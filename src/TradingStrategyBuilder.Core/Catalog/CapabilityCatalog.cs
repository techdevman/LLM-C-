using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingStrategyBuilder.Core.Catalog
{
    /// <summary>
    /// Capability Catalog - A curated, authoritative registry of available signals and capabilities.
    /// This is the single source of truth for what signals exist and how they can be used.
    /// </summary>
    public class CapabilityCatalog
    {
        private readonly Dictionary<string, SignalCapability> _capabilities;
        private readonly Dictionary<string, List<string>> _aliases; // Maps aliases to canonical IDs

        public CapabilityCatalog()
        {
            _capabilities = new Dictionary<string, SignalCapability>(StringComparer.OrdinalIgnoreCase);
            _aliases = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            InitializeCatalog();
        }

        /// <summary>
        /// Get a capability by its ID
        /// </summary>
        public SignalCapability? GetCapability(string id)
        {
            if (_capabilities.TryGetValue(id, out var capability))
                return capability;

            // Try alias lookup
            if (_aliases.TryGetValue(id, out var canonicalIds))
            {
                return canonicalIds.FirstOrDefault() is string canonicalId
                    ? _capabilities.GetValueOrDefault(canonicalId)
                    : null;
            }

            return null;
        }

        /// <summary>
        /// Search capabilities by name pattern (for LLM prompt inclusion)
        /// </summary>
        public IEnumerable<SignalCapability> SearchByName(string pattern)
        {
            var lowerPattern = pattern.ToLowerInvariant();
            return _capabilities.Values
                .Where(c => c.Name.Contains(lowerPattern, StringComparison.OrdinalIgnoreCase) ||
                           c.Aliases.Any(a => a.Contains(lowerPattern, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Get all capabilities (for LLM prompt - limited subset)
        /// </summary>
        public IEnumerable<SignalCapability> GetAllCapabilities()
        {
            return _capabilities.Values;
        }

        /// <summary>
        /// Get a compact registry for LLM prompts (name + id + brief description only)
        /// </summary>
        public string GetCompactRegistry()
        {
            var entries = _capabilities.Values
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Name)
                .Select(c => $"- {c.Name} (ID: {c.Id}) - {c.Description}");

            return string.Join("\n", entries);
        }

        private void InitializeCatalog()
        {
            // Raw Data Signals
            RegisterRawSignal("raw.close", "Close", "Closing price");
            RegisterRawSignal("raw.open", "Open", "Opening price");
            RegisterRawSignal("raw.high", "High", "High price");
            RegisterRawSignal("raw.low", "Low", "Low price");
            RegisterRawSignal("raw.volume", "Volume", "Trading volume");
            RegisterRawSignal("raw.vix", "VIX", "VIX volatility index");

            // Moving Averages
            RegisterSignal(new SignalCapability
            {
                Id = "sma",
                Name = "SMA",
                Aliases = new[] { "Simple Moving Average", "Moving Average" },
                Category = "Indicator",
                Description = "Simple Moving Average - calculates average price over a period",
                SignalType = "SignalValueSMA",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Length", Type = ArgType.Number, Min = 1, Description = "Period length" }
                },
                RequiredChildren = 1,
                ChildDescriptions = new[] { "Source signal (e.g., Close)" }
            });

            RegisterSignal(new SignalCapability
            {
                Id = "ema",
                Name = "EMA",
                Aliases = new[] { "Exponential Moving Average" },
                Category = "Indicator",
                Description = "Exponential Moving Average - gives more weight to recent prices",
                SignalType = "SignalValueEMA",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Length", Type = ArgType.Number, Min = 1, Description = "Period length" }
                },
                RequiredChildren = 1,
                ChildDescriptions = new[] { "Source signal (e.g., Close)" }
            });

            // Technical Indicators
            RegisterSignal(new SignalCapability
            {
                Id = "rsi",
                Name = "RSI",
                Aliases = new[] { "Relative Strength Index" },
                Category = "Indicator",
                Description = "Relative Strength Index - momentum oscillator (0-100)",
                SignalType = "SignalValueRSI",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Length", Type = ArgType.Number, Min = 1, Description = "Period length (typically 14)" }
                },
                RequiredChildren = 1,
                ChildDescriptions = new[] { "Source signal (typically Close)" }
            });

            RegisterSignal(new SignalCapability
            {
                Id = "atr",
                Name = "ATR",
                Aliases = new[] { "Average True Range" },
                Category = "Indicator",
                Description = "Average True Range - measures market volatility",
                SignalType = "SignalValueRangeStochasticATR",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Length", Type = ArgType.Number, Min = 1, Description = "Period length (typically 14)" }
                },
                RequiredChildren = 2,
                ChildDescriptions = new[] { "High signal", "Low signal" }
            });

            RegisterSignal(new SignalCapability
            {
                Id = "highest",
                Name = "Highest",
                Aliases = new[] { "Max", "High" },
                Category = "Indicator",
                Description = "Returns the highest value over a lookback period",
                SignalType = "SignalValueHighest",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Length", Type = ArgType.Number, Min = 1, Description = "Lookback period" }
                },
                RequiredChildren = 1,
                ChildDescriptions = new[] { "Source signal" }
            });

            RegisterSignal(new SignalCapability
            {
                Id = "lowest",
                Name = "Lowest",
                Aliases = new[] { "Min", "Low" },
                Category = "Indicator",
                Description = "Returns the lowest value over a lookback period",
                SignalType = "SignalValueLowest",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Length", Type = ArgType.Number, Min = 1, Description = "Lookback period" }
                },
                RequiredChildren = 1,
                ChildDescriptions = new[] { "Source signal" }
            });

            RegisterSignal(new SignalCapability
            {
                Id = "percentchange",
                Name = "PercentChange",
                Aliases = new[] { "Percent Change", "PctChange" },
                Category = "Indicator",
                Description = "Calculates percentage change over a specified depth (bars)",
                SignalType = "SignalValuePercentChange",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Depth", Type = ArgType.Number, Min = 1, Description = "Number of bars back to compare" }
                },
                RequiredChildren = 1,
                ChildDescriptions = new[] { "Source signal" }
            });

            // Parametric Signals (Rules/Comparisons)
            RegisterSignal(new SignalCapability
            {
                Id = "parametric",
                Name = "Parametric",
                Aliases = new[] { "Rule", "Condition", "Comparison" },
                Category = "Logic",
                Description = "Composite signal that evaluates rules and combines them with logical operations (AND, OR, etc.)",
                SignalType = "SignalParametric",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Rule1 Base Offset", Type = ArgType.Number, Description = "Offset for first signal in Rule1 (bars)" },
                    new ArgDefinition { Key = "Rule1 Second Offset", Type = ArgType.Number, Description = "Offset for second signal in Rule1 (bars)" },
                    new ArgDefinition { Key = "Rule1 Value", Type = ArgType.Number, Optional = true, Description = "Constant value for Rule1 if comparing to value" }
                },
                RequiredChildren = 2,
                ChildDescriptions = new[] { "First signal (Rule1 left side)", "Second signal or value (Rule1 right side)" }
            });

            // Additional commonly used signals for Phase 1
            RegisterSignal(new SignalCapability
            {
                Id = "macd",
                Name = "MACD",
                Category = "Indicator",
                Description = "Moving Average Convergence Divergence",
                SignalType = "SignalValueMACD",
                RequiredArgs = new[]
                {
                    new ArgDefinition { Key = "Length", Type = ArgType.Number, Min = 1, Description = "Signal smoothing length" }
                },
                RequiredChildren = 2,
                ChildDescriptions = new[] { "Short EMA", "Long EMA" }
            });
        }

        private void RegisterRawSignal(string id, string name, string description)
        {
            RegisterSignal(new SignalCapability
            {
                Id = id,
                Name = name,
                Category = "Data",
                Description = description,
                SignalType = "SignalValueRAW",
                RequiredArgs = Array.Empty<ArgDefinition>(),
                RequiredChildren = 0,
                ChildDescriptions = Array.Empty<string>()
            });
        }

        private void RegisterSignal(SignalCapability capability)
        {
            _capabilities[capability.Id] = capability;

            // Register aliases
            foreach (var alias in capability.Aliases)
            {
                if (!_aliases.ContainsKey(alias))
                    _aliases[alias] = new List<string>();
                _aliases[alias].Add(capability.Id);
            }
        }
    }
}
