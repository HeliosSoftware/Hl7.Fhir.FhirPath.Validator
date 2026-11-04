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
	}
}
