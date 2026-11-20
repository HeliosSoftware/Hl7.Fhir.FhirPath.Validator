using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Nodes;

namespace Test.Fhir.R5.FhirPath.Validator
{
	[TestClass]
	public class DecimalLiteralPreserverTests
	{
		// Same shape as fhir-test-cases r5/parameters-example-decimals.xml.
		const string DecimalsXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Parameters xmlns=""http://hl7.org/fhir"">
	<id value=""example-decimals""/>
	<parameter>
		<name value=""exponent""/>
		<valueDecimal value=""9.5e2""/>
	</parameter>
	<parameter>
		<name value=""negative-exponent""/>
		<valueDecimal value=""1.5e-3""/>
	</parameter>
	<parameter>
		<name value=""negative-value""/>
		<valueDecimal value=""-9.5e2""/>
	</parameter>
	<parameter>
		<name value=""upper-case-exponent""/>
		<valueDecimal value=""1.5E2""/>
	</parameter>
	<parameter>
		<name value=""plain""/>
		<valueDecimal value=""1.50""/>
	</parameter>
</Parameters>";

		static Resource ParseXml(string xml)
		{
			var settings = new FhirXmlPocoDeserializerSettings() { Validator = null, ValidateOnFailedParse = false };
			return Test.Fhir.FhirPath.Validator.Hl7UnitTestFileR5.DeserializeXmlResource(new FhirXmlPocoDeserializer(settings), xml);
		}

		static string Request(Resource resource)
		{
			var parameters = new Parameters();
			parameters.Parameter.Add(new Parameters.ParameterComponent() { Name = "expression", Value = new FhirString("1") });
			parameters.Parameter.Add(new Parameters.ParameterComponent() { Name = "resource", Resource = resource });
			return parameters.ToJson();
		}

		static JsonArray InputParameters(string requestJson)
		{
			var request = JsonNode.Parse(requestJson);
			return request["parameter"][1]["resource"]["parameter"].AsArray();
		}

		[TestMethod]
		public void FirelyDropsTheExponent()
		{
			// Guards the premise: if Firely ever keeps the text, this whole class can go.
			var json = Request(ParseXml(DecimalsXml));
			Assert.AreEqual("950", InputParameters(json)[0]["valueDecimal"].ToJsonString());
		}

		[TestMethod]
		public void RestoresExponentLiteralsFromXml()
		{
			var json = DecimalLiteralPreserver.PatchRequest(Request(ParseXml(DecimalsXml)), DecimalsXml, false);
			var p = InputParameters(json);
			Assert.AreEqual("9.5e2", p[0]["valueDecimal"].ToJsonString());
			Assert.AreEqual("1.5e-3", p[1]["valueDecimal"].ToJsonString());
			Assert.AreEqual("-9.5e2", p[2]["valueDecimal"].ToJsonString());
			Assert.AreEqual("1.5E2", p[3]["valueDecimal"].ToJsonString());
			Assert.AreEqual("1.50", p[4]["valueDecimal"].ToJsonString());
			// Names and the rest of the request are untouched.
			Assert.AreEqual("\"exponent\"", p[0]["name"].ToJsonString());
			Assert.AreEqual("\"1\"", JsonNode.Parse(json)["parameter"][0]["valueString"].ToJsonString());
		}

		[TestMethod]
		public void RestoresExponentLiteralsFromJson()
		{
			const string source = @"{""resourceType"":""Parameters"",""parameter"":[{""name"":""exponent"",""valueDecimal"":9.5e2}]}";
			var resource = new FhirJsonPocoDeserializer().DeserializeResource(source);
			var json = DecimalLiteralPreserver.PatchRequest(Request(resource), source, true);
			Assert.AreEqual("9.5e2", InputParameters(json)[0]["valueDecimal"].ToJsonString());
		}

		[TestMethod]
		public void RestoresInsideContainedResources()
		{
			const string xml = @"<Patient xmlns=""http://hl7.org/fhir"">
	<contained><Observation><status value=""final""/><code><text value=""x""/></code><valueQuantity><value value=""9.5e2""/></valueQuantity></Observation></contained>
	<contained><Observation><status value=""final""/><code><text value=""y""/></code><valueQuantity><value value=""1.5E2""/></valueQuantity></Observation></contained>
</Patient>";
			var json = DecimalLiteralPreserver.PatchRequest(Request(ParseXml(xml)), xml, false);
			var contained = JsonNode.Parse(json)["parameter"][1]["resource"]["contained"].AsArray();
			Assert.AreEqual("9.5e2", contained[0]["valueQuantity"]["value"].ToJsonString());
			Assert.AreEqual("1.5E2", contained[1]["valueQuantity"]["value"].ToJsonString());
		}

