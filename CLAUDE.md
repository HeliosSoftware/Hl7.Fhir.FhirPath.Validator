# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

### Build Commands
```bash
# Clean, restore, build and test (full cycle)
dotnet clean src/Hl7.FhirPath.Validator.sln
dotnet restore src/Hl7.FhirPath.Validator.sln
dotnet build src/Hl7.FhirPath.Validator.sln
dotnet test src/Hl7.FhirPath.Validator.sln

# Run a single test
dotnet test src/Test.Fhir.R5.FhirPath.Validator --filter "FullyQualifiedName~TestName"

# Build specific project
dotnet build src/Hl7.Fhir.R5.FhirPath.Validator/Hl7.Fhir.R5.FhirPath.Validator.csproj
```

### NuGet Package Creation
Debug builds automatically generate NuGet packages in the bin/Debug folder with prefix `brianpos.Fhir.*`.

## Architecture Overview

This is a **FHIRPath expression static validator** that validates FHIRPath expressions used in FHIR contexts without executing them. It uses the **Visitor Pattern** to traverse parsed expression trees and perform type checking.

### Project Structure
- **Base Library** (`Hl7.Fhir.Base.FhirPath.Validator`): Core validation logic, version-agnostic
- **Version-Specific Libraries**: Thin wrappers for R4, R4B, and R5 FHIR versions
- **Test Projects**: MSTest-based tests for R4B and R5 validators

### Key Classes
- **BaseFhirPathExpressionVisitor**: Main validation engine that traverses expression trees
- **FhirPathVisitorProps**: Type information propagated through expressions
- **SymbolTable**: Registry of FHIRPath functions and their validation rules
- **SearchExpressionValidator**: Specialized validator for FHIR search parameters
- **ExtensionResolvingFhirPathExpressionVisitor**: Handles FHIR extension validation

### Validation Flow
1. Parse FHIRPath expression using `FhirPathCompiler`
2. Set context type (e.g., Patient, Observation)
3. Accept visitor on parsed expression
4. Retrieve validation results from `visitor.Outcome` (OperationOutcome resource)

### Entry Points
```csharp
// Basic validation
var visitor = new FhirPathExpressionVisitor();
visitor.AddInputType(typeof(Patient));
var expression = compiler.Parse("name.given");
expression.Accept(visitor);
var outcome = visitor.Outcome;

// Search parameter validation
var validator = new SearchExpressionValidator(...);
var issues = validator.Validate(type, code, expression, searchType, url);
```

### Important Variables
The validator recognizes FHIR-specific variables:
- `%resource` - Current resource being validated
- `%rootResource` - Root resource in nested contexts
- `%context` - Current context in expressions

When modifying the validator, ensure compatibility across all supported FHIR versions (R4, R4B, R5).