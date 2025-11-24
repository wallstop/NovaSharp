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
        public void ContentTypeStringReturnsExpected(HttpResourceType type, string expected)
        {
            HttpResource resource = type switch
            {
                HttpResourceType.PlainText
                or HttpResourceType.Html
                or HttpResourceType.Json
                or HttpResourceType.Xml
                or HttpResourceType.Javascript
                or HttpResourceType.Css => HttpResource.CreateText(type, "payload"),
                _ => HttpResource.CreateBinary(type, new byte[] { 0 }),
            };

            Assert.That(resource.ContentTypeString, Is.EqualTo(expected));
        }

        [Test]
        public void CallbackResourceThrowsWhenRequestingContentType()
        {
            HttpResource resource = HttpResource.CreateCallback(_ => null);
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = resource.ContentTypeString;
            });
        }

        [Test]
        public void CreateBinaryThrowsOnNullByteArray()
        {
            Assert.Throws<ArgumentNullException>(() =>
                HttpResource.CreateBinary(HttpResourceType.Binary, (byte[])null)
            );
        }

        [Test]
        public void CreateBinaryThrowsOnNullBase64()
        {
            Assert.Throws<ArgumentNullException>(() =>
                HttpResource.CreateBinary(HttpResourceType.Binary, (string)null)
            );
        }

        [Test]
        public void CreateTextThrowsOnNullString()
        {
            Assert.Throws<ArgumentNullException>(() =>
                HttpResource.CreateText(HttpResourceType.Html, null)
            );
        }

        [Test]
        public void CreateBinaryWrapsPayloadAsReadOnlyMemory()
        {
            byte[] data = { 1, 2, 3 };
            HttpResource resource = HttpResource.CreateBinary(HttpResourceType.Binary, data);

            Assert.That(resource.Data.ToArray(), Is.EqualTo(data));
        }

        [Test]
        public void CreateTextEncodesUtf8Payload()
        {
            HttpResource resource = HttpResource.CreateText(HttpResourceType.Html, "✓");
            byte[] bytes = resource.Data.ToArray();

            Assert.That(bytes, Is.EqualTo(new byte[] { 0xE2, 0x9C, 0x93 }));
        }
    }
}
