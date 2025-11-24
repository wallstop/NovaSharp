namespace NovaSharp.VsCodeDebugger.SDK
{
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

    /*---------------------------------------------------------------------------------------------
    Copyright (c) Microsoft Corporation

    All rights reserved.

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
     *--------------------------------------------------------------------------------------------*/
    using System;
    using System.Globalization;
    using System.Text;
    using System.IO;
    using System.Text.RegularExpressions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Serialization.Json;

    /// <summary>
    /// Base type for every Debug Adapter Protocol payload (requests, responses, events).
    /// </summary>
    public class ProtocolMessage
    {
        /// <summary>
        /// Running sequence assigned to the payload.
        /// </summary>
        public int Sequenceuence { get; internal set; }

        /// <summary>
        /// Message category (request/response/event).
        /// </summary>
        public string Type { get; private set; }

        public ProtocolMessage(string typ)
        {
            Type = typ;
        }

        public ProtocolMessage(string typ, int sq)
        {
            Type = typ;
            Sequenceuence = sq;
        }
    }

    /// <summary>
    /// Represents an inbound request from VS Code.
    /// </summary>
    public class Request : ProtocolMessage
    {
        public string Command { get; }
        public Table Arguments { get; }

        public Request(int id, string cmd, Table arg)
            : base("request", id)
        {
            Command = cmd;
            Arguments = arg;
        }
    }

    /*
     * subclasses of ResponseBody are serialized as the Body of a response.
     * Don't change their instance variables since that will break the debug protocol.
     */
    /// <summary>
    /// Marker base class for strongly-typed response bodies.
    /// </summary>
    public class ResponseBody
    {
        // empty
    }

    /// <summary>
    /// Outbound response payload.
    /// </summary>
    public class Response : ProtocolMessage
    {
        /// <summary>
        /// Indicates whether the request succeeded.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Optional human-readable error message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Sequence number of the originating request.
        /// </summary>
        public int RequestSequenceuence { get; private set; }

        /// <summary>
        /// Command being answered.
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        /// Strongly typed body returned to VS Code.
        /// </summary>
        public ResponseBody Body { get; private set; }

        public Response(Table req)
            : base("response")
        {
            Success = true;
            RequestSequenceuence = req.Get("Sequenceuence").ToObject<int>();
            Command = req.Get("Command").ToObject<string>();
        }

        /// <summary>
        /// Marks the response successful and assigns the body.
        /// </summary>
        public void SetBody(ResponseBody bdy)
        {
            Success = true;
            Body = bdy;
        }

        /// <summary>
        /// Marks the response failed and adds error context.
        /// </summary>
        public void SetErrorBody(string msg, ResponseBody bdy = null)
        {
            Success = false;
            Message = msg;
            Body = bdy;
        }
    }

    /// <summary>
    /// Outbound event notification.
    /// </summary>
    public class Event : ProtocolMessage
    {
        public string @event { get; }
        public object Body { get; }

        public Event(string type, object bdy = null)
            : base("event")
        {
            @event = type;
            Body = bdy;
        }
    }

    /// <summary>
    /// Implements the VS Code debug protocol framing and dispatch pipeline.
    /// </summary>
    public abstract class ProtocolServer
    {
        public bool Trace { get; set; }
        public bool TraceResponse { get; set; }

        protected const int BufferSize = 4096;
        protected const string TwoCrLf = "\r\n\r\n";
        protected static readonly Regex ContentLengthMatcher = new(@"Content-Length: (\d+)");

        protected static readonly Encoding Encoding = Encoding.UTF8;

        private int _sequenceNumber;

        private Stream _outputStream;

        private readonly ByteBuffer _rawData;
        private int _bodyLength;

        private bool _stopRequested;

        protected ProtocolServer()
        {
            _sequenceNumber = 1;
            _bodyLength = -1;
            _rawData = new ByteBuffer();
        }

        /// <summary>
        /// Continuously reads framed messages from <paramref name="inputStream"/> and dispatches them until <see cref="Stop"/> is called.
        /// </summary>
        public void ProcessLoop(Stream inputStream, Stream outputStream)
        {
            _outputStream = outputStream;

            byte[] buffer = new byte[BufferSize];

            _stopRequested = false;
            while (!_stopRequested)
            {
                int read = inputStream.Read(buffer, 0, buffer.Length);

                if (read == 0)
                {
                    // end of stream
                    break;
                }

                if (read > 0)
                {
                    _rawData.Append(buffer, read);
                    ProcessData();
                }
            }
        }

        /// <summary>
        /// Requests termination of the processing loop.
        /// </summary>
        public void Stop()
        {
            _stopRequested = true;
        }

        /// <summary>
        /// Sends an event payload to VS Code.
        /// </summary>
        public void SendEvent(Event e)
        {
            SendMessage(e);
        }

        protected abstract void DispatchRequest(string Command, Table args, Response response);

        // ---- private ------------------------------------------------------------------------

        private void ProcessData()
        {
            while (true)
            {
                if (_bodyLength >= 0)
                {
                    if (_rawData.Length >= _bodyLength)
                    {
                        byte[] buf = _rawData.RemoveFirst(_bodyLength);

                        _bodyLength = -1;

                        Dispatch(Encoding.GetString(buf));

                        continue; // there may be more complete messages to process
                    }
                }
                else
                {
                    string s = _rawData.GetString(Encoding);
                    int idx = s.IndexOf(TwoCrLf);
                    if (idx != -1)
                    {
                        Match m = ContentLengthMatcher.Match(s);
                        if (m.Success && m.Groups.Count == 2)
                        {
                            _bodyLength = Convert.ToInt32(
                                m.Groups[1].ToString(),
                                CultureInfo.InvariantCulture
                            );

                            _rawData.RemoveFirst(idx + TwoCrLf.Length);

                            continue; // try to handle a complete message
                        }
                    }
                }
                break;
            }
        }

        private void Dispatch(string req)
        {
            try
            {
                Table request = JsonTableConverter.JsonToTable(req);
                if (request != null && request["type"].ToString() == "request")
                {
                    if (Trace)
                    {
                        Console.Error.WriteLine($"C {request["Command"]}: {req}");
                    }

                    Response response = new(request);

                    DispatchRequest(
                        request.Get("Command").String,
                        request.Get("Arguments").Table,
                        response
                    );

                    SendMessage(response);
                }
            }
            catch
            {
                // Swallow
            }
        }

        protected void SendMessage(ProtocolMessage message)
        {
            message.Sequenceuence = _sequenceNumber++;

            if (TraceResponse && message.Type == "response")
            {
                Console.Error.WriteLine($" R: {JsonTableConverter.ObjectToJson(message)}");
            }
            if (Trace && message.Type == "event")
            {
                Event e = (Event)message;
                Console.Error.WriteLine($"E {e.@event}: {JsonTableConverter.ObjectToJson(e.Body)}");
            }

            byte[] data = ConvertToBytes(message);
            try
            {
                _outputStream.Write(data, 0, data.Length);
                _outputStream.Flush();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private static byte[] ConvertToBytes(ProtocolMessage request)
        {
            string asJson = JsonTableConverter.ObjectToJson(request);
            byte[] jsonBytes = Encoding.GetBytes(asJson);

            string header = $"Content-Length: {jsonBytes.Length}{TwoCrLf}";
            byte[] headerBytes = Encoding.GetBytes(header);

            byte[] data = new byte[headerBytes.Length + jsonBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, data, 0, headerBytes.Length);
            Buffer.BlockCopy(jsonBytes, 0, data, headerBytes.Length, jsonBytes.Length);

            return data;
        }
    }

    //--------------------------------------------------------------------------------------

    /// <summary>
    /// Simple grow-only buffer used to accumulate framed protocol data.
    /// </summary>
    internal sealed class ByteBuffer
    {
        private byte[] _buffer;

        public ByteBuffer()
        {
            _buffer = new byte[0];
        }

        /// <summary>
        /// Gets the current number of bytes stored in the buffer.
        /// </summary>
        public int Length
        {
            get { return _buffer.Length; }
        }

        /// <summary>
        /// Interprets the buffer contents as a string using the specified encoding.
        /// </summary>
        public string GetString(Encoding enc)
        {
            return enc.GetString(_buffer);
        }

        /// <summary>
        /// Appends <paramref name="length"/> bytes from <paramref name="b"/> to the buffer.
        /// </summary>
        public void Append(byte[] b, int length)
        {
            byte[] newBuffer = new byte[_buffer.Length + length];
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);
            Buffer.BlockCopy(b, 0, newBuffer, _buffer.Length, length);
            _buffer = newBuffer;
        }

        /// <summary>
        /// Removes and returns the first <paramref name="n"/> bytes from the buffer.
        /// </summary>
        public byte[] RemoveFirst(int n)
        {
            byte[] b = new byte[n];
            Buffer.BlockCopy(_buffer, 0, b, 0, n);
            byte[] newBuffer = new byte[_buffer.Length - n];
            Buffer.BlockCopy(_buffer, n, newBuffer, 0, _buffer.Length - n);
            _buffer = newBuffer;
            return b;
        }
    }
}
#endif
