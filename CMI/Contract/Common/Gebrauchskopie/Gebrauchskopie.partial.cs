using System.Xml.Serialization;

namespace CMI.Contract.Common.Gebrauchskopie
{
    public partial class Paket
    {
        [XmlAttribute(AttributeName = "schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string SchemaLocation { get; set; }
    }
}