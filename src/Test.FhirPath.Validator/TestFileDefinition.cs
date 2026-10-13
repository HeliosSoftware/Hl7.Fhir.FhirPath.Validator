using System.Collections.Generic;
using System.Xml.Serialization;

namespace Test.Fhir.R4B.FhirPath.Validator
{
    [XmlRoot(ElementName = "tests")]
    public class Tests
    {
        public string Notes { get; set; }

        [XmlElement(ElementName = "group")]
        public List<Group> Groups { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "mode")]
        public string Mode { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }

        [XmlAttribute(AttributeName = "reference")]
        public string Reference { get; set; }
    }

    public class Group
    {
        public string Notes { get; set; }

        [XmlElement(ElementName = "test")]
        public List<Test> Tests { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }

        [XmlAttribute(AttributeName = "reference")]
        public string Reference { get; set; }
    }

    public class Test
    {
        [XmlElement(ElementName = "expression")]
        public Expression Expression { get; set; }

        [XmlElement(ElementName = "output")]
        public List<Output> Outputs { get; set; }

        public string Notes { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }

        [XmlAttribute(AttributeName = "reference")]
        public string Reference { get; set; }

        [XmlAttribute(AttributeName = "inputfile")]
        public string InputFile { get; set; }

        /// <summary>Expression evaluated first; each result becomes the focus ($this) of the test expression.</summary>
        [XmlAttribute(AttributeName = "context")]
        public string Context { get; set; }

        /// <summary>Declared type(s) of that focus, '|'-separated (e.g. 'Period|Timing'), used for semantic checks.</summary>
        [XmlAttribute(AttributeName = "contextType")]
        public string ContextType { get; set; }

        [XmlAttribute(AttributeName = "mode")]
        public string Mode { get; set; }

        [XmlAttribute(AttributeName = "checkOrderedFunctions")]
        public bool CheckOrderedFunctions { get; set; }

        [XmlAttribute(AttributeName = "predicate")]
        public bool Predicate { get; set; }

        [XmlAttribute(AttributeName = "ordered")]
        public string Ordered { get; set; }
    }

    public class Expression
    {
        [XmlAttribute(AttributeName = "invalid")]
        public string Invalid { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    public class Output
    {
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    public enum OutputType
    {
        Boolean,
        Code,
        Date,
        DateTime,
        Decimal,
        Integer,
        Quantity,
        String,
        Time
    }

    public enum InvalidType
    {
        [XmlEnum("false")]
        False,
        Semantic,
        [XmlEnum("true")]
        True
    }
}
