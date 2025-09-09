using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Hl7.Fhir.FhirPath.Validator
{
    public class FhirPathExpressionVisitor : ExtensionResolvingFhirPathExpressionVisitor
    {
        public FhirPathExpressionVisitor()
            : base(CreateDefaultResolver(), ModelInspector.ForAssembly(typeof(Patient).Assembly),
                  Hl7.Fhir.Model.ModelInfo.SupportedResources,
                  Hl7.Fhir.Model.ModelInfo.OpenTypes)
        {
        }

        private static IResourceResolver CreateDefaultResolver()
        {
            try
            {
                var disable = Environment.GetEnvironmentVariable("FHIR_DISABLE_SPEC_ZIP");
                if (!string.IsNullOrEmpty(disable) && (disable == "1" || disable.Equals("true", StringComparison.OrdinalIgnoreCase)))
                {
                    return new InMemoryResourceResolver();
                }

                return new CachedResolver(
                    new MultiResolver(
                        ZipSource.CreateValidationSource()
                    )
                );
            }
            catch
            {
                // Fallback when specification.zip is not available in the runtime environment
                return new InMemoryResourceResolver();
            }
        }

        public FhirPathExpressionVisitor(IResourceResolver source)
            : base(source, ModelInspector.ForAssembly(typeof(Patient).Assembly),
                  Hl7.Fhir.Model.ModelInfo.SupportedResources,
                  Hl7.Fhir.Model.ModelInfo.OpenTypes)
        {
        }
    }
}
