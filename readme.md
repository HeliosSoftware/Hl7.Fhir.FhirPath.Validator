| FhirPath Static Analysis |
|---|

## Introduction ##

Many [FHIR][fhir-spec] resources contain [FhirPath][fhirpath-spec] expressions as string values that are to 
be use in a specific context, such as SearchParameters, StructureDefinitions, and Quesitonnaires.

The Firely SDK provides a FHIRPath engine for evaluating these expressions at runtime along with a parser/compiler.

This project provides a static analysis tool that can help ensure that a valid fhirpath expression (returned
by the Firely parser) is valid for the context in which it is to be used.

For example it could check that a specific custom search parameter was valid against the Patient resource.

The library contains:

* A visitor of the Firely Expression class returned by the FhirPath parser that can be used to verify the validity of the FhirPath expression.
* Unit test verifying all the R4B/R5 search expressions provided by the Firely SDK
* Unit test verifying all the R4B/R5 invariant expressions provided by the Hl7 SDK

Known Issues/incomplete funcitonality:
* missing functions: intersect, exclude, single, iif
* length() doesn't check that context is a string
* toChars() returns a string not string[]
* Math functions (argument checks)
	* log
	* power
	* round
* Comparisons don't check for type conversions, or that the types are compatible/same
	* though does identify that the resulting type is boolean for downstream processing
* Boolean logic operators should check that both sides are boolean type parameters
* Math operators
* Reflection
* Checking types of parameters to functions (not just return types and object mdel prop names)

> **Note:** Only reviewed up to section 6 in the specification


The library depends on several NuGet packages (notably):

* `Hl7.Fhir.Conformance` - contains the FhirPath Engine, Introspection, and base models
* *The version specific assemblies also leverage the `Hl7.Fhir.*` packages*
	* [R4][r4-spec], [R4B][r4b-spec], [R5][r5-spec]

## Getting Started ##

The best place to start is to look at the unit tests and the console application runner.

### Running Tests

The project includes a console application that can run all tests and return proper exit codes for CI/CD pipelines:

```bash
# Build and run the console application
dotnet run --project src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj

# With custom configuration
dotnet run --project src/Test.Fhir.R5.FhirPath.Validator/Test.Fhir.R5.FhirPath.Validator.csproj -- --fhir-test-file "path/to/tests.xml"
```

**Exit Codes:**
- `0` - All tests passed (success)
- `1` - One or more tests failed
- `2` - Fatal error occurred

### Known Test Failures

The test runner supports a `known-test-failures.json` file in the project root to ignore expected failures when determining overall success. This allows CI/CD pipelines to pass even when there are known issues being tracked.

Example `known-test-failures.json`:
```json
{
  "description": "Known test failures that should be ignored when determining overall test success",
  "version": "1.0",
  "knownFailures": [
    {
      "groupName": "PrecisionDecimal",
      "testName": "*",
      "reason": "Precision decimal handling not fully implemented",
      "issueUrl": "https://github.com/HeliosSoftware/Hl7.Fhir.FhirPath.Validator/issues/123"
    }
  ]
}
```

The test runner will:
- Mark matching failures as "KNOWN FAILURE" instead of "FAILED"
- Include known failures in the summary but not count them toward the exit code
- Return success (exit code 0) if only known failures occur

### Configuration

Tests can be configured via CLI arguments, environment variables, or defaults:

| CLI Argument | Environment Variable | Description | Default Value |
|-------------|---------------------|-------------|---------------|
| `--fhir-test-file` | `FHIR_TEST_FILE` | Path to the FHIR test cases XML file | `../fhir-test-cases/r5/fhirpath/tests-fhir-r5.xml` |
| `--fhir-test-base-path` | `FHIR_TEST_BASE_PATH` | Base path for FHIR test case files | `../fhir-test-cases/r5/` |
| `--fhirpath-results-path` | `FHIRPATH_RESULTS_PATH` | Path to store test result JSON files | `./static/results` |

To date the tests cover verifying the R4B and R5 Search Parameters and FhirPath expressions from the official FHIR test cases.

## Support ##
None officially.
For questions and broader discussions, we use the .NET FHIR Implementers chat on [Zulip][netapi-zulip].

## Contributing ##

I am welcoming any contributors!

If you want to participate in this project, we're using [Git Flow][nvie] for our branch management, so please submit your commits using pull requests no on the develop branches mentioned above!

### GIT branching strategy ###

- [NVIE](http://nvie.com/posts/a-successful-git-branching-model/)
- Or see: [Git workflow](https://www.atlassian.com/git/workflows#!workflow-gitflow)

[netapi-zulip]: https://chat.fhir.org/#narrow/stream/dotnet
[fhir-spec]: http://www.hl7.org/fhir
[r4-spec]: http://www.hl7.org/fhir/r4
[r4b-spec]: http://www.hl7.org/fhir/r4b
[r5-spec]: http://www.hl7.org/fhir/r5
[fhirpath-spec]: http://hl7.org/fhirpath/

### History ###
This project was created to help verify the validity of the fhirpath expressions
throughout the core HL7 specifications, however once working discovered that this
could also be relevant for others to perform the same style of checks in running systems,
such as servers wanting to check their own fhirpath expressions.
