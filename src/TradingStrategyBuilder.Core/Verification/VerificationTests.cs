using System;
using System.Linq;
using TradingStrategyBuilder.Core.Catalog;
using TradingStrategyBuilder.Core.IR;
using TradingStrategyBuilder.Core.Validation;
using Newtonsoft.Json;

namespace TradingStrategyBuilder.Core.Verification
{
    /// <summary>
    /// Verification tests to validate Phase 1 deliverables:
    /// 1. Small curated capability catalog
    /// 2. First-version IR schema
    /// 3. Basic validation rules in C#
    /// </summary>
    public class VerificationTests
    {
        public static VerificationResults RunAll()
        {
            var results = new VerificationResults();

            // Test 1: Capability Catalog
            results.CatalogTest = TestCapabilityCatalog();

            // Test 2: IR Schema
            results.IRSchemaTest = TestIRSchema();

            // Test 3: Validation Rules
            results.ValidationTest = TestValidationRules();

            return results;
        }

        /// <summary>
        /// Test 1: Verify small curated capability catalog exists and functions
        /// </summary>
        public static string TestCapabilityCatalog()
        {
            try
            {
                var catalog = new CapabilityCatalog();

                // Verify catalog is not empty
                var allCapabilities = catalog.GetAllCapabilities().ToList();
                if (allCapabilities.Count == 0)
                    return "❌ FAIL: Catalog is empty";

                // Verify we have representative signals
                var rawSignals = allCapabilities.Where(c => c.Category == "Data").ToList();
                var indicatorSignals = allCapabilities.Where(c => c.Category == "Indicator").ToList();
                var logicSignals = allCapabilities.Where(c => c.Category == "Logic").ToList();

                if (rawSignals.Count == 0)
                    return "❌ FAIL: No raw data signals found";
                if (indicatorSignals.Count == 0)
                    return "❌ FAIL: No indicator signals found";

                // Verify we can retrieve a signal by ID
                var smaCapability = catalog.GetCapability("sma");
                if (smaCapability == null)
                    return "❌ FAIL: Cannot retrieve signal by ID 'sma'";
                if (smaCapability.Name != "SMA")
                    return "❌ FAIL: Retrieved signal has wrong name";

                // Verify search works
                var searchResults = catalog.SearchByName("moving average").ToList();
                if (searchResults.Count == 0)
                    return "❌ FAIL: Search functionality not working";

                // Verify compact registry exists
                var registry = catalog.GetCompactRegistry();
                if (string.IsNullOrEmpty(registry))
                    return "❌ FAIL: Compact registry is empty";

                return $"✅ PASS: Catalog has {allCapabilities.Count} signals " +
                       $"(Raw: {rawSignals.Count}, Indicators: {indicatorSignals.Count}, Logic: {logicSignals.Count})";
            }
            catch (Exception ex)
            {
                return $"❌ FAIL: Exception: {ex.Message}";
            }
        }

        /// <summary>
        /// Test 2: Verify first-version IR schema exists and is serializable
        /// </summary>
        public static string TestIRSchema()
        {
            try
            {
                // Create a sample IR structure
                var ir = new IntermediateRepresentation
                {
                    Version = "1.0",
                    Strategy = new StrategyIR
                    {
                        EntrySignals = new List<SignalNodeIR>
                        {
                            new SignalNodeIR
                            {
                                CatalogId = "raw.close",
                                Args = new Dictionary<string, object>(),
                                Children = new List<SignalNodeIR>()
                            }
                        },
                        ExitSignals = new List<SignalNodeIR>(),
                        Settings = new StrategySettingsIR
                        {
                            Symbol = "AAPL",
                            Timeframe = "1D"
                        }
                    }
                };

                // Verify version field exists
                if (ir.Version != "1.0")
                    return "❌ FAIL: IR version is not '1.0'";

                // Verify structure can be serialized to JSON
                var json = JsonConvert.SerializeObject(ir, Formatting.Indented);
                if (string.IsNullOrEmpty(json))
                    return "❌ FAIL: IR cannot be serialized to JSON";

                // Verify structure can be deserialized
                var deserialized = JsonConvert.DeserializeObject<IntermediateRepresentation>(json);
                if (deserialized == null)
                    return "❌ FAIL: IR cannot be deserialized from JSON";
                if (deserialized.Version != "1.0")
                    return "❌ FAIL: Deserialized IR has wrong version";

                // Verify CatalogId field exists
                if (ir.Strategy.EntrySignals[0].CatalogId != "raw.close")
                    return "❌ FAIL: CatalogId field not working";

                // Verify Args dictionary exists
                if (ir.Strategy.EntrySignals[0].Args == null)
                    return "❌ FAIL: Args dictionary is null";

                // Verify Children array exists
                if (ir.Strategy.EntrySignals[0].Children == null)
                    return "❌ FAIL: Children array is null";

                // Verify Settings structure exists
                if (ir.Strategy.Settings == null)
                    return "❌ FAIL: Settings structure is null";
                if (ir.Strategy.Settings.Symbol != "AAPL")
                    return "❌ FAIL: Settings.Symbol not working";

                return "✅ PASS: IR schema is valid and serializable";
            }
            catch (Exception ex)
            {
                return $"❌ FAIL: Exception: {ex.Message}";
            }
        }

