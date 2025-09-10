using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Fhir.FhirPath.Validator;

namespace Test.Fhir.R5.FhirPath.Validator
{
    public class KnownFailure
    {
        public string groupName { get; set; }
        public string testName { get; set; }
        public string reason { get; set; }
    }

    public class KnownTestFailures
    {
        public string description { get; set; }
        public string version { get; set; }
        public List<KnownFailure> knownFailures { get; set; } = new List<KnownFailure>();
    }

    class Program
    {
        private static KnownTestFailures _knownFailures;

        static int Main(string[] args)
        {
            Console.WriteLine("FHIR R5 FhirPath Validator Test Runner");
            Console.WriteLine("======================================");
            
            try
            {
                // Parse command line arguments for test configuration
                ParseArguments(args);
                
                // Load known test failures
                LoadKnownTestFailures();
                
                // Initialize the test class
                var testInstance = new Hl7UnitTestFileR5();
                testInstance.Init();
                
                bool runServer = args.Contains("--server");
                
                Console.WriteLine($"Configuration:");
                Console.WriteLine($"  Test File: {TestConfiguration.FhirTestFile}");
                Console.WriteLine($"  Base Path: {TestConfiguration.FhirTestBasePath}");
                Console.WriteLine($"  Results Path: {TestConfiguration.FhirPathResultsPath}");
                Console.WriteLine($"  Server URL: {TestConfiguration.ServerUrl}");
                Console.WriteLine($"  Known Failures: {_knownFailures?.knownFailures?.Count ?? 0} defined");
                Console.WriteLine($"  Mode: {(runServer ? "Server evaluation (JSON will be written)" : "Local evaluation")}");
                Console.WriteLine();
                
                // Get all test data
                var testData = Hl7UnitTestFileR5.TestDataKeys.ToList();
                Console.WriteLine($"Found {testData.Count} test cases to execute.");
                Console.WriteLine();
                
                int passed = 0;
                int failed = 0;
                int skipped = 0;
                int knownFailures = 0;
                int knownFailuresFixed = 0;
                
                if (runServer)
                {
                    // Ensure the results directory exists so JSON can be written
                    System.IO.Directory.CreateDirectory(TestConfiguration.FhirPathResultsPath);
                    // Use the first configured server (currently Helios Software (R5))
                    var engineName = Hl7UnitTestFileR5.servers.First().EngineName;

                    foreach (var test in testData)
                    {
                        string groupName = (string)test[0];
                        string testName = (string)test[1];
                        
                        Console.Write($"Running {groupName}.{testName} (server)... ");
                        
                        try
                        {
                            // Execute server-based evaluation (this method records JSON via RecordResult)
                            testInstance.TestEvaluateOnServer(engineName, groupName, testName);
                            
                            if (IsKnownFailure(groupName, testName))
                            {
                                Console.WriteLine("PASSED (EXPECTED TO FAIL) - known failure resolved");
                                knownFailuresFixed++;
                            }
                            else
                            {
                                Console.WriteLine("PASSED");
                                passed++;
                            }
                        }
                        catch (AssertInconclusiveException ex)
                        {
                            Console.WriteLine($"SKIPPED - {ex.Message}");
                            skipped++;
                        }
                        catch (AssertFailedException ex)
                        {
                            if (IsKnownFailure(groupName, testName))
                            {
                                Console.WriteLine($"KNOWN FAILURE - {ex.Message}");
                                knownFailures++;
                            }
                            else
                            {
                                Console.WriteLine($"FAILED - {ex.Message}");
                                failed++;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (IsKnownFailure(groupName, testName))
                            {
                                Console.WriteLine($"KNOWN FAILURE - {ex.Message}");
                                knownFailures++;
                            }
                            else
                            {
                                Console.WriteLine($"ERROR - {ex.Message}");
                                failed++;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var test in testData)
                    {
                        string groupName = (string)test[0];
                        string testName = (string)test[1];
                        
                        Console.Write($"Running {groupName}.{testName}... ");
                        
                        try
                        {
                            // Run CheckStaticReturnTypes test
                            testInstance.CheckStaticReturnTypes(groupName, testName);
                            
                            // Run TestEvaluateExpression test
                            testInstance.TestEvaluateExpression(groupName, testName);
                            
                            if (IsKnownFailure(groupName, testName))
                            {
                                Console.WriteLine("PASSED (EXPECTED TO FAIL) - known failure resolved");
                                knownFailuresFixed++;
                            }
                            else
                            {
                                Console.WriteLine("PASSED");
                                passed++;
                            }
                        }
                        catch (AssertInconclusiveException ex)
                        {
                            Console.WriteLine($"SKIPPED - {ex.Message}");
                            skipped++;
                        }
                        catch (AssertFailedException ex)
                        {
                            if (IsKnownFailure(groupName, testName))
                            {
                                Console.WriteLine($"KNOWN FAILURE - {ex.Message}");
                                knownFailures++;
                            }
                            else
                            {
                                Console.WriteLine($"FAILED - {ex.Message}");
                                failed++;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (IsKnownFailure(groupName, testName))
                            {
                                Console.WriteLine($"KNOWN FAILURE - {ex.Message}");
                                knownFailures++;
                            }
                            else
                            {
                                Console.WriteLine($"ERROR - {ex.Message}");
                                failed++;
                            }
                        }
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("Test Results:");
                Console.WriteLine($"  Passed: {passed}");
                Console.WriteLine($"  Failed: {failed}");
                Console.WriteLine($"  Known Failures: {knownFailures}");
                Console.WriteLine($"  Known Failures Resolved: {knownFailuresFixed}");
                Console.WriteLine($"  Skipped: {skipped}");
                Console.WriteLine($"  Total: {testData.Count}");
                
                if (failed > 0 || knownFailuresFixed > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("❌ Tests FAILED");
                    if (failed > 0)
                        Console.WriteLine($"   {failed} unexpected failures occurred");
                    if (knownFailuresFixed > 0)
                        Console.WriteLine($"   {knownFailuresFixed} known-failure test(s) unexpectedly PASSED. Update known-test-failures.json.");
                    return 1; // Exit code 1 indicates failure
                }
                else
                {
                    Console.WriteLine();
                    if (knownFailures > 0)
                    {
                        Console.WriteLine($"✅ All tests PASSED ({knownFailures} known failures ignored)");
                    }
                    else
                    {
                        Console.WriteLine("✅ All tests PASSED");
                    }
                    return 0; // Exit code 0 indicates success
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return 2; // Exit code 2 indicates fatal error
            }
        }
        
        private static void LoadKnownTestFailures()
        {
            // Try multiple locations for known-test-failures.json
            string configPath = TestConfiguration.KnownFailuresFile;
            var possiblePaths = new List<string>();
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                possiblePaths.Add(Path.IsPathRooted(configPath)
                    ? configPath
                    : Path.Combine(Directory.GetCurrentDirectory(), configPath));
            }
            // Prioritize external usage patterns
            possiblePaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "known-test-failures.json"));
            possiblePaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "static", "known-test-failures.json"));
            possiblePaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "static", "known-test-failures.json"));
            // Check for sample file last (example in this repo)
            possiblePaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "static", "known-test-failures.sample.json"));
            
