using OmniPort.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(stream);

            var nodes = xmlDocument.GetElementsByTagName(recordNodeName);
            foreach (XmlNode node in nodes)
            {
                var dict = new Dictionary<string, object?>();
                foreach (XmlNode child in node.ChildNodes)
                {
                    dict[child.Name] = child.InnerText;
                }
                yield return dict;
            }
        }
    }
}