        /// <summary>
        /// Test 3: Verify basic validation rules exist and function
        /// </summary>
        public static string TestValidationRules()
        {
            try
            {
                var catalog = new CapabilityCatalog();
                var validator = new IRValidator(catalog);

                // Test 1: Valid IR should pass
                var validIR = new IntermediateRepresentation
                {
                    Version = "1.0",
                    Strategy = new StrategyIR
                    {
                        EntrySignals = new List<SignalNodeIR>
                        {
                            new SignalNodeIR
                            {
                                CatalogId = "sma",
                                Args = new Dictionary<string, object> { { "Length", 200 } },
                                Children = new List<SignalNodeIR>
                                {
                                    new SignalNodeIR
                                    {
                                        CatalogId = "raw.close",
                                        Args = new Dictionary<string, object>(),
                                        Children = new List<SignalNodeIR>()
                                    }
                                }
                            }
                        },
                        ExitSignals = new List<SignalNodeIR>()
                    }
                };

                var validResult = validator.Validate(validIR);
                if (!validResult.IsValid)
                    return $"❌ FAIL: Valid IR failed validation: {validResult.GetErrorMessage()}";

                // Test 2: Invalid version should fail
                var invalidVersionIR = new IntermediateRepresentation
                {
                    Version = "2.0", // Invalid version
                    Strategy = new StrategyIR { EntrySignals = new List<SignalNodeIR>(), ExitSignals = new List<SignalNodeIR>() }
                };
                var versionResult = validator.Validate(invalidVersionIR);
                if (versionResult.IsValid)
                    return "❌ FAIL: Invalid version passed validation";

                // Test 3: Unknown catalog ID should fail
                var unknownIdIR = new IntermediateRepresentation
                {
                    Version = "1.0",
                    Strategy = new StrategyIR
                    {
                        EntrySignals = new List<SignalNodeIR>
                        {
                            new SignalNodeIR
                            {
                                CatalogId = "unknown_signal_id",
                                Args = new Dictionary<string, object>(),
                                Children = new List<SignalNodeIR>()
                            }
                        },
                        ExitSignals = new List<SignalNodeIR>()
                    }
                };
                var unknownIdResult = validator.Validate(unknownIdIR);
                if (unknownIdResult.IsValid)
                    return "❌ FAIL: Unknown catalog ID passed validation";

                // Test 4: Missing required argument should fail
                var missingArgIR = new IntermediateRepresentation
                {
                    Version = "1.0",
                    Strategy = new StrategyIR
                    {
                        EntrySignals = new List<SignalNodeIR>
                        {
                            new SignalNodeIR
                            {
                                CatalogId = "sma",
                                Args = new Dictionary<string, object>(), // Missing Length argument
                                Children = new List<SignalNodeIR>
                                {
                                    new SignalNodeIR
                                    {
                                        CatalogId = "raw.close",
                                        Args = new Dictionary<string, object>(),
                                        Children = new List<SignalNodeIR>()
                                    }
                                }
                            }
                        },
                        ExitSignals = new List<SignalNodeIR>()
                    }
                };
                var missingArgResult = validator.Validate(missingArgIR);
                if (missingArgResult.IsValid)
                    return "❌ FAIL: Missing required argument passed validation";

                // Test 5: Missing required children should fail
                var missingChildrenIR = new IntermediateRepresentation
                {
                    Version = "1.0",
                    Strategy = new StrategyIR
                    {
                        EntrySignals = new List<SignalNodeIR>
                        {
                            new SignalNodeIR
                            {
                                CatalogId = "sma",
                                Args = new Dictionary<string, object> { { "Length", 200 } },
                                Children = new List<SignalNodeIR>() // Missing required child
                            }
                        },
                        ExitSignals = new List<SignalNodeIR>()
                    }
                };
                var missingChildrenResult = validator.Validate(missingChildrenIR);
                if (missingChildrenResult.IsValid)
                    return "❌ FAIL: Missing required children passed validation";

                return "✅ PASS: All validation rules are functional";
            }
            catch (Exception ex)
            {
                return $"❌ FAIL: Exception: {ex.Message}";
            }
        }
    }

    public class VerificationResults
    {
        public string CatalogTest { get; set; } = string.Empty;
        public string IRSchemaTest { get; set; } = string.Empty;
        public string ValidationTest { get; set; } = string.Empty;

        public bool AllPassed => 
            CatalogTest.StartsWith("✅") && 
            IRSchemaTest.StartsWith("✅") && 
            ValidationTest.StartsWith("✅");

        public string GetSummary()
        {
            return $"""
                Verification Results:
                
                1. Capability Catalog: {CatalogTest}
                2. IR Schema: {IRSchemaTest}
                3. Validation Rules: {ValidationTest}
                
                Overall: {(AllPassed ? "✅ ALL TESTS PASSED" : "❌ SOME TESTS FAILED")}
                """;
        }
    }
}
