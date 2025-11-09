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
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using Interpreter;

    // ---- Types -------------------------------------------------------------------------

    public class Message
    {
        public int Id { get; private set; }
        public string Format { get; private set; }
        public object Variables { get; private set; }
        public object ShowUser { get; private set; }
        public object SendTelemetry { get; private set; }

        public Message(
            int id,
            string format,
            object variables = null,
            bool user = true,
            bool telemetry = false
        )
        {
            Id = id;
            Format = format;
            Variables = variables;
            ShowUser = user;
            SendTelemetry = telemetry;
        }
    }

    public class StackFrame
    {
        public int Id { get; private set; }
        public Source Source { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public string Name { get; private set; }

        public int? EndLine { get; private set; }
        public int? EndColumn { get; private set; }

        public StackFrame(
            int id,
            string name,
            Source source,
            int line,
            int column = 0,
            int? endLine = null,
            int? endColumn = null
        )
        {
            Id = id;
            Name = name;
            Source = source;
            Line = line;
            Column = column;
            EndLine = endLine;
            EndColumn = endColumn;
        }
    }

    public class Scope
    {
        public string Name { get; private set; }
        public int VariablesReference { get; private set; }
        public bool Expensive { get; private set; }

        public Scope(string name, int variablesReference, bool expensive = false)
        {
            Name = name;
            VariablesReference = variablesReference;
            Expensive = expensive;
        }
    }

    public class Variable
    {
        public string Name { get; private set; }
        public string Value { get; private set; }
        public int VariablesReference { get; private set; }

        public Variable(string name, string value, int variablesReference = 0)
        {
            Name = name;
            Value = value;
            VariablesReference = variablesReference;
        }
    }

    public class Thread
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public Thread(int id, string name)
        {
            Id = id;
            if (name == null || name.Length == 0)
            {
                Name = $"Thread #{id}";
            }
            else
            {
                Name = name;
            }
        }
    }

    public class Source
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public int SourceReference { get; private set; }

        public Source(string name, string path, int sourceReference = 0)
        {
            Name = name;
            Path = path;
            SourceReference = sourceReference;
        }

        public Source(string path, int sourceReference = 0)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
            SourceReference = sourceReference;
        }
    }

    public class Breakpoint
    {
        public bool Verified { get; private set; }
        public int Line { get; private set; }

        public Breakpoint(bool verified, int line)
        {
            Verified = verified;
            Line = line;
        }
    }

    // ---- Events -------------------------------------------------------------------------

    public class InitializedEvent : Event
    {
        public InitializedEvent()
            : base("initialized") { }
    }

    public class StoppedEvent : Event
    {
        public StoppedEvent(int tid, string reasn, string txt = null)
            : base(
                "stopped",
                new
                {
                    threadId = tid,
                    reason = reasn,
                    text = txt,
                }
            ) { }
    }

    public class ExitedEvent : Event
    {
        public ExitedEvent(int exCode)
            : base("exited", new { exitCode = exCode }) { }
    }

    public class TerminatedEvent : Event
    {
        public TerminatedEvent()
            : base("terminated") { }
    }

    public class ThreadEvent : Event
    {
        public ThreadEvent(string reasn, int tid)
            : base("thread", new { reason = reasn, threadId = tid }) { }
    }

    public class OutputEvent : Event
    {
        public OutputEvent(string cat, string outpt)
            : base("output", new { category = cat, output = outpt }) { }
    }

    // ---- Response -------------------------------------------------------------------------

    public class Capabilities : ResponseBody
    {
        public bool supportsConfigurationDoneRequest;
        public bool supportsFunctionBreakpoints;
        public bool supportsConditionalBreakpoints;
        public bool supportsEvaluateForHovers;
        public object[] exceptionBreakpointFilters;
    }

    public class ErrorResponseBody : ResponseBody
    {
        public Message Error { get; private set; }

        public ErrorResponseBody(Message error)
        {
            Error = error;
        }
    }

    public class StackTraceResponseBody : ResponseBody
    {
        public StackFrame[] StackFrames { get; private set; }

        public StackTraceResponseBody(List<StackFrame> frames = null)
        {
            if (frames == null)
            {
                StackFrames = new StackFrame[0];
            }
            else
            {
                StackFrames = frames.ToArray<StackFrame>();
            }
        }
    }

    public class ScopesResponseBody : ResponseBody
    {
        public Scope[] Scopes { get; private set; }

        public ScopesResponseBody(List<Scope> scps = null)
        {
            if (scps == null)
            {
                Scopes = new Scope[0];
            }
            else
            {
                Scopes = scps.ToArray<Scope>();
            }
        }
    }

    public class VariablesResponseBody : ResponseBody
    {
        public Variable[] Variables { get; private set; }

        public VariablesResponseBody(List<Variable> vars = null)
        {
            if (vars == null)
            {
                Variables = new Variable[0];
            }
            else
            {
                Variables = vars.ToArray<Variable>();
            }
        }
    }

    public class ThreadsResponseBody : ResponseBody
    {
        public Thread[] Threads { get; private set; }

        public ThreadsResponseBody(List<Thread> vars = null)
        {
            if (vars == null)
            {
                Threads = new Thread[0];
            }
            else
            {
                Threads = vars.ToArray<Thread>();
            }
        }
    }

    public class EvaluateResponseBody : ResponseBody
    {
        public string Result { get; private set; }
        public string Type { get; set; }
        public int VariablesReference { get; private set; }

        public EvaluateResponseBody(string value, int reff = 0)
        {
            Result = value;
            VariablesReference = reff;
        }
    }

    public class SetBreakpointsResponseBody : ResponseBody
    {
        public Breakpoint[] Breakpoints { get; private set; }

        public SetBreakpointsResponseBody(List<Breakpoint> bpts = null)
        {
            if (bpts == null)
            {
                Breakpoints = new Breakpoint[0];
            }
            else
            {
                Breakpoints = bpts.ToArray<Breakpoint>();
            }
        }
    }

    // ---- The Session --------------------------------------------------------

    public abstract class DebugSession : ProtocolServer
    {
        private readonly bool _debuggerLinesStartAt1;
        private readonly bool _debuggerPathsAreUri;
        private bool _clientLinesStartAt1 = true;
        private bool _clientPathsAreUri = true;

        public DebugSession(bool debuggerLinesStartAt1, bool debuggerPathsAreUri = false)
        {
            _debuggerLinesStartAt1 = debuggerLinesStartAt1;
            _debuggerPathsAreUri = debuggerPathsAreUri;
        }

        public void SendResponse(Response response, ResponseBody body = null)
        {
            if (body != null)
            {
                response.SetBody(body);
            }
            SendMessage(response);
        }

        public void SendErrorResponse(
            Response response,
            int id,
            string format,
            object arguments = null,
            bool user = true,
            bool telemetry = false
        )
        {
            Message msg = new(id, format, arguments, user, telemetry);
            string message = Utilities.ExpandVariables(msg.Format, msg.Variables);
            response.SetErrorBody(message, new ErrorResponseBody(msg));
            SendMessage(response);
        }

        protected override void DispatchRequest(string command, Table args, Response response)
        {
            if (args == null)
            {
                args = new Table(null);
            }

            try
            {
                switch (command)
                {
                    case "initialize":

                        if (args["linesStartAt1"] != null)
                        {
                            _clientLinesStartAt1 = args.Get("linesStartAt1").ToObject<bool>();
                        }

                        string pathFormat = args.Get("pathFormat").ToObject<string>();
                        if (pathFormat != null)
                        {
                            switch (pathFormat)
                            {
                                case "uri":
                                    _clientPathsAreUri = true;
                                    break;
                                case "path":
                                    _clientPathsAreUri = false;
                                    break;
                                default:
                                    SendErrorResponse(
                                        response,
                                        1015,
                                        "initialize: bad value '{_format}' for pathFormat",
                                        new { _format = pathFormat }
                                    );
                                    return;
                            }
                        }
                        Initialize(response, args);
                        break;

                    case "launch":
                        Launch(response, args);
                        break;

                    case "attach":
                        Attach(response, args);
                        break;

                    case "disconnect":
                        Disconnect(response, args);
                        break;

                    case "next":
                        Next(response, args);
                        break;

                    case "continue":
                        Continue(response, args);
                        break;

                    case "stepIn":
                        StepIn(response, args);
                        break;

                    case "stepOut":
                        StepOut(response, args);
                        break;

                    case "pause":
                        Pause(response, args);
                        break;

                    case "stackTrace":
                        StackTrace(response, args);
                        break;

                    case "scopes":
                        Scopes(response, args);
                        break;

                    case "variables":
                        Variables(response, args);
                        break;

                    case "source":
                        Source(response, args);
                        break;

                    case "threads":
                        Threads(response, args);
                        break;

                    case "setBreakpoints":
                        SetBreakpoints(response, args);
                        break;

                    case "setFunctionBreakpoints":
                        SetFunctionBreakpoints(response, args);
                        break;

                    case "setExceptionBreakpoints":
                        SetExceptionBreakpoints(response, args);
                        break;

                    case "evaluate":
                        Evaluate(response, args);
                        break;

                    default:
                        SendErrorResponse(
                            response,
                            1014,
                            "unrecognized request: {_request}",
                            new { _request = command }
                        );
                        break;
                }
            }
            catch (Exception e)
            {
                SendErrorResponse(
                    response,
                    1104,
                    "error while processing request '{_request}' (exception: {_exception})",
                    new { _request = command, _exception = e.Message }
                );
            }

            if (command == "disconnect")
            {
                Stop();
            }
        }

        public abstract void Initialize(Response response, Table args);

        public abstract void Launch(Response response, Table arguments);

        public abstract void Attach(Response response, Table arguments);

        public abstract void Disconnect(Response response, Table arguments);

        public virtual void SetFunctionBreakpoints(Response response, Table arguments) { }

        public virtual void SetExceptionBreakpoints(Response response, Table arguments) { }

        public abstract void SetBreakpoints(Response response, Table arguments);

        public abstract void Continue(Response response, Table arguments);

        public abstract void Next(Response response, Table arguments);

        public abstract void StepIn(Response response, Table arguments);

        public abstract void StepOut(Response response, Table arguments);

        public abstract void Pause(Response response, Table arguments);

        public abstract void StackTrace(Response response, Table arguments);

        public abstract void Scopes(Response response, Table arguments);

        public abstract void Variables(Response response, Table arguments);

        public virtual void Source(Response response, Table arguments)
        {
            SendErrorResponse(response, 1020, "Source not supported");
        }

        public abstract void Threads(Response response, Table arguments);

        public abstract void Evaluate(Response response, Table arguments);

        // protected

        protected int ConvertDebuggerLineToClient(int line)
        {
            if (_debuggerLinesStartAt1)
            {
                return _clientLinesStartAt1 ? line : line - 1;
            }
            else
            {
                return _clientLinesStartAt1 ? line + 1 : line;
            }
        }

        protected int ConvertClientLineToDebugger(int line)
        {
            if (_debuggerLinesStartAt1)
            {
                return _clientLinesStartAt1 ? line : line + 1;
            }
            else
            {
                return _clientLinesStartAt1 ? line - 1 : line;
            }
        }

        protected string ConvertDebuggerPathToClient(string path)
        {
            if (_debuggerPathsAreUri)
            {
                if (_clientPathsAreUri)
                {
                    return path;
                }
                else
                {
                    Uri uri = new(path);
                    return uri.LocalPath;
                }
            }
            else
            {
                if (_clientPathsAreUri)
                {
                    try
                    {
                        Uri uri = new(path);
                        return uri.AbsoluteUri;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return path;
                }
            }
        }

        protected string ConvertClientPathToDebugger(string clientPath)
        {
            if (clientPath == null)
            {
                return null;
            }

            if (_debuggerPathsAreUri)
            {
                if (_clientPathsAreUri)
                {
                    return clientPath;
                }
                else
                {
                    Uri uri = new(clientPath);
                    return uri.AbsoluteUri;
                }
            }
            else
            {
                if (_clientPathsAreUri)
                {
                    if (Uri.IsWellFormedUriString(clientPath, UriKind.Absolute))
                    {
                        Uri uri = new(clientPath);
                        return uri.LocalPath;
                    }
                    Console.Error.WriteLine("path not well formed: '{0}'", clientPath);
                    return null;
                }
                else
                {
                    return clientPath;
                }
            }
        }
    }
}
#endif
