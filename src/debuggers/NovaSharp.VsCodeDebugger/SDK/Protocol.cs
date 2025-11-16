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
    using System.Text;
    using System.IO;
    using System.Text.RegularExpressions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Serialization.Json;

    public class ProtocolMessage
    {
        public int seq;
        public string Type { get; private set; }

        public ProtocolMessage(string typ)
        {
            Type = typ;
        }

        public ProtocolMessage(string typ, int sq)
        {
            Type = typ;
            seq = sq;
        }
    }

    public class Request : ProtocolMessage
    {
        public string command;
        public Table arguments;

        public Request(int id, string cmd, Table arg)
            : base("request", id)
        {
            command = cmd;
            arguments = arg;
        }
    }

    /*
     * subclasses of ResponseBody are serialized as the body of a response.
     * Don't change their instance variables since that will break the debug protocol.
     */
    public class ResponseBody
    {
        // empty
    }

    public class Response : ProtocolMessage
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public int RequestSeq { get; private set; }
        public string Command { get; private set; }
        public ResponseBody Body { get; private set; }

        public Response(Table req)
            : base("response")
        {
            Success = true;
            RequestSeq = req.Get("seq").ToObject<int>();
            Command = req.Get("command").ToObject<string>();
        }

        public void SetBody(ResponseBody bdy)
        {
            Success = true;
            Body = bdy;
        }

        public void SetErrorBody(string msg, ResponseBody bdy = null)
        {
            Success = false;
            Message = msg;
            Body = bdy;
        }
    }

    public class Event : ProtocolMessage
    {
        public readonly string @event;
        public readonly object body;

        public Event(string type, object bdy = null)
            : base("event")
        {
            @event = type;
            this.body = bdy;
        }
    }

    /*
     * The ProtocolServer can be used to implement a server that uses the VSCode debug protocol.
     */
    public abstract class ProtocolServer
    {
        public bool trace;
        public bool traceResponse;

        protected const int BUFFER_SIZE = 4096;
        protected const string TWO_CRLF = "\r\n\r\n";
        protected static readonly Regex ContentLengthMatcher = new(@"Content-Length: (\d+)");

        protected static readonly Encoding Encoding = Encoding.UTF8;

        private int _sequenceNumber;

        private Stream _outputStream;

        private readonly ByteBuffer _rawData;
        private int _bodyLength;

        private bool _stopRequested;

        public ProtocolServer()
        {
            _sequenceNumber = 1;
            _bodyLength = -1;
            _rawData = new ByteBuffer();
        }

        public void ProcessLoop(Stream inputStream, Stream outputStream)
        {
            _outputStream = outputStream;

            byte[] buffer = new byte[BUFFER_SIZE];

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

        public void Stop()
        {
            _stopRequested = true;
        }

        public void SendEvent(Event e)
        {
            SendMessage(e);
        }

        protected abstract void DispatchRequest(string command, Table args, Response response);

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
                    int idx = s.IndexOf(TWO_CRLF);
                    if (idx != -1)
                    {
                        Match m = ContentLengthMatcher.Match(s);
                        if (m.Success && m.Groups.Count == 2)
                        {
                            _bodyLength = Convert.ToInt32(m.Groups[1].ToString());

                            _rawData.RemoveFirst(idx + TWO_CRLF.Length);

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
                    if (trace)
                    {
                        Console.Error.WriteLine($"C {request["command"]}: {req}");
                    }

                    Response response = new(request);

                    DispatchRequest(
                        request.Get("command").String,
                        request.Get("arguments").Table,
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
            message.seq = _sequenceNumber++;

            if (traceResponse && message.Type == "response")
            {
                Console.Error.WriteLine($" R: {JsonTableConverter.ObjectToJson(message)}");
            }
            if (trace && message.Type == "event")
            {
                Event e = (Event)message;
                Console.Error.WriteLine($"E {e.@event}: {JsonTableConverter.ObjectToJson(e.body)}");
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

            string header = $"Content-Length: {jsonBytes.Length}{TWO_CRLF}";
            byte[] headerBytes = Encoding.GetBytes(header);

            byte[] data = new byte[headerBytes.Length + jsonBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, data, 0, headerBytes.Length);
            Buffer.BlockCopy(jsonBytes, 0, data, headerBytes.Length, jsonBytes.Length);

            return data;
        }
    }

    //--------------------------------------------------------------------------------------

    internal sealed class ByteBuffer
    {
        private byte[] _buffer;

        public ByteBuffer()
        {
            _buffer = new byte[0];
        }

        public int Length
        {
            get { return _buffer.Length; }
        }

        public string GetString(Encoding enc)
        {
            return enc.GetString(_buffer);
        }

        public void Append(byte[] b, int length)
        {
            byte[] newBuffer = new byte[_buffer.Length + length];
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);
            Buffer.BlockCopy(b, 0, newBuffer, _buffer.Length, length);
            _buffer = newBuffer;
        }

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
