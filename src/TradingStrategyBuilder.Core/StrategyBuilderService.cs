using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingStrategyBuilder.Core.Catalog;
using TradingStrategyBuilder.Core.Compilation;
using TradingStrategyBuilder.Core.IR;
using TradingStrategyBuilder.Core.LLM;
using TradingStrategyBuilder.Core.Validation;
using Newtonsoft.Json.Linq;

namespace TradingStrategyBuilder.Core
{
    /// <summary>
    /// Main service orchestrating the translation pipeline:
    /// Natural Language → IR → Validation → Compilation → Signal Objects
    /// </summary>
    public class StrategyBuilderService
    {
        private readonly CapabilityCatalog _catalog;
        private readonly LLMTranslator _translator;
        private readonly IRValidator _validator;
        private readonly SignalCompiler _compiler;

        public StrategyBuilderService(string openAiApiKey, string? apiUrl = null)
        {
            _catalog = new CapabilityCatalog();
            _translator = new LLMTranslator(_catalog, openAiApiKey, apiUrl);
            _validator = new IRValidator(_catalog);
            _compiler = new SignalCompiler(_catalog);
        }

        /// <summary>
        /// Build a strategy from natural language
        /// </summary>
        public async Task<StrategyBuildResult> BuildStrategyAsync(string naturalLanguage)
        {
            try
            {
                // Step 1: Translate natural language to IR
                var ir = await _translator.TranslateAsync(naturalLanguage);

                // Step 2: Check for clarification requests
                if (!string.IsNullOrEmpty(ir.Strategy.ClarificationRequest))
                {
                    return new StrategyBuildResult
                    {
                        Success = false,
                        ClarificationRequest = ir.Strategy.ClarificationRequest,
                        IR = ir
                    };
                }

                // Step 3: Validate IR
                var validationResult = _validator.Validate(ir);
                if (!validationResult.IsValid)
                {
                    return new StrategyBuildResult
                    {
                        Success = false,
                        ValidationErrors = validationResult.Errors,
                        ErrorMessage = validationResult.GetErrorMessage(),
                        IR = ir
                    };
                }

                // Step 4: Compile to Signal objects
                var entrySignals = _compiler.CompileSignals(ir.Strategy.EntrySignals);
                var exitSignals = _compiler.CompileSignals(ir.Strategy.ExitSignals);

                return new StrategyBuildResult
                {
                    Success = true,
                    IR = ir,
                    EntrySignals = entrySignals,
                    ExitSignals = exitSignals,
                    Settings = ir.Strategy.Settings
                };
            }
            catch (Exception ex)
            {
                return new StrategyBuildResult
                {
                    Success = false,
                    ErrorMessage = $"Error building strategy: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get the capability catalog (for UI display)
        /// </summary>
        public CapabilityCatalog GetCatalog() => _catalog;
    }

    public class StrategyBuildResult
    {
        public bool Success { get; set; }
        public IntermediateRepresentation? IR { get; set; }
        public List<JObject>? EntrySignals { get; set; }
        public List<JObject>? ExitSignals { get; set; }
        public StrategySettingsIR? Settings { get; set; }
        public List<ValidationError>? ValidationErrors { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ClarificationRequest { get; set; }
        public Exception? Exception { get; set; }

        public string ToJson()
        {
            var result = new
            {
                Success,
                EntrySignals = EntrySignals,
                ExitSignals = ExitSignals,
                Settings,
                ErrorMessage,
                ClarificationRequest
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
        }
    }
}
