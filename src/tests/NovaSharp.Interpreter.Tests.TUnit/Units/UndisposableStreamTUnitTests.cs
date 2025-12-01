#pragma warning disable CA2007, CA1849 // Tests intentionally exercise synchronous stream APIs.
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.IO;

    public sealed class UndisposableStreamTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DisposeAndCloseDoNotPropagateToInnerStream()
        {
            using TrackingStream inner = new();
            using UndisposableStream wrapper = new(inner);

            wrapper.Dispose();

            await Assert.That(inner.DisposeCalled).IsFalse();
            wrapper.WriteByte(1);

#if !(PCL || ENABLE_DOTNET || NETFX_CORE)
            wrapper.Close();
            await Assert.That(inner.CloseCalled).IsFalse();
#endif
            await Task.CompletedTask;
        }

        [global::TUnit.Core.Test]
        public async Task OperationsForwardToInnerStream()
        {
            using TrackingStream inner = new();
            using UndisposableStream wrapper = new(inner);

            await Assert.That(wrapper.CanRead).IsTrue();
            await Assert.That(wrapper.CanWrite).IsTrue();
            await Assert.That(wrapper.CanSeek).IsTrue();
            await Assert.That(wrapper.CanTimeout).IsTrue();

            byte[] payload = { 1, 2, 3 };
            wrapper.Write(payload, 0, payload.Length);
            wrapper.WriteByte(4);
            wrapper.Flush();

            await Assert.That(inner.FlushCalled).IsTrue();
            await Assert.That(wrapper.Length).IsEqualTo(inner.Length);

            wrapper.Position = 0;
            byte[] readBuffer = new byte[4];
            int read = wrapper.Read(readBuffer, 0, readBuffer.Length);
            await Assert.That(read).IsEqualTo(4);
            await Assert
                .That(readBuffer.AsSpan().SequenceEqual(new byte[] { 1, 2, 3, 4 }))
                .IsTrue();

            wrapper.SetLength(10);
            await Assert.That(inner.Length).IsEqualTo(10);

            long endPosition = wrapper.Seek(0, SeekOrigin.End);
            await Assert.That(endPosition).IsEqualTo(10);

            wrapper.Position = 0;
            await Assert.That(wrapper.ReadByte()).IsEqualTo(1);

            wrapper.ReadTimeout = 123;
            wrapper.WriteTimeout = 321;
            await Assert.That(inner.ReadTimeout).IsEqualTo(123);
            await Assert.That(inner.WriteTimeout).IsEqualTo(321);
            await Assert.That(wrapper.ReadTimeout).IsEqualTo(123);
            await Assert.That(wrapper.WriteTimeout).IsEqualTo(321);

#if !(NETFX_CORE)
            IAsyncResult writeResult = wrapper.BeginWrite(new byte[] { 5 }, 0, 1, null, null);
            wrapper.EndWrite(writeResult);
            await Assert.That(inner.BeginWriteCalled).IsTrue();
            await Assert.That(inner.EndWriteCalled).IsTrue();

            wrapper.Position = 0;
            byte[] asyncReadBuffer = new byte[1];
            IAsyncResult readResult = wrapper.BeginRead(asyncReadBuffer, 0, 1, null, null);
            int asyncRead = wrapper.EndRead(readResult);
            await Assert.That(inner.BeginReadCalled).IsTrue();
            await Assert.That(inner.EndReadCalled).IsTrue();
            await Assert.That(asyncRead).IsEqualTo(1);
#endif

            await Assert.That(wrapper.Equals(inner)).IsTrue();
            await Assert.That(wrapper.GetHashCode()).IsEqualTo(inner.GetHashCode());
            await Assert.That(wrapper.ToString()).IsEqualTo(inner.ToString());
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
#pragma warning restore CA2007, CA1849
