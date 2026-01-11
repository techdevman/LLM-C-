using System;
using System.Collections.Generic;
using System.Linq;
using TradingStrategyBuilder.Core.Catalog;
using TradingStrategyBuilder.Core.IR;
using TradingStrategyBuilder.Core.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TradingStrategyBuilder.Core.Compilation
{
    /// <summary>
    /// Compiles validated IR into Signal objects (existing JSON structure)
    /// </summary>
    public class SignalCompiler
    {
        private readonly CapabilityCatalog _catalog;

        public SignalCompiler(CapabilityCatalog catalog)
        {
            _catalog = catalog;
        }

        /// <summary>
        /// Compile IR to Signal objects (returns JSON-compatible structure)
        /// </summary>
        public List<JObject> CompileSignals(List<SignalNodeIR> signalNodes)
        {
            return signalNodes.Select(CompileSignalNode).ToList();
        }

        private JObject CompileSignalNode(SignalNodeIR node)
        {
            var capability = _catalog.GetCapability(node.CatalogId);
            if (capability == null)
                throw new InvalidOperationException($"Unknown capability: {node.CatalogId}");

            var signal = new JObject
            {
                ["$type"] = capability.SignalType,
                ["Key"] = GenerateSignalKey(node, capability),
                ["Type"] = GetSignalTypeCode(capability.SignalType)
            };

            // Add arguments
            if (node.Args.Count > 0 || capability.RequiredArgs.Length > 0)
            {
                var args = new JArray();
                foreach (var arg in capability.RequiredArgs)
                {
                    if (node.Args.TryGetValue(arg.Key, out var value))
                    {
                        args.Add(new JObject
                        {
                            ["Key"] = arg.Key,
                            ["BaseValue"] = Convert.ToDouble(value),
                            ["Base"] = Convert.ToDouble(value),
                            ["Last"] = Convert.ToDouble(value),
                            ["Min"] = arg.Min ?? 0.0,
                            ["Max"] = arg.Max ?? 1000000.0,
                            ["Step"] = 0.0,
                            ["Type"] = 0,
                            ["ValueType"] = 0
                        });
                    }
                }
                signal["Args"] = args;
            }

            // Add children
            if (node.Children.Count > 0)
            {
                var children = new JArray();
                foreach (var child in node.Children)
                {
                    children.Add(CompileSignalNode(child));
                }
                signal["Children"] = children;
            }

            // Add parametric-specific fields
            if (capability.SignalType == "SignalParametric")
            {
                signal["R1Md"] = node.Rule1Mode == "Signal" ? 0 : 1;
                signal["R1Op"] = node.Rule1Operation ?? "=";
                signal["CrOp"] = ConvertCrossOpToCode(node.CrossOp ?? "OFF");
                signal["R2Md"] = node.Rule2Mode == "Signal" ? 0 : 1;
                signal["R2Op"] = node.Rule2Operation;
                signal["Entry"] = true; // Default for entry signals
                signal["Exit"] = false;
            }

            // Add common fields
            signal["MktN"] = 1;
            signal["Rqd"] = false;
            signal["SymbolId"] = new JObject
            {
                ["Fn"] = null,
                ["GenMd"] = 0,
                ["ID"] = null,
                ["IsSS"] = false,
                ["Name"] = "@ES", // Default, should be set from settings
                ["Shift"] = 0,
                ["Tf"] = "1D",
                ["Var"] = null
            };

            return signal;
        }

        private string GenerateSignalKey(SignalNodeIR node, SignalCapability capability)
        {
            // Generate a unique key for this signal instance
            // In a real system, this would be more sophisticated
            var keyParts = new List<string> { capability.Name };

            // Add argument values to key for uniqueness
            foreach (var arg in node.Args.OrderBy(a => a.Key))
            {
                if (arg.Key == "Length" || arg.Key == "Depth")
                {
                    keyParts.Add(arg.Value?.ToString() ?? "");
                }
            }

            return string.Join("_", keyParts);
        }

        private int GetSignalTypeCode(string signalType)
        {
            // Map signal types to type codes (these would match your actual system)
            return signalType switch
            {
                "SignalValueRAW" => 2,
                "SignalValueSMA" => 2,
                "SignalValueEMA" => 2,
                "SignalValueRSI" => 2,
                "SignalValueHighest" => 2,
                "SignalValueLowest" => 2,
                "SignalValuePercentChange" => 2,
                "SignalParametric" => 7,
                _ => 2
            };
        }

        private int ConvertCrossOpToCode(string crossOp)
        {
            return crossOp switch
            {
                "OFF" => 0,
                "AND" => 1,
                "OR" => 2,
                "XOR" => 3,
                "IF" => 4,
                _ => 0
            };
        }
    }
}
