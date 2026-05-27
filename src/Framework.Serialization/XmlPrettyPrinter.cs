using System.IO;
using System.Text;
using System.Xml;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Pretty-prints XML text using <see cref="XmlReader" /> and <see cref="XmlWriter" />.
///     Returns the original text unchanged when the input is not well-formed XML.
/// </summary>
public static class XmlPrettyPrinter
{
    /// <summary>
    ///     Returns a pretty-printed (indented, two-space) version of the supplied XML text.
    ///     When the input is not well-formed XML the original text is returned verbatim.
    /// </summary>
    /// <param name="rawXml">The raw XML text.</param>
    /// <returns>The pretty-printed XML, or the original text on parse failure.</returns>
    public static string PrettyPrint(string rawXml)
    {
        if (string.IsNullOrEmpty(rawXml))
        {
            return rawXml;
        }

        try
        {
            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreWhitespace = true,
                XmlResolver = null,
            };
            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8,
            };

            using var stringReader = new StringReader(rawXml);
            using var xmlReader = XmlReader.Create(stringReader, readerSettings);
            using var stringWriter = new Utf8StringWriter();

            using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
            {
                xmlWriter.WriteNode(xmlReader, defattr: false);
            }

            return stringWriter.ToString();
        }
        catch (XmlException)
        {
            return rawXml;
        }
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
