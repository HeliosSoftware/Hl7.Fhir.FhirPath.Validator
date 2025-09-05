using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Fhir.FhirPath.Validator;

namespace Test.Fhir.R5.FhirPath.Validator
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("FHIR R5 FhirPath Validator Test Runner");
            Console.WriteLine("======================================");
            
            try
            {
                // Parse command line arguments for test configuration
                ParseArguments(args);
                
                // Initialize the test class
                var testInstance = new Hl7UnitTestFileR5();
                testInstance.Init();
                
                Console.WriteLine($"Configuration:");
                Console.WriteLine($"  Test File: {TestConfiguration.FhirTestFile}");
                Console.WriteLine($"  Base Path: {TestConfiguration.FhirTestBasePath}");
                Console.WriteLine($"  Results Path: {TestConfiguration.FhirPathResultsPath}");
                Console.WriteLine();
                
                // Get all test data
                var testData = Hl7UnitTestFileR5.TestDataKeys.ToList();
                Console.WriteLine($"Found {testData.Count} test cases to execute.");
                Console.WriteLine();
                
                int passed = 0;
                int failed = 0;
                int skipped = 0;
                
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
                        
                        Console.WriteLine("PASSED");
                        passed++;
                    }
                    catch (AssertInconclusiveException ex)
                    {
                        Console.WriteLine($"SKIPPED - {ex.Message}");
                        skipped++;
                    }
                    catch (AssertFailedException ex)
                    {
                        Console.WriteLine($"FAILED - {ex.Message}");
                        failed++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR - {ex.Message}");
                        failed++;
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("Test Results:");
                Console.WriteLine($"  Passed: {passed}");
                Console.WriteLine($"  Failed: {failed}");
                Console.WriteLine($"  Skipped: {skipped}");
                Console.WriteLine($"  Total: {testData.Count}");
                
                if (failed > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("❌ Tests FAILED");
                    return 1; // Exit code 1 indicates failure
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("✅ All tests PASSED");
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
            Console.WriteLine("  --help, -h                     Show this help message");
            Console.WriteLine();
            Console.WriteLine("Environment Variables:");
            Console.WriteLine("  FHIR_TEST_FILE                 Same as --fhir-test-file");
            Console.WriteLine("  FHIR_TEST_BASE_PATH            Same as --fhir-test-base-path");
            Console.WriteLine("  FHIRPATH_RESULTS_PATH          Same as --fhirpath-results-path");
            Console.WriteLine();
            Console.WriteLine("Exit Codes:");
            Console.WriteLine("  0 - All tests passed");
            Console.WriteLine("  1 - One or more tests failed");
            Console.WriteLine("  2 - Fatal error occurred");
        }
    }
}
