using BusinessLogic.Interfaces;
using System.Xml;
using System.Text;

namespace Infrastructure.Parsers
{
    public class XmlImportParser : IImportParser
    {
        private readonly string recordNodeName;

        public XmlImportParser(string recordNodeName)
        {
            this.recordNodeName = recordNodeName;
        }

        public async Task<IReadOnlyList<IDictionary<string, object?>>> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string xmlText = await reader.ReadToEndAsync(cancellationToken);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlText);

            List<IDictionary<string, object?>> rows = new List<IDictionary<string, object?>>();
            XmlNodeList nodes = xmlDocument.GetElementsByTagName(recordNodeName);
            foreach (XmlNode node in nodes)
            {
                Dictionary<string, object?> dict = new Dictionary<string, object?>();
                foreach (XmlNode child in node.ChildNodes)
                {
                    dict[child.Name] = child.InnerText;
                }
                rows.Add(dict);
            }

            return rows;
        }
    }
}
