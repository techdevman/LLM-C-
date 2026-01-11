using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TradingStrategyBuilder.Core.Catalog;
using TradingStrategyBuilder.Core.IR;

namespace TradingStrategyBuilder.Core.LLM
{
    /// <summary>
    /// LLM Translator - Responsible only for translating natural language into IR.
    /// Uses OpenAI API (or compatible).
    /// </summary>
    public class LLMTranslator
    {
        private readonly CapabilityCatalog _catalog;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly HttpClient _httpClient;

        public LLMTranslator(CapabilityCatalog catalog, string apiKey, string? apiUrl = null)
        {
            _catalog = catalog;
            _apiKey = apiKey;
            _apiUrl = apiUrl ?? "https://api.openai.com/v1/chat/completions";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<IntermediateRepresentation> TranslateAsync(string naturalLanguage)
        {
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = naturalLanguage;

            var requestBody = new
            {
                model = "gpt-4o", 
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1, // Low temperature for deterministic output
                response_format = new { type = "json_object" }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            
            // Check for errors and provide detailed error messages
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                var errorMessage = $"OpenAI API error ({response.StatusCode}): ";
                
                // Try to parse error response for more details
                try
                {
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(errorBody);
                    if (errorObj?.error != null)
                    {
                        var errorDetails = errorObj.error;
                        errorMessage += $"{errorDetails.message ?? errorDetails.code ?? "Unknown error"}";
                        if (errorDetails.type != null)
                        {
                            errorMessage += $" (Type: {errorDetails.type})";
                        }
                    }
                    else
                    {
                        errorMessage += errorBody;
                    }
                }
                catch
                {
                    errorMessage += errorBody;
                }
                
                throw new Exception(errorMessage);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(responseBody);

            if (apiResponse?.Choices == null || apiResponse.Choices.Length == 0)
                throw new Exception("No response from LLM - API returned empty choices array");

            var irJson = apiResponse.Choices[0].Message?.Content;
            if (string.IsNullOrEmpty(irJson))
                throw new Exception("Empty response from LLM - message content is null or empty");

            var ir = JsonConvert.DeserializeObject<IntermediateRepresentation>(irJson);

            if (ir == null)
                throw new Exception("Failed to parse LLM response as IR - deserialization returned null");

            return ir;
        }

        private string BuildSystemPrompt()
        {
            var catalogRegistry = _catalog.GetCompactRegistry();

            return $@"You are a trading strategy translator. Your job is to translate natural language trading strategy descriptions into a structured Intermediate Representation (IR) in JSON format.

AVAILABLE SIGNALS:
{catalogRegistry}

IR STRUCTURE:
{{
  ""Version"": ""1.0"",
  ""Strategy"": {{
    ""EntrySignals"": [
      {{
        ""CatalogId"": ""signal-id-from-catalog"",
        ""Args"": {{ ""Key"": value }},
        ""Children"": [ /* child signals */ ],
        ""Rule1Mode"": ""Signal"" or ""Value"",  /* for parametric signals */
        ""Rule1Operation"": "">"", ""<"", "">="", ""<="", ""="", ""!="", ""crosses above"", ""crosses below"",
        ""CrossOp"": ""OFF"", ""AND"", ""OR"", ""XOR"", ""IF"",
        ""Rule2Mode"": ""Signal"" or ""Value"",
        ""Rule2Operation"": ""operation""
      }}
    ],
    ""ExitSignals"": [ /* same structure as EntrySignals */ ],
    ""Settings"": {{
      ""Symbol"": ""symbol name"",
      ""Timeframe"": ""1D"",
      ""StartDate"": ""YYYY-MM-DD"",
      ""EndDate"": ""YYYY-MM-DD"",
      ""PositionSize"": 0.1,
      ""MaxPositions"": 10,
      ""MaxHoldDays"": 10,
      ""EntryMode"": ""Market"" or ""Limit"",
      ""ExitMode"": ""Market""
    }},
    ""ClarificationRequest"": null  /* Only include if information is missing/ambiguous */
  }}
}}

RULES:
1. You MUST only use signal IDs from the catalog above. Never invent new signals.
2. For parametric/comparison signals, use CatalogId ""parametric"" and set Rule1Mode, Rule1Operation, CrossOp appropriately.
3. All signal arguments must match the required args from the catalog.
4. If information is missing or ambiguous (especially Symbol, Timeframe, Dates), set ClarificationRequest with a clear question.
5. Use proper nesting - signals that require inputs must have them as Children.
6. Return ONLY valid JSON, no additional text.

Example: ""Buy when Close > 200 SMA""
{{
  ""Version"": ""1.0"",
  ""Strategy"": {{
    ""EntrySignals"": [
      {{
        ""CatalogId"": ""parametric"",
        ""Rule1Mode"": ""Signal"",
        ""Rule1Operation"": "">"",
        ""CrossOp"": ""OFF"",
        ""Args"": {{ ""Rule1 Base Offset"": 0, ""Rule1 Second Offset"": 0 }},
        ""Children"": [
          {{ ""CatalogId"": ""raw.close"", ""Args"": {{}}, ""Children"": [] }},
          {{
            ""CatalogId"": ""sma"",
            ""Args"": {{ ""Length"": 200 }},
            ""Children"": [
              {{ ""CatalogId"": ""raw.close"", ""Args"": {{}}, ""Children"": [] }}
            ]
          }}
        ]
      }}
    ],
    ""ExitSignals"": [],
    ""Settings"": null,
    ""ClarificationRequest"": ""What symbol should this strategy trade?""
  }}
}}";
        }

    }
}
