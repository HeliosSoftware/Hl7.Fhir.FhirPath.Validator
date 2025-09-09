# Hl7.Fhir.FhirPath.Validator - Windows Setup Guide

## Repository Analysis Summary

This repository contains a **FhirPath Static Analysis Tool** that validates FhirPath expressions against FHIR specifications. The project is built with .NET and includes:

- **Base Library**: Core validation logic (`Hl7.Fhir.Base.FhirPath.Validator`)
- **Version-Specific Libraries**: R4, R4B, and R5 implementations
- **Test Projects**: Comprehensive unit tests for validation functionality
- **Multi-Target Support**: .NET 9.0, 8.0, .NET Framework 4.6.2, .NET Standard 2.0/2.1

## Prerequisites

### 1. Install .NET SDK

You need .NET SDK 8.0 or later (preferably .NET 9.0 for full compatibility):

**Option A: Download from Microsoft**

1. Visit [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
2. Download and install .NET 9.0 SDK (or .NET 8.0 SDK minimum)

**Option B: Using Windows Package Manager (winget)**

```bash
winget install Microsoft.DotNet.SDK.9
```

**Option C: Using Chocolatey**

```bash
choco install dotnet-sdk
```

### 2. Verify Installation

Open Command Prompt or PowerShell and run:

```bash
dotnet --version
```

You should see version 8.0.x or 9.0.x

### 3. Install Visual Studio (Optional but Recommended)

- **Visual Studio 2022** (Community, Professional, or Enterprise)
- Or **Visual Studio Code** with C# extension

## Building the Project

### Method 1: Using .NET CLI (Recommended)

1. **Navigate to the project directory:**

```bash
cd "c:\Users\Doug\Desktop\Code\helios\Hl7.Fhir.FhirPath.Validator"
```

2. **Restore NuGet packages:**

```bash
dotnet restore src/Hl7.FhirPath.Validator.sln
```

3. **Build the solution:**

```bash
dotnet build src/Hl7.FhirPath.Validator.sln --configuration Release
```

4. **Run tests:**

```bash
dotnet test src/Hl7.FhirPath.Validator.sln --configuration Release
```

### Method 2: Using MSBuild (Alternative)

The repository includes a `test.bat` file, but it references an incorrect solution file. Use these corrected commands:

```bash
msbuild src\Hl7.FhirPath.Validator.sln /t:clean /v:minimal
msbuild src\Hl7.FhirPath.Validator.sln /t:restore /v:minimal
msbuild src\Hl7.FhirPath.Validator.sln /t:build /v:minimal
msbuild src\Hl7.FhirPath.Validator.sln /t:vstest /v:minimal
```

### Method 3: Using Visual Studio

1. Open `src/Hl7.FhirPath.Validator.sln` in Visual Studio
2. Right-click the solution → **Restore NuGet Packages**
3. Build → **Build Solution** (Ctrl+Shift+B)
4. Test → **Run All Tests** (Ctrl+R, A)

## Project Structure

```
src/
├── Hl7.Fhir.Base.FhirPath.Validator/          # Core validation library
├── Hl7.Fhir.R4.FhirPath.Validator/            # R4-specific implementation
├── Hl7.Fhir.R4B.FhirPath.Validator/           # R4B-specific implementation
├── Hl7.Fhir.R5.FhirPath.Validator/            # R5-specific implementation
├── Test.Fhir.R5.FhirPath.Validator/           # R5 unit tests
├── Test.FhirPath.Validator/                   # General unit tests
└── Hl7.FhirPath.Validator.sln                 # Solution file
```

## Key Dependencies

The project uses these major NuGet packages:

- **Hl7.Fhir.Conformance** (v5.12.2) - Core FHIR functionality
- **Hl7.Fhir.Specification.Data.R5** (v5.12.2) - R5 specification data
- **Microsoft.NET.Test.Sdk** - Testing framework
- **MSTest.TestAdapter/TestFramework** - Unit testing

## Configuration

### Environment Variables

The R5 unit tests can be configured using CLI arguments, environment variables, or will fall back to default values. The priority order is: **CLI Arguments** → **Environment Variables** → **Default Values**.

> **Note:** The default values assume that the [fhir-test-cases](https://github.com/FHIR/fhir-test-cases) repository is cloned at the same level as this project:
> ```
> parent-directory/
> ├── Hl7.Fhir.FhirPath.Validator/    (this project)
> └── fhir-test-cases/                (FHIR test cases repo)
>     └── r5/
>         ├── fhirpath/
>         │   └── tests-fhir-r5.xml
>         └── examples/
> ```

| CLI Argument              | Environment Variable    | Description                          | Default Value                                      |
| ------------------------- | ----------------------- | ------------------------------------ | -------------------------------------------------- |
| `--fhir-test-file`        | `FHIR_TEST_FILE`        | Path to the FHIR test cases XML file | `../fhir-test-cases/r5/fhirpath/tests-fhir-r5.xml` |
| `--fhir-test-base-path`   | `FHIR_TEST_BASE_PATH`   | Base path for FHIR test case files   | `../fhir-test-cases/r5/`                           |
| `--fhirpath-results-path` | `FHIRPATH_RESULTS_PATH` | Path to store test result JSON files | `./static/results`                                 |
| `--url`                   | `FHIR_VALIDATOR_URL`    | FHIRPath evaluation server URL       | `https://fhirpath.heliossoftware.com/r5`           |

### Using CLI Arguments (Highest Priority)

**Running tests with custom paths:**
```bash
dotnet test src/Hl7.FhirPath.Validator.sln -- --fhir-test-file "c:\your\custom\path\tests-fhir-r5.xml" --fhir-test-base-path "c:\your\custom\path\r5\" --fhirpath-results-path "c:\your\custom\path\results" --url "https://fhirpath.heliossoftware.com/r5"
```

**Running specific test project with CLI args:**
```bash
dotnet test src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj -- --fhir-test-file "c:\custom\tests.xml"
```

### Using Environment Variables (Medium Priority)

**Setting Environment Variables:**

**Windows Command Prompt:**
```cmd
set FHIR_TEST_FILE=c:\your\custom\path\tests-fhir-r5.xml
set FHIR_TEST_BASE_PATH=c:\your\custom\path\r5\
set FHIRPATH_RESULTS_PATH=c:\your\custom\path\results
set FHIR_VALIDATOR_URL=https://fhirpath.heliossoftware.com/r5
```

**Windows PowerShell:**
```powershell
$env:FHIR_TEST_FILE = "c:\your\custom\path\tests-fhir-r5.xml"
$env:FHIR_TEST_BASE_PATH = "c:\your\custom\path\r5\"
$env:FHIRPATH_RESULTS_PATH = "c:\your\custom\path\results"
$env:FHIR_VALIDATOR_URL = "https://fhirpath.heliossoftware.com/r5"
```

**System Environment Variables (Persistent):**
1. Open System Properties → Advanced → Environment Variables
2. Add new variables under "User variables" or "System variables"
3. Restart your IDE/command prompt to pick up the changes

## Running Tests

### Console Application (Recommended for CI/CD)

The test project can be run as a console application that returns proper exit codes for build pipelines:

```bash
# Build and run the console application
dotnet run --project src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj

# Or build first, then run the executable
dotnet build src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj
dotnet src/Test.Fhir.R5.FhirPath.Validator/bin/Debug/net80/Test.Fhir.R5.FhirPath.Validator.dll
```

**Exit Codes:**
- `0` - All tests passed (success)
- `1` - One or more tests failed
- `2` - Fatal error occurred

**With CLI Arguments:**
```bash
dotnet run --project src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj -- --fhir-test-file "path/to/tests.xml" --fhir-test-base-path "path/to/base/" --url "https://fhirpath.heliossoftware.com/r5"
```

**Help:**
```bash
dotnet run --project src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj -- --help
```

### Traditional MSTest Runner

```bash
dotnet test src/Hl7.FhirPath.Validator.sln
```

### Run Specific Test Project

```bash
dotnet test src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj
```

### Run Tests with Detailed Output

```bash
dotnet test src/Hl7.FhirPath.Validator.sln --logger "console;verbosity=detailed"
```

## Usage Examples

Based on the test files, here's how to use the validator:

### Basic Usage

```csharp
using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Model;

// Create a visitor for validation
var visitor = new FhirPathExpressionVisitor();
visitor.AddInputType(typeof(Patient));

// Parse and validate a FhirPath expression
string expression = "contact.telecom.where(use='phone').system";
var compiler = new FhirPathCompiler();
var parsedExpression = compiler.Parse(expression);

// Validate the expression
parsedExpression.Accept(visitor);

// Check results
if (visitor.Outcome.Success)
{
    Console.WriteLine("Expression is valid!");
}
else
{
    Console.WriteLine("Validation errors found:");
    Console.WriteLine(visitor.Outcome.ToXml());
}
```

## Troubleshooting

### Common Issues

1. **Build Errors Related to .NET Version**

   - Ensure you have .NET 8.0 or 9.0 SDK installed
   - Check `dotnet --version`

2. **NuGet Package Restore Issues**

   - Clear NuGet cache: `dotnet nuget locals all --clear`
   - Restore packages: `dotnet restore`

3. **Test Failures**

   - Some tests validate against HL7 FHIR specifications
   - Ensure internet connectivity for downloading specification data

4. **MSBuild Not Found**
   - Install Visual Studio Build Tools or full Visual Studio
   - Or use `dotnet build` instead of `msbuild`

### Performance Notes

- Initial build may take longer due to NuGet package downloads
- Test execution includes validation of R4B/R5 search parameters and invariants
- The project targets multiple frameworks, so build output will include multiple assemblies

## Development Environment Setup

For development work:

1. **Install Visual Studio 2022** with:

   - .NET desktop development workload
   - ASP.NET and web development workload

2. **Recommended Extensions:**

   - ReSharper or Visual Studio IntelliCode
   - NuGet Package Manager

3. **Git Configuration:**
   - The project uses Git Flow branching strategy
   - Submit pull requests to the develop branch

## CI/CD Integration

### GitHub Actions Example

```yaml
name: FHIR FhirPath Validation Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Checkout fhir-test-cases
      uses: actions/checkout@v4
      with:
        repository: FHIR/fhir-test-cases
        path: fhir-test-cases
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore src/Hl7.FhirPath.Validator.sln
    
    - name: Build
      run: dotnet build src/Hl7.FhirPath.Validator.sln --no-restore
    
    - name: Run FhirPath Validation Tests
      run: dotnet run --project src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj --no-build
```

### Azure DevOps Pipeline Example

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- checkout: self
- checkout: git://FHIR/fhir-test-cases@main

- script: dotnet restore src/Hl7.FhirPath.Validator.sln
  displayName: 'Restore packages'

- script: dotnet build src/Hl7.FhirPath.Validator.sln --no-restore
  displayName: 'Build solution'

- script: dotnet run --project src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj --no-build
  displayName: 'Run FhirPath validation tests'
```

## Next Steps

After successful setup:

1. Explore the unit tests in `Test.Fhir.R5.FhirPath.Validator/Hl7UnitTests.cs`
2. Review the core validation logic in `BaseFhirPathExpressionVisitor.cs`
3. Try validating your own FhirPath expressions using the examples above
4. Set up CI/CD integration using the examples above

## Support

- **Documentation**: See the main README.md
- **Issues**: Check the GitHub repository for known issues
- **Community**: .NET FHIR Implementers chat on [Zulip](https://chat.fhir.org/#narrow/stream/dotnet)
