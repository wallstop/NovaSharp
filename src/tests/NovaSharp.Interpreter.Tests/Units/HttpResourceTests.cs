namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.RemoteDebugger.Network;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HttpResourceTests
    {
        [TestCase(HttpResourceType.PlainText, "text/plain")]
        [TestCase(HttpResourceType.Html, "text/html")]
        [TestCase(HttpResourceType.Json, "application/json")]
        [TestCase(HttpResourceType.Xml, "application/xml")]
        [TestCase(HttpResourceType.Jpeg, "image/jpeg")]
        [TestCase(HttpResourceType.Png, "image/png")]
        [TestCase(HttpResourceType.Binary, "application/octet-stream")]
        [TestCase(HttpResourceType.Javascript, "application/javascript")]
        [TestCase(HttpResourceType.Css, "text/css")]
        public void GetContentTypeStringReturnsExpected(HttpResourceType type, string expected)
        {
            HttpResource resource = type switch
            {
                HttpResourceType.PlainText
                or HttpResourceType.Html
                or HttpResourceType.Json
                or HttpResourceType.Xml
                or HttpResourceType.Javascript
                or HttpResourceType.Css
                    => HttpResource.CreateText(type, "payload"),
                _ => HttpResource.CreateBinary(type, new byte[] { 0 }),
            };

            Assert.That(resource.GetContentTypeString(), Is.EqualTo(expected));
        }

        [Test]
        public void CallbackResourceThrowsWhenRequestingContentType()
        {
            HttpResource resource = HttpResource.CreateCallback(_ => null);
            Assert.Throws<InvalidOperationException>(() => resource.GetContentTypeString());
        }
    }
}