            string knownFailuresPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    knownFailuresPath = path;
                    break;
                }
            }
            
            if (knownFailuresPath != null)
            {
                try
                {
                    string json = File.ReadAllText(knownFailuresPath);
                    _knownFailures = JsonSerializer.Deserialize<KnownTestFailures>(json);
                    Console.WriteLine($"Loaded {_knownFailures?.knownFailures?.Count ?? 0} known test failures from {knownFailuresPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load known-test-failures.json: {ex.Message}");
                    _knownFailures = new KnownTestFailures();
                }
            }
            else
            {
                Console.WriteLine($"No known-test-failures.json found in any of the expected locations:");
                foreach (var path in possiblePaths)
                {
                    Console.WriteLine($"  - {path}");
                }
                _knownFailures = new KnownTestFailures();
            }
        }

        private static bool IsKnownFailure(string groupName, string testName)
        {
            if (_knownFailures?.knownFailures == null)
                return false;

            return _knownFailures.knownFailures.Any(kf => 
                (kf.groupName == groupName || kf.groupName == "*") &&
                (kf.testName == testName || kf.testName == "*"));
        }

        private static void ParseArguments(string[] args)
        {
            // Arguments are already parsed by TestConfiguration class
            // This method exists for potential future enhancements
            
            if (args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                Environment.Exit(0);
            }
        }
        
        private static void ShowHelp()
        {
            Console.WriteLine("FHIR R5 FhirPath Validator Test Runner");
            Console.WriteLine();
            Console.WriteLine("Usage: Test.Fhir.R5.FhirPath.Validator [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --fhir-test-file <path>        Path to the FHIR test cases XML file");
            Console.WriteLine("  --fhir-test-base-path <path>   Base path for FHIR test case files");
            Console.WriteLine("  --fhirpath-results-path <path> Path to store test result JSON files");
            Console.WriteLine("  --url <url>                    FHIRPath evaluation server URL for remote evaluation (TestEvaluateOnServer)");
            Console.WriteLine("  --server                       Run server-evaluation tests and write JSON results to FHIRPATH_RESULTS_PATH");
            Console.WriteLine("  --known-failures <path>         Path to known test failures JSON file (optional)");
            Console.WriteLine("  --help, -h                     Show this help message");
            Console.WriteLine();
            Console.WriteLine("Environment Variables:");
            Console.WriteLine("  FHIR_TEST_FILE                 Same as --fhir-test-file");
            Console.WriteLine("  FHIR_TEST_BASE_PATH            Same as --fhir-test-base-path");
            Console.WriteLine("  FHIRPATH_RESULTS_PATH          Same as --fhirpath-results-path");
            Console.WriteLine("  FHIR_VALIDATOR_URL             Same as --url");
            Console.WriteLine("  KNOWN_TEST_FAILURES_FILE       Same as --known-failures");
            Console.WriteLine();
            Console.WriteLine("Exit Codes:");
            Console.WriteLine("  0 - All tests passed");
            Console.WriteLine("  1 - One or more tests failed");
            Console.WriteLine("  2 - Fatal error occurred");
        }
    }
}
