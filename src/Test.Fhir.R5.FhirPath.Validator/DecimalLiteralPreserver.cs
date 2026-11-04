using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Test.Fhir.R5.FhirPath.Validator
{
	/// <summary>
	/// Puts the source file's decimal text back into JSON that Firely re-serialized.
	/// </summary>
	/// <remarks>
	/// The test input is parsed into a Firely POCO and written out again with
	/// <c>ToJson()</c> before it is POSTed to the server. A .NET <see cref="decimal"/>
	/// cannot hold a negative scale, so <c>9.5e2</c> goes out as <c>950</c> and the
	/// precision in the mantissa is lost. The lowBoundary/highBoundary tests on
	/// <c>parameters-example-decimals.xml</c> depend on that precision
	/// (HeliosSoftware/hfs#2173).
	///
	/// This walks the source (XML or JSON) side by side with the serialized JSON and,
	/// wherever a JSON number is numerically equal to the source text but spelled
	/// differently, swaps in the source text. A value that does not match is left
	/// alone, so a structural mismatch between the two trees can never change a value.
	/// </remarks>
	public static class DecimalLiteralPreserver
	{
		static readonly XNamespace Fhir = "http://hl7.org/fhir";

		/// <summary>
		/// Patch the <c>resource</c> parameter of a fhirpath-lab request (a serialized
		/// <c>Parameters</c>) from the original test input text.
		/// </summary>
		public static string PatchRequest(string requestJson, string sourceContent, bool sourceIsJson)
		{
			if (string.IsNullOrEmpty(sourceContent))
				return requestJson;
			var request = JsonNode.Parse(requestJson) as JsonObject;
			var resource = (request?["parameter"] as JsonArray)?
				.OfType<JsonObject>()
				.FirstOrDefault(p => (p["name"] as JsonValue)?.GetValue<string>() == "resource")?
				["resource"] as JsonObject;
			if (resource == null)
				return requestJson;
			if (!PatchResource(resource, sourceContent, sourceIsJson))
				return requestJson;
			return request.ToJsonString();
		}

		/// <summary>Patch a serialized resource in place. Returns true if anything changed.</summary>
		public static bool PatchResource(JsonObject resource, string sourceContent, bool sourceIsJson)
		{
			try
			{
				if (sourceIsJson)
				{
					using var doc = JsonDocument.Parse(sourceContent);
					return WalkJson(doc.RootElement, resource);
				}
				return WalkXml(XDocument.Parse(sourceContent).Root, resource);
			}
			catch (Exception ex) when (ex is JsonException || ex is System.Xml.XmlException)
			{
				// The input already parsed once through Firely; if it does not parse
				// here, send what Firely produced rather than fail the test.
				return false;
			}
		}

		// ---- JSON source ------------------------------------------------------------

		static bool WalkJson(JsonElement source, JsonNode target)
		{
			bool changed = false;
			switch (target)
			{
				case JsonObject obj when source.ValueKind == JsonValueKind.Object:
					foreach (var name in obj.Select(p => p.Key).ToList())
					{
						if (!source.TryGetProperty(name, out var s))
							continue;
						var replacement = Replacement(s, obj[name]);
						if (replacement != null) { obj[name] = replacement; changed = true; }
						else if (obj[name] != null) changed |= WalkJson(s, obj[name]);
					}
					break;
				case JsonArray arr when source.ValueKind == JsonValueKind.Array:
					for (int i = 0; i < arr.Count && i < source.GetArrayLength(); i++)
					{
						var s = source[i];
						var replacement = Replacement(s, arr[i]);
						if (replacement != null) { arr[i] = replacement; changed = true; }
						else if (arr[i] != null) changed |= WalkJson(s, arr[i]);
					}
					break;
			}
			return changed;
		}

		static JsonNode Replacement(JsonElement source, JsonNode target)
		{
			if (source.ValueKind != JsonValueKind.Number)
				return null;
			return Replacement(source.GetRawText(), target);
		}

		// ---- XML source -------------------------------------------------------------

		static bool WalkXml(XElement source, JsonObject target)
		{
			if (source == null || target == null)
				return false;

			// A resource in JSON is one object carrying resourceType; in XML it is wrapped
			// in an element of that name (inside contained / resource / outcome ...).
			var resourceType = (target["resourceType"] as JsonValue)?.GetValue<string>();
			if (resourceType != null && source.Name != Fhir + resourceType)
			{
				var inner = source.Elements(Fhir + resourceType).FirstOrDefault();
				if (inner == null)
					return false;
				source = inner;
			}

			bool changed = false;
			// Repeating elements are siblings in XML and one array in JSON, in the same order.
			foreach (var group in source.Elements().Where(e => e.Name.Namespace == Fhir).GroupBy(e => e.Name.LocalName))
			{
				var elements = group.ToList();
				changed |= PairXml(elements, target, group.Key);
				// A primitive's id/extensions go in the "_name" sibling.
				changed |= PairXml(elements, target, "_" + group.Key);
			}
			return changed;
		}

		static bool PairXml(System.Collections.Generic.List<XElement> elements, JsonObject parent, string name)
		{
			var node = parent[name];
			if (node is JsonArray arr)
			{
				bool changed = false;
				for (int i = 0; i < arr.Count && i < elements.Count; i++)
				{
					var replacement = Replacement(elements[i], arr[i]);
					if (replacement != null) { arr[i] = replacement; changed = true; }
					else if (arr[i] is JsonObject o) changed |= WalkXml(elements[i], o);
				}
				return changed;
			}
			if (elements.Count != 1 || node == null)
				return false;
			var single = Replacement(elements[0], node);
			if (single != null) { parent[name] = single; return true; }
			return node is JsonObject obj && WalkXml(elements[0], obj);
		}

		static JsonNode Replacement(XElement source, JsonNode target)
		{
			var raw = source.Attribute("value")?.Value;
			return raw == null ? null : Replacement(raw.Trim(), target);
		}

		// ---- shared -----------------------------------------------------------------

		/// <summary>
		/// The source text as a JSON number, if the target is a JSON number with the same
		/// value but different text; otherwise null.
		/// </summary>
		static JsonNode Replacement(string raw, JsonNode target)
		{
			if (target is not JsonValue value || value.GetValueKind() != JsonValueKind.Number)
				return null;
			var current = value.ToJsonString();
			if (current == raw)
				return null;
			if (!decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var rawValue)
				|| !decimal.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out var currentValue)
				|| rawValue != currentValue)
				return null;
			try
			{
				// JsonElement keeps the literal's text when written back out.
				using var doc = JsonDocument.Parse(raw);
				if (doc.RootElement.ValueKind != JsonValueKind.Number)
					return null;
				return JsonValue.Create(doc.RootElement.Clone());
			}
			catch (JsonException)
			{
				// Valid FHIR decimal text is valid JSON number text; anything else stays as is.
				return null;
			}
		}
	}
}
