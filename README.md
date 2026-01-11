# Trading Strategy Builder - Phase 1 Proof of Concept

A deterministic LLM assistant for building trading strategies using a structured Intermediate Representation (IR) approach.

## Architecture Overview

This Phase 1 POC demonstrates a scalable architecture for translating natural language trading strategies into executable signal trees:

```
Natural Language → IR → Validation → Compilation → Signal Objects
```

### Key Components

1. **Capability Catalog** (`TradingStrategyBuilder.Core/Catalog/`)
   - Curated, authoritative registry of available signals
   - Stable identifiers and metadata
   - Representative subset of signals that demonstrates scalability

2. **Intermediate Representation (IR)** (`TradingStrategyBuilder.Core/IR/`)
   - Small, versioned JSON structure
   - Optimized for LLM reasoning
   - References catalog entries instead of raw schemas

3. **Validation Layer** (`TradingStrategyBuilder.Core/Validation/`)
   - Strict validation of IR against catalog
   - Enforces structure, parameters, and composition rules
   - Fails deterministically on invalid input

4. **Compilation** (`TradingStrategyBuilder.Core/Compilation/`)
   - Converts validated IR to existing Signal object structure
   - Maintains compatibility with current system

5. **LLM Integration** (`TradingStrategyBuilder.Core/LLM/`)
   - OpenAI API integration (or compatible)
   - Translates natural language to IR
   - Uses compact catalog registry in prompts

6. **WPF Application** (`TradingStrategyBuilder.App/`)
   - Basic UI for testing the system
   - Displays IR, validation results, and compiled signals

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- OpenAI API key (set in `.env` file or environment variable `OPENAI_API_KEY`)

### Building

```bash
dotnet restore
dotnet build
```

### Running

**⚠️ Important: Run the App project, NOT the Core project!**

**Quick Start:**
1. Create a `.env` file in the project root:
   ```
   OPENAI_API_KEY=your-api-key-here
   ```
   (See `.env.example` for template)

2. Run the **App** project (not Core):
   
   **From Visual Studio:**
   - Set `TradingStrategyBuilder.App` as Startup Project
   - Press **F5** or click **Start**
   - ✅ The `.env` file loads automatically!
   
   **From Command Line:**
   ```bash
   dotnet run --project src/TradingStrategyBuilder.App
   ```

   **Note**: `TradingStrategyBuilder.Core` is a class library and cannot be run directly.
   
   The application automatically loads the `.env` file when you run it from Visual Studio or command line.

**For detailed step-by-step instructions, see [QUICK_START.md](QUICK_START.md)**  
**If you get "Class Library cannot be started" error, see [HOW_TO_RUN.md](HOW_TO_RUN.md)**

### Configuration

The application requires an OpenAI API key. Set it via:
- Environment variable: `OPENAI_API_KEY`
- Or modify `MainWindow.xaml.cs` to prompt for it

## Usage

1. Enter a natural language trading strategy description in the input box
2. Click "Build Strategy"
3. View results in the tabs:
   - **IR**: Intermediate Representation (LLM output)
   - **Compiled Signals**: JSON structure compatible with existing system
   - **Validation**: Validation results and errors

### Example Inputs

- "Buy when Close > 200 SMA"
- "Buy when RSI(14) < 30 and price is above 50 EMA"
- "Buy when Close > Highest(20) and exit after 10 days"

## Phase 1 Scope

This is a proof-of-concept demonstrating:
- ✅ Capability Catalog architecture
- ✅ IR structure and versioning
- ✅ Validation pipeline
- ✅ Compilation to existing signal format
- ✅ End-to-end flow from natural language to signals

**Not included in Phase 1:**
- Full coverage of all 8,000 signals
- Production-ready error handling
- Full strategy settings mapping
- Advanced clarification logic
- UI polish

## Project Structure

```
TradingStrategyBuilder/
├── src/
│   ├── TradingStrategyBuilder.Core/        # Core library
│   │   ├── Catalog/                        # Capability Catalog
│   │   ├── IR/                            # Intermediate Representation
│   │   ├── Validation/                    # Validation layer
│   │   ├── Compilation/                   # IR to Signal compiler
│   │   └── LLM/                           # LLM integration
│   └── TradingStrategyBuilder.App/         # WPF application
└── README.md
```

## Design Principles

1. **Determinism**: System fails deterministically rather than guessing
2. **Validation**: Strict validation before compilation
3. **Scalability**: Catalog-based approach scales to 8,000+ signals
4. **Separation of Concerns**: LLM only translates, validation/compilation handle logic
5. **Versioning**: IR is versioned for future compatibility

## Next Steps (Post Phase 1)

- Expand catalog to include more signals
- Enhanced clarification logic
- More sophisticated IR structure
- Full strategy settings support
- Integration with existing strategy execution system
- Performance optimizations
- Comprehensive error handling

## License

Internal use only.