		[TestMethod]
		public void LeavesValuesThatDoNotMatchTheSource()
		{
			// If the two trees ever pair up the wrong elements, a value must not change.
			var source = new JsonObject { ["resourceType"] = "Parameters", ["x"] = 950 };
			Assert.IsFalse(DecimalLiteralPreserver.PatchResource(source, @"<Parameters xmlns=""http://hl7.org/fhir""><x value=""9.6e2""/></Parameters>", false));
			Assert.AreEqual("950", source["x"].ToJsonString());
		}

		[TestMethod]
		public void UnparseableSourceIsIgnored()
		{
			var request = Request(ParseXml(DecimalsXml));
			Assert.AreEqual(request, DecimalLiteralPreserver.PatchRequest(request, "<not xml", false));
		}

		// Same shape as fhir-test-cases r5/parameters-example-large-exponents.xml (2026-11-20).
		const string LargeExponentsXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Parameters xmlns=""http://hl7.org/fhir"">
	<id value=""example-large-exponents""/>
	<parameter>
		<name value=""huge-quantity""/>
		<valueQuantity>
			<value value=""1e999999999""/>
			<unit value=""kg""/>
			<system value=""http://unitsofmeasure.org""/>
			<code value=""kg""/>
		</valueQuantity>
	</parameter>
	<parameter>
		<name value=""tiny-quantity""/>
		<valueQuantity>
			<value value=""1e-999999999""/>
			<unit value=""kg""/>
		</valueQuantity>
	</parameter>
	<parameter>
		<name value=""huge-decimal""/>
		<valueDecimal value=""1e999999999""/>
	</parameter>
	<parameter>
		<name value=""tiny-decimal""/>
		<valueDecimal value=""1e-999999999""/>
	</parameter>
</Parameters>";

		static Resource ParseTestInput(string content, string inputFile)
		{
			var xmlSettings = new FhirXmlPocoDeserializerSettings() { Validator = null, AnnotateResourceParseExceptions = true, ValidateOnFailedParse = false };
			var jsonSettings = new FhirJsonPocoDeserializerSettings() { Validator = null, AnnotateResourceParseExceptions = true, ValidateOnFailedParse = false };
			return Test.Fhir.FhirPath.Validator.Hl7UnitTestFileR5.DeserializeTestInput(
				new FhirJsonPocoDeserializer(jsonSettings), new FhirXmlPocoDeserializer(xmlSettings), content, inputFile);
		}

		[TestMethod]
		public void AHugeExponentDoesNotAbortTheInputFile()
		{
			var resource = ParseTestInput(LargeExponentsXml, "parameters-example-large-exponents.xml");
			Assert.AreEqual("Parameters", resource.TypeName);
			Assert.AreEqual(4, ((Parameters)resource).Parameter.Count);
		}

		[TestMethod]
		public void AHugeExponentIsSentToTheServerAsWritten()
		{
			var resource = ParseTestInput(LargeExponentsXml, "parameters-example-large-exponents.xml");
			var patched = DecimalLiteralPreserver.PatchRequest(Request(resource), LargeExponentsXml, false);
			var p = InputParameters(patched);
			Assert.AreEqual("1e999999999", p[0]["valueQuantity"]["value"].ToJsonString());
			Assert.AreEqual("\"kg\"", p[0]["valueQuantity"]["unit"].ToJsonString());
			Assert.AreEqual("1e-999999999", p[1]["valueQuantity"]["value"].ToJsonString());
			Assert.AreEqual("1e999999999", p[2]["valueDecimal"].ToJsonString());
			Assert.AreEqual("1e-999999999", p[3]["valueDecimal"].ToJsonString());
		}

		[TestMethod]
		public void AHugeExponentInJsonInputIsSentAsWritten()
		{
			const string json = @"{""resourceType"":""Parameters"",""parameter"":[{""name"":""huge"",""valueDecimal"":1e999999999},{""name"":""plain"",""valueDecimal"":1.50}]}";
			var resource = ParseTestInput(json, "parameters-example-large-exponents.json");
			var patched = DecimalLiteralPreserver.PatchRequest(Request(resource), json, true);
			var p = InputParameters(patched);
			Assert.AreEqual("1e999999999", p[0]["valueDecimal"].ToJsonString());
			Assert.AreEqual("1.50", p[1]["valueDecimal"].ToJsonString());
		}

		[TestMethod]
		public void OnlyUnrepresentableNumbersAreRestored()
		{
			Assert.IsNull(DecimalLiteralPreserver.Unrepresentable("1.5"));
			Assert.IsNull(DecimalLiteralPreserver.Unrepresentable("abc"));
			Assert.IsNull(DecimalLiteralPreserver.Unrepresentable(""));
			Assert.AreEqual("1e999999999", DecimalLiteralPreserver.Unrepresentable("1e999999999").ToJsonString());
		}
	}
}
