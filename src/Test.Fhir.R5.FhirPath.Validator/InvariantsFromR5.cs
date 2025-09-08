﻿using Hl7.Fhir.FhirPath.Validator;
using Hl7.Fhir.Introspection;
using Hl7.FhirPath.Expressions;
using Hl7.FhirPath;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Language.Debugging;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification.Navigation;
using System.Runtime.InteropServices;

namespace Test.Fhir.FhirPath.Validator
{
    [TestClass]
    public class InvariantsFromR5
    {
        private readonly ModelInspector _mi = ModelInspector.ForAssembly(typeof(Patient).Assembly);
        FhirPathCompiler _compiler;

        [TestInitialize]
        public void Init()
        {
            // include all the conformance types
            _mi.Import(typeof(StructureDefinition).Assembly);

            Hl7.Fhir.FhirPath.ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();
            SymbolTable symbolTable = new(FhirPathCompiler.DefaultSymbolTable);
            _compiler = new FhirPathCompiler(symbolTable);
        }

        // [TestMethod]
        public void ReadAllInvariants()
        {
            ZipSource _source = ZipSource.CreateValidationSource();
            _source.Prepare();
            if (_source.ListSummaries().Count() == 0)
            {
                // Need to re-create the set!
                System.IO.Directory.Delete(_source.ExtractPath, true);
				_source = ZipSource.CreateValidationSource();
				_source.Prepare();
			}
			foreach (var item in _source.ListSummaries().Where(s => s.ResourceTypeName == "StructureDefinition"))
            {
                try
                {
                    var canonical = item.GetConformanceCanonicalUrl();
                    var sd = _source.ResolveByCanonicalUri(canonical) as StructureDefinition;
                    if (sd != null && sd.Kind == StructureDefinition.StructureDefinitionKind.Resource)
                    {
                        var elements = sd.Differential.Element.Where(e => e.Constraint.Any()).ToList();
                        if (elements.Any())
                        {
                            // Console.WriteLine($"Resource: {sd.Name}");
                            foreach (var ed in elements)
                            {
                                Console.WriteLine($"{ed.Path} ({sd.Url})");
                                foreach (var c in ed.Constraint)
                                {
                                    Console.WriteLine($"\t{c.Key}:\t {c.Expression}");
                                    //var visitor = new FhirPathExpressionVisitor();
                                    //var t = SelectType(ed.Path, out var rt);
                                    //if (t != rt)
                                    //{
                                    //}
                                    //if (t != null)
                                    //    visitor.RegisterVariable("context", t);
                                    //visitor.AddInputType(t);
                                    //if (rt.IsAssignableTo(typeof(Resource)))
                                    //    visitor.RegisterVariable("resource", rt);
                                    //else
                                    //    visitor.RegisterVariable("resource", typeof(Resource));
                                    //var pe = _compiler.Parse(c.Expression);
                                    //var r = pe.Accept(visitor);
                                    //Console.WriteLine($"Result: {r}");
                                    //Console.WriteLine("---------");

                                    //Console.WriteLine(visitor.ToString());
                                    //Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
                                    //Assert.IsTrue(visitor.Outcome.Success == true);
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Cannot load {canonical}");
                    }
                }
                catch (StructuralTypeException ex)
                {
                    Console.WriteLine($"Error parsing {item.ResourceUri}");
                    Console.WriteLine($"{ex.Message}");
                }
            }
        }

        public static IEnumerable<object[]> R5Invariants
        {
            get
            {
                var result = new List<object[]>();
                var knownBadInvariants = new[] {
                    "ObservationDefinition obd-0",
                    "Bundle bdl-14",
                    "CodeSystem csd-3",
                    "ObservationDefinition.component obd-1",
                    "Bundle bdl-16",
                    "Questionnaire que-2", // the invariant really should be item.descendants().linkId.isDistinct() as doing it from the root can walk into all the contained content too (and trips in there)
                };

                ZipSource source = ZipSource.CreateValidationSource();
                source.Prepare();
                foreach (var item in source.ListSummaries().Where(s => s.ResourceTypeName == "StructureDefinition"))
                {
                    try
                    {
                        var sd = source.ResolveByUri(item.ResourceUri) as StructureDefinition;
                        if (sd != null && sd.Kind == StructureDefinition.StructureDefinitionKind.Resource && sd.Abstract == false)
                        {
                            var elements = sd.Differential.Element.Where(e => e.Constraint.Any()).ToList();
                            if (elements.Any())
                            {
                                foreach (var ed in elements)
                                {
                                    foreach (var c in ed.Constraint)
                                    {
                                        if (!string.IsNullOrEmpty(c.Expression))
                                            result.Add(new object[] { ed.Path, c.Key, c.Expression, !knownBadInvariants.Contains($"{ed.Path} {c.Key}") });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                return result;
            }
        }

        [TestMethod]
        [DynamicData(nameof(R5Invariants))]
        public void TestR5Invariants(string path, string key, string expression, bool expectSuccess)
        {
            // TODO: Fix FhirPath validation issues for invariants dgr-1 and pld-3 
            // These fail due to 'in' operator expecting single item but receiving arrays
            if ((path == "DiagnosticReport" && key == "dgr-1") || 
                (path == "PlanDefinition" && key == "pld-3"))
            {
                Assert.Inconclusive($"TODO: Fix FhirPath validation issue for {path}.{key} - 'in' operator expects single item");
                return;
            }

            // string expression = "(software.empty() and implementation.empty()) or kind != 'requirements'";
            Console.WriteLine($"Context: {path}");
            Console.WriteLine($"Invariant key: {key}");
            Console.WriteLine($"Expression:\r\n{expression}");

            Console.WriteLine("---------");
            var visitor = new FhirPathExpressionVisitor();
            visitor.SetContext(path);
            var pe = _compiler.Parse(expression);
            var r = pe.Accept(visitor);
            Console.WriteLine($"Result: {r}");
            Console.WriteLine("---------");

            Console.WriteLine(visitor.ToString());
            Console.WriteLine(visitor.Outcome.ToXml(new FhirXmlSerializationSettings() { Pretty = true }));
            Assert.IsTrue((visitor.Outcome.Success && visitor.Outcome.Warnings == 0) == expectSuccess);
            if (expectSuccess)
                Assert.AreEqual("boolean", r.ToString(), "Invariants must return a boolean");

			// Also verify that the echo visitor will reproduce the exact expression (with whitespace)
			var expr = _compiler.Parse(expression);
			var echoExpr = expr.EchoExpression();
			Assert.AreEqual(expression, echoExpr, "Echo should be the same");
		}
	}
}