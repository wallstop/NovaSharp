namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Threading;
    using NovaSharp.Interpreter.IO;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UndisposableStreamTests
    {
        [Test]
        public void DisposeAndCloseDoNotPropagateToInnerStream()
        {
            TrackingStream inner = new();
            UndisposableStream wrapper = new(inner);

            wrapper.Dispose();
            Assert.Multiple(() =>
            {
                Assert.That(inner.DisposeCalled, Is.False);
                Assert.That(() => wrapper.WriteByte(1), Throws.Nothing);
            });

#if !(PCL || ENABLE_DOTNET || NETFX_CORE)
            wrapper.Close();
            Assert.That(inner.CloseCalled, Is.False);
#endif
        }

        [Test]
        public void OperationsForwardToInnerStream()
        {
            TrackingStream inner = new();
            UndisposableStream wrapper = new(inner);

            Assert.Multiple(() =>
            {
                Assert.That(wrapper.CanRead, Is.True);
                Assert.That(wrapper.CanWrite, Is.True);
                Assert.That(wrapper.CanSeek, Is.True);
                Assert.That(wrapper.CanTimeout, Is.True);
            });

            byte[] payload = { 1, 2, 3 };
            wrapper.Write(payload, 0, payload.Length);
            wrapper.WriteByte(4);
            wrapper.Flush();

            Assert.That(inner.FlushCalled, Is.True);
            Assert.That(wrapper.Length, Is.EqualTo(inner.Length));

            wrapper.Position = 0;
            byte[] readBuffer = new byte[4];
            int read = wrapper.Read(readBuffer, 0, readBuffer.Length);
            Assert.That(read, Is.EqualTo(4));
            Assert.That(readBuffer, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));

            wrapper.SetLength(10);
            Assert.That(inner.Length, Is.EqualTo(10));

            long endPosition = wrapper.Seek(0, SeekOrigin.End);
            Assert.That(endPosition, Is.EqualTo(10));

            wrapper.Position = 0;
            Assert.That(wrapper.ReadByte(), Is.EqualTo(1));

            wrapper.ReadTimeout = 123;
            wrapper.WriteTimeout = 321;
            Assert.Multiple(() =>
            {
                Assert.That(inner.ReadTimeout, Is.EqualTo(123));
                Assert.That(inner.WriteTimeout, Is.EqualTo(321));
                Assert.That(wrapper.ReadTimeout, Is.EqualTo(123));
                Assert.That(wrapper.WriteTimeout, Is.EqualTo(321));
            });

#if !(NETFX_CORE)
            IAsyncResult writeResult = wrapper.BeginWrite(new byte[] { 5 }, 0, 1, null, null);
            wrapper.EndWrite(writeResult);
            Assert.Multiple(() =>
            {
                Assert.That(inner.BeginWriteCalled, Is.True);
                Assert.That(inner.EndWriteCalled, Is.True);
            });

            wrapper.Position = 0;
            byte[] asyncReadBuffer = new byte[1];
            IAsyncResult readResult = wrapper.BeginRead(asyncReadBuffer, 0, 1, null, null);
            int asyncRead = wrapper.EndRead(readResult);
            Assert.Multiple(() =>
            {
                Assert.That(inner.BeginReadCalled, Is.True);
                Assert.That(inner.EndReadCalled, Is.True);
                Assert.That(asyncRead, Is.EqualTo(1));
            });
#endif

            Assert.Multiple(() =>
            {
                Assert.That(wrapper.Equals(inner), Is.True);
                Assert.That(wrapper.GetHashCode(), Is.EqualTo(inner.GetHashCode()));
                Assert.That(wrapper.ToString(), Is.EqualTo(inner.ToString()));
            });
        }

        private sealed class TrackingStream : MemoryStream
        {
            public bool DisposeCalled { get; private set; }
            public bool CloseCalled { get; private set; }
            public bool FlushCalled { get; private set; }
            public bool BeginWriteCalled { get; private set; }
            public bool EndWriteCalled { get; private set; }
            public bool BeginReadCalled { get; private set; }
            public bool EndReadCalled { get; private set; }

            private int _readTimeout;
            private int _writeTimeout;

            public override bool CanTimeout => true;

            public override int ReadTimeout
            {
                get => _readTimeout;
                set => _readTimeout = value;
            }

            public override int WriteTimeout
            {
                get => _writeTimeout;
                set => _writeTimeout = value;
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCalled = true;
                base.Dispose(disposing);
            }

            public override void Close()
            {
                CloseCalled = true;
                base.Close();
            }

            public override void Flush()
            {
                FlushCalled = true;
                base.Flush();
            }

            public override IAsyncResult BeginWrite(
                byte[] buffer,
                int offset,
                int count,
                AsyncCallback callback,
                object state
            )
            {
                BeginWriteCalled = true;
                return base.BeginWrite(buffer, offset, count, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                EndWriteCalled = true;
                base.EndWrite(asyncResult);
            }

            public override IAsyncResult BeginRead(
                byte[] buffer,
                int offset,
                int count,
                AsyncCallback callback,
                object state
            )
            {
                BeginReadCalled = true;
                return base.BeginRead(buffer, offset, count, callback, state);
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                EndReadCalled = true;
                return base.EndRead(asyncResult);
            }
        }
    }
}
