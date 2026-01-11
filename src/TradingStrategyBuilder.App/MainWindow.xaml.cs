using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TradingStrategyBuilder.Core;
using Newtonsoft.Json;
using DotNetEnv;

namespace TradingStrategyBuilder.App
{
    public partial class MainWindow : Window
    {
        private StrategyBuilderService? _service;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize service (in production, API key should come from config/user input)
            InitializeService();
        }

        private void InitializeService()
        {
            try
            {
                // Find project root directory by looking for .sln file or .env file
                string? projectRoot = FindProjectRoot();
                string? apiKey = null;

                // Load .env file if project root found
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    var envPath = Path.Combine(projectRoot, ".env");
                    if (File.Exists(envPath))
                    {
                        Env.Load(envPath);
                        apiKey = Env.GetString("OPENAI_API_KEY", null);
                    }
                }

                // Fallback: Try environment variable if .env not found
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                }
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    StatusText.Text = "Warning: Set OPENAI_API_KEY in .env file or environment variable";
                    StatusText.Foreground = Brushes.Orange;
                }
                else
                {
                    _service = new StrategyBuilderService(apiKey);
                    StatusText.Text = "Ready - Service initialized";
                    StatusText.Foreground = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error initializing service: {ex.Message}";
                StatusText.Foreground = Brushes.Red;
            }
        }

        private string? FindProjectRoot()
        {
            // Start from the executable directory (bin/Debug/net8.0-windows or bin/Release/net8.0-windows)
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Go up directories to find project root (where .sln or .env file is located)
            var directory = new DirectoryInfo(currentDir);
            
            // Look for .sln file or .env file (project root indicators)
            while (directory != null)
            {
                // Check for .sln file (solution file indicates project root)
                var slnFiles = directory.GetFiles("*.sln");
                if (slnFiles.Length > 0)
                {
                    return directory.FullName;
                }

                // Check for .env file (if .env exists, we're at project root)
                var envFile = Path.Combine(directory.FullName, ".env");
                if (File.Exists(envFile))
                {
                    return directory.FullName;
                }

                // Go up one directory
                directory = directory.Parent;
            }

            // Fallback: Try current working directory
            var workingDir = Directory.GetCurrentDirectory();
            var workingDirEnv = Path.Combine(workingDir, ".env");
            if (File.Exists(workingDirEnv))
            {
                return workingDir;
            }

            return null;
        }

        private async void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            if (_service == null)
            {
                MessageBox.Show("Service not initialized. Please set OPENAI_API_KEY environment variable.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var input = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter a strategy description.", "Input Required", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BuildButton.IsEnabled = false;
            StatusText.Text = "Building strategy...";
            StatusText.Foreground = Brushes.Blue;

            try
            {
                var result = await _service.BuildStrategyAsync(input);

                // Display IR
                if (result.IR != null)
                {
                    IRTextBox.Text = JsonConvert.SerializeObject(result.IR, Formatting.Indented);
                }

                // Display validation results
                if (result.ValidationErrors != null && result.ValidationErrors.Count > 0)
                {
                    var validationText = "Validation Errors:\n\n";
                    foreach (var error in result.ValidationErrors)
                    {
                        validationText += $"{error.Path}: {error.Message}\n";
                    }
                    ValidationTextBox.Text = validationText;
                }
                else if (result.Success)
                {
                    ValidationTextBox.Text = "✓ Validation passed";
                }
                else if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    ValidationTextBox.Text = $"Error:\n\n{result.ErrorMessage}";
                }

                // Display compiled signals
                if (result.Success && result.EntrySignals != null)
                {
                    var signalsOutput = new
                    {
                        EntrySignals = result.EntrySignals,
                        ExitSignals = result.ExitSignals ?? new List<Newtonsoft.Json.Linq.JObject>(),
                        Settings = result.Settings
                    };
                    SignalsTextBox.Text = JsonConvert.SerializeObject(signalsOutput, Formatting.Indented);
                    StatusText.Text = "✓ Strategy built successfully";
                    StatusText.Foreground = Brushes.Green;
                }
                else if (!string.IsNullOrEmpty(result.ClarificationRequest))
                {
                    SignalsTextBox.Text = $"Clarification Needed:\n\n{result.ClarificationRequest}";
                    StatusText.Text = "Clarification requested";
                    StatusText.Foreground = Brushes.Orange;
                    ValidationTextBox.Text = $"User Input Needed:\n\n{result.ClarificationRequest}";
                }
                else
                {
                    var errorDetails = result.ErrorMessage ?? "Unknown error";
                    if (result.Exception != null)
                    {
                        errorDetails += $"\n\nException Type: {result.Exception.GetType().Name}";
                        if (result.Exception.InnerException != null)
                        {
                            errorDetails += $"\nInner Exception: {result.Exception.InnerException.Message}";
                        }
                    }
                    SignalsTextBox.Text = errorDetails;
                    StatusText.Text = "✗ Strategy build failed";
                    StatusText.Foreground = Brushes.Red;
                    ValidationTextBox.Text = $"Error:\n\n{errorDetails}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                StatusText.Foreground = Brushes.Red;
                SignalsTextBox.Text = $"Exception: {ex}\n\nStackTrace:\n{ex.StackTrace}";
                ValidationTextBox.Text = $"Error: {ex.Message}";
            }
            finally
            {
                BuildButton.IsEnabled = true;
            }
        }
    }
}
