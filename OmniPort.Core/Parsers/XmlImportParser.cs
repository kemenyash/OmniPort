using OmniPort.Core.Interfaces;
using System.Xml;

namespace OmniPort.Core.Parsers
{
    public class XmlImportParser : IImportParser
    {
        private readonly string recordNodeName;

        public XmlImportParser(string recordNodeName)
        {
            this.recordNodeName = recordNodeName;
        }

        public IEnumerable<IDictionary<string, object?>> Parse(Stream stream)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(stream);

            XmlNodeList nodes = xmlDocument.GetElementsByTagName(recordNodeName);
            foreach (XmlNode node in nodes)
            {
                Dictionary<string, object?> dict = new Dictionary<string, object?>();
                foreach (XmlNode child in node.ChildNodes)
                {
                    dict[child.Name] = child.InnerText;
                }
                yield return dict;
            }
        }
    }
}
