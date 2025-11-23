namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using NovaSharp.RemoteDebugger.Network;
    using NUnit.Framework;

    [TestFixture]
    public sealed class XmlWriterExtensionsTests
    {
        [Test]
        public void AttributeAndElementHandleNullValues()
        {
            string xml = WriteXml(xw =>
            {
                using (xw.Element("root"))
                {
                    xw.Attribute("text", (string)null)
                        .Attribute("object", (object)null)
                        .Element("child", (string)null)
                        .ElementCData("payload", (string)null);
                }
            });

            Assert.That(
                xml,
                Does.Contain("text=\"(null)\"")
                    .And.Contain("object=\"(null)\"")
                    .And.Contains("<child>(null)</child>")
            );
            Assert.That(xml, Does.Contain("<payload><![CDATA[(null)]]></payload>"));
        }

        [Test]
        public void FormatOverloadsExpandPlaceholders()
        {
            string xml = WriteXml(xw =>
            {
                using (xw.Element("root"))
                {
                    xw.Attribute("formatted", "value-{0}", 42)
                        .Element("entry", "sum={0}", 3 + 4)
                        .ElementCData("body", "name={0}", "nova")
                        .Comment("debug {0}", 123);
                }
            });

            Assert.That(xml, Does.Contain("formatted=\"value-42\""));
            Assert.That(xml, Does.Contain("<entry>sum=7</entry>"));
            Assert.That(xml, Does.Contain("<body><![CDATA[name=nova]]></body>"));
            Assert.That(xml, Does.Contain("<!--debug 123-->"));
        }

        [Test]
        public void CommentSkipsNullValues()
        {
            string xml = WriteXml(xw =>
            {
                using (xw.Element("root"))
                {
                    xw.Comment((object)null).Comment("visible");
                }
            });

            Assert.That(xml, Does.Contain("<!--visible-->"));
            Assert.That(xml, Does.Not.Contain("null"));
        }

        private static string WriteXml(Action<XmlWriter> writerAction)
        {
            XmlWriterSettings settings = new()
            {
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = true,
                CloseOutput = true,
                ConformanceLevel = ConformanceLevel.Fragment,
            };

            using StringWriter sw = new();
            using XmlWriter xw = XmlWriter.Create(sw, settings);
            writerAction(xw);
            xw.Flush();
            return sw.ToString();
        }
    }
}
