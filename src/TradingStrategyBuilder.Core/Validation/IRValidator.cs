using System;
using System.Collections.Generic;
using System.Linq;
using TradingStrategyBuilder.Core.Catalog;
using TradingStrategyBuilder.Core.IR;

namespace TradingStrategyBuilder.Core.Validation
{
    /// <summary>
    /// Validates Intermediate Representation against the Capability Catalog.
    /// Enforces strict validation - invalid IR must fail deterministically.
    /// </summary>
    public class IRValidator
    {
        private readonly CapabilityCatalog _catalog;

        public IRValidator(CapabilityCatalog catalog)
        {
            _catalog = catalog;
        }

        public ValidationResult Validate(IntermediateRepresentation ir)
        {
            var errors = new List<ValidationError>();

            // Version check
            if (ir.Version != "1.0")
            {
                errors.Add(new ValidationError("Unsupported IR version", "Version"));
            }

            // Strategy validation
            if (ir.Strategy == null)
            {
                errors.Add(new ValidationError("Strategy is required", "Strategy"));
                return new ValidationResult(errors);
            }

            // Validate entry signals
            foreach (var signal in ir.Strategy.EntrySignals)
            {
                errors.AddRange(ValidateSignalNode(signal, "EntrySignals"));
            }

            // Validate exit signals
            foreach (var signal in ir.Strategy.ExitSignals)
            {
                errors.AddRange(ValidateSignalNode(signal, "ExitSignals"));
            }

            return new ValidationResult(errors);
        }

        private List<ValidationError> ValidateSignalNode(SignalNodeIR node, string path)
        {
            var errors = new List<ValidationError>();

            // Check catalog ID exists
            var capability = _catalog.GetCapability(node.CatalogId);
            if (capability == null)
            {
                errors.Add(new ValidationError($"Unknown signal catalog ID: {node.CatalogId}", path));
                return errors; // Can't validate further without capability
            }

            // Validate required arguments
            foreach (var requiredArg in capability.RequiredArgs.Where(a => !a.Optional))
            {
                if (!node.Args.ContainsKey(requiredArg.Key))
                {
                    errors.Add(new ValidationError(
                        $"Missing required argument '{requiredArg.Key}' for signal '{node.CatalogId}'",
                        $"{path}.Args"));
                }
                else
                {
                    // Validate argument type and constraints
                    var argValue = node.Args[requiredArg.Key];
                    if (requiredArg.Type == ArgType.Number)
                    {
                        if (!(argValue is double || argValue is int))
                        {
                            errors.Add(new ValidationError(
                                $"Argument '{requiredArg.Key}' must be a number for signal '{node.CatalogId}'",
                                $"{path}.Args.{requiredArg.Key}"));
                        }
                        else
                        {
                            var numValue = Convert.ToDouble(argValue);
                            if (requiredArg.Min.HasValue && numValue < requiredArg.Min.Value)
                            {
                                errors.Add(new ValidationError(
                                    $"Argument '{requiredArg.Key}' must be >= {requiredArg.Min.Value} for signal '{node.CatalogId}'",
                                    $"{path}.Args.{requiredArg.Key}"));
                            }
                            if (requiredArg.Max.HasValue && numValue > requiredArg.Max.Value)
                            {
                                errors.Add(new ValidationError(
                                    $"Argument '{requiredArg.Key}' must be <= {requiredArg.Max.Value} for signal '{node.CatalogId}'",
                                    $"{path}.Args.{requiredArg.Key}"));
                            }
                        }
                    }
                }
            }

            // Validate children count
            if (node.Children.Count < capability.RequiredChildren)
            {
                errors.Add(new ValidationError(
                    $"Signal '{node.CatalogId}' requires {capability.RequiredChildren} children, but {node.Children.Count} provided",
                    $"{path}.Children"));
            }

            // Validate parametric signal specific fields
            if (capability.SignalType == "SignalParametric")
            {
                if (string.IsNullOrEmpty(node.Rule1Mode))
                {
                    errors.Add(new ValidationError(
                        $"Parametric signal '{node.CatalogId}' requires Rule1Mode",
                        $"{path}.Rule1Mode"));
                }
                else if (node.Rule1Mode != "Signal" && node.Rule1Mode != "Value")
                {
                    errors.Add(new ValidationError(
                        $"Rule1Mode must be 'Signal' or 'Value' for signal '{node.CatalogId}'",
                        $"{path}.Rule1Mode"));
                }

                if (string.IsNullOrEmpty(node.Rule1Operation))
                {
                    errors.Add(new ValidationError(
                        $"Parametric signal '{node.CatalogId}' requires Rule1Operation",
                        $"{path}.Rule1Operation"));
                }

                if (string.IsNullOrEmpty(node.CrossOp))
                {
                    errors.Add(new ValidationError(
                        $"Parametric signal '{node.CatalogId}' requires CrossOp",
                        $"{path}.CrossOp"));
                }
            }

            // Recursively validate children
            for (int i = 0; i < node.Children.Count; i++)
            {
                errors.AddRange(ValidateSignalNode(node.Children[i], $"{path}.Children[{i}]"));
            }

            return errors;
        }
    }

    public class ValidationResult
    {
        public List<ValidationError> Errors { get; }
        public bool IsValid => Errors.Count == 0;

        public ValidationResult(List<ValidationError> errors)
        {
            Errors = errors;
        }

        public string GetErrorMessage()
        {
            if (IsValid)
                return "Validation passed";

            return "Validation failed:\n" + string.Join("\n", Errors.Select(e => $"- {e.Path}: {e.Message}"));
        }
    }

    public class ValidationError
    {
        public string Message { get; }
        public string Path { get; }

        public ValidationError(string message, string path)
        {
            Message = message;
            Path = path;
        }
    }
}
