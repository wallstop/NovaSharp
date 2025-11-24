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
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    // ---- Types -------------------------------------------------------------------------

    /// <summary>
    /// DAP message payload used for error/info responses originating from the adapter.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// VS Code error identifier.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Format string describing the error or info message.
        /// </summary>
        public string Format { get; private set; }

        /// <summary>
        /// Named variables substituted into the <see cref="Format"/> string.
        /// </summary>
        public object Variables { get; private set; }

        /// <summary>
        /// Indicates whether the message should be shown to the user.
        /// </summary>
        public object ShowUser { get; private set; }

        /// <summary>
        /// Indicates whether the message should be forwarded as telemetry.
        /// </summary>
        public object SendTelemetry { get; private set; }

        /// <summary>
        /// Initializes a new message payload.
        /// </summary>
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

    /// <summary>
    /// Represents an entry on the call stack reported back to VS Code.
    /// </summary>
    public class StackFrame
    {
        /// <summary>
        /// Unique identifier referencing the stack frame inside the adapter.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Source metadata describing where the frame resides.
        /// </summary>
        public Source Source { get; private set; }

        /// <summary>
        /// One-based starting line number in the client coordinate system.
        /// </summary>
        public int Line { get; private set; }

        /// <summary>
        /// One-based starting column number in the client coordinate system.
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// Human-friendly frame name displayed by VS Code.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Optional inclusive end line for multi-line frames.
        /// </summary>
        public int? EndLine { get; private set; }

        /// <summary>
        /// Optional inclusive end column for multi-line frames.
        /// </summary>
        public int? EndColumn { get; private set; }

        /// <summary>
        /// Creates a new stack frame record.
        /// </summary>
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

    /// <summary>
    /// Describes a logical scope (locals, globals, etc.) used by VS Code when enumerating variables.
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// Display name shown in the VS Code scopes pane.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Adapter-provided reference used to request the variables inside this scope.
        /// </summary>
        public int VariablesReference { get; private set; }

        /// <summary>
        /// Indicates whether requesting this scope is expensive (VS Code may lazy-load).
        /// </summary>
        public bool Expensive { get; private set; }

        /// <summary>
        /// Initializes a new scope descriptor.
        /// </summary>
        public Scope(string name, int variablesReference, bool expensive = false)
        {
            Name = name;
            VariablesReference = variablesReference;
            Expensive = expensive;
        }
    }

    /// <summary>
    /// Represents a single variable entry returned to VS Code.
    /// </summary>
    public class Variable
    {
        /// <summary>
        /// Variable name as rendered in VS Code.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Stringified value shown to the user.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Reference used to expand child variables (0 for scalars).
        /// </summary>
        public int VariablesReference { get; private set; }

        /// <summary>
        /// Creates a new variable entry.
        /// </summary>
        public Variable(string name, string value, int variablesReference = 0)
        {
            Name = name;
            Value = value;
            VariablesReference = variablesReference;
        }
    }

    /// <summary>
    /// Identifies an execution thread reported back to VS Code.
    /// </summary>
    public class Thread
    {
        /// <summary>
        /// Adapter-specific identifier for the thread or coroutine.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Name displayed in VS Code's threads view.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a thread descriptor.
        /// </summary>
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

    /// <summary>
    /// Represents the script file associated with a stack frame or breakpoint.
    /// </summary>
    public class Source
    {
        /// <summary>
        /// File name shown in UI (no directory component).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Full path (or URI) used to locate the file.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Optional reference used when the file is provided via the adapter instead of disk.
        /// </summary>
        public int SourceReference { get; private set; }

        /// <summary>
        /// Creates a source descriptor with explicit name and path.
        /// </summary>
        public Source(string name, string path, int sourceReference = 0)
        {
            Name = name;
            Path = path;
            SourceReference = sourceReference;
        }

        /// <summary>
        /// Creates a source descriptor using the file name derived from <paramref name="path"/>.
        /// </summary>
        public Source(string path, int sourceReference = 0)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
            SourceReference = sourceReference;
        }
    }

    /// <summary>
    /// Represents a breakpoint as acknowledged by the adapter.
    /// </summary>
    public class Breakpoint
    {
        /// <summary>
        /// Indicates whether the breakpoint was successfully registered.
        /// </summary>
        public bool Verified { get; private set; }

        /// <summary>
        /// Client-facing line number where the breakpoint is set.
        /// </summary>
        public int Line { get; private set; }

        /// <summary>
        /// Initializes a breakpoint response entry.
        /// </summary>
        public Breakpoint(bool verified, int line)
        {
            Verified = verified;
            Line = line;
        }
    }

    // ---- Events -------------------------------------------------------------------------

    /// <summary>
    /// Signals that the adapter is ready to accept configuration requests.
    /// </summary>
    public class InitializedEvent : Event
    {
        public InitializedEvent()
            : base("initialized") { }
    }

    /// <summary>
    /// Notifies VS Code that execution paused (breakpoint, step, exception, etc.).
    /// </summary>
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

    /// <summary>
    /// Indicates that the debuggee process terminated with an exit code.
    /// </summary>
    public class ExitedEvent : Event
    {
        public ExitedEvent(int exCode)
            : base("exited", new { exitCode = exCode }) { }
    }

    /// <summary>
    /// Indicates that the debugging session has ended.
    /// </summary>
    public class TerminatedEvent : Event
    {
        public TerminatedEvent()
            : base("terminated") { }
    }

    /// <summary>
    /// Announces that a thread started or exited.
    /// </summary>
    public class ThreadEvent : Event
    {
        public ThreadEvent(string reasn, int tid)
            : base("thread", new { reason = reasn, threadId = tid }) { }
    }

    /// <summary>
    /// Sends console output (stdout/stderr/custom channels) back to VS Code.
    /// </summary>
    public class OutputEvent : Event
    {
        public OutputEvent(string cat, string outpt)
            : base("output", new { category = cat, output = outpt }) { }
    }

    // ---- Response -------------------------------------------------------------------------

    /// <summary>
    /// Advertises which DAP features the NovaSharp adapter implements.
    /// </summary>
    public class Capabilities : ResponseBody
    {
        /// <summary>
        /// True when the adapter expects a configurationDone request.
        /// </summary>
        public bool SupportsConfigurationDoneRequest { get; init; }

        /// <summary>
        /// True when function breakpoints are supported.
        /// </summary>
        public bool SupportsFunctionBreakpoints { get; init; }

        /// <summary>
        /// True when conditional breakpoints are supported.
        /// </summary>
        public bool SupportsConditionalBreakpoints { get; init; }

        /// <summary>
        /// True when hover evaluations do not cause side-effects.
        /// </summary>
        public bool SupportsEvaluateForHovers { get; init; }

        /// <summary>
        /// Exception filters surfaced to the client (NovaSharp uses an empty list today).
        /// </summary>
        public IReadOnlyList<object> ExceptionBreakpointFilters { get; init; } =
            Array.Empty<object>();
    }

    /// <summary>
    /// Carries detailed error information for failed requests.
    /// </summary>
    public class ErrorResponseBody : ResponseBody
    {
        /// <summary>
        /// Detailed adapter message describing why the request failed.
        /// </summary>
        public Message Error { get; private set; }

        public ErrorResponseBody(Message error)
        {
            Error = error;
        }
    }

    /// <summary>
    /// Response payload that transfers stack frames to VS Code.
    /// </summary>
    public class StackTraceResponseBody : ResponseBody
    {
        /// <summary>
        /// Stack frames returned to the client.
        /// </summary>
        public IReadOnlyList<StackFrame> StackFrames { get; }

        public StackTraceResponseBody(List<StackFrame> frames = null)
        {
            if (frames == null)
            {
                StackFrames = Array.Empty<StackFrame>();
            }
            else
            {
                StackFrames = frames.ToArray();
            }
        }
    }

    /// <summary>
    /// Response payload that lists scopes for the active stack frame.
    /// </summary>
    public class ScopesResponseBody : ResponseBody
    {
        /// <summary>
        /// Scopes available for the selected stack frame.
        /// </summary>
        public IReadOnlyList<Scope> Scopes { get; }

        public ScopesResponseBody(List<Scope> scps = null)
        {
            if (scps == null)
            {
                Scopes = Array.Empty<Scope>();
            }
            else
            {
                Scopes = scps.ToArray();
            }
        }
    }

    /// <summary>
    /// Response payload that returns the variables for a scope or reference.
    /// </summary>
    public class VariablesResponseBody : ResponseBody
    {
        /// <summary>
        /// Variables expanded for the requested reference.
        /// </summary>
        public IReadOnlyList<Variable> Variables { get; }

        public VariablesResponseBody(List<Variable> vars = null)
        {
            if (vars == null)
            {
                Variables = Array.Empty<Variable>();
            }
            else
            {
                Variables = vars.ToArray();
            }
        }
    }

    /// <summary>
    /// Response payload that enumerates available threads.
    /// </summary>
    public class ThreadsResponseBody : ResponseBody
    {
        /// <summary>
        /// Threads currently being tracked by the adapter.
        /// </summary>
        public IReadOnlyList<Thread> Threads { get; }

        public ThreadsResponseBody(List<Thread> vars = null)
        {
            if (vars == null)
            {
                Threads = Array.Empty<Thread>();
            }
            else
            {
                Threads = vars.ToArray();
            }
        }
    }

    /// <summary>
    /// Response payload for Evaluate requests.
    /// </summary>
    public class EvaluateResponseBody : ResponseBody
    {
        /// <summary>
        /// Stringified evaluation result.
        /// </summary>
        public string Result { get; private set; }

        /// <summary>
        /// Friendly type name for the evaluation result.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Reference used to expand any nested values.
        /// </summary>
        public int VariablesReference { get; private set; }

        public EvaluateResponseBody(string value, int reff = 0)
        {
            Result = value;
            VariablesReference = reff;
        }
    }

    /// <summary>
    /// Response payload that confirms which breakpoints were registered.
    /// </summary>
    public class SetBreakpointsResponseBody : ResponseBody
    {
        /// <summary>
        /// Mirrors the breakpoints acknowledged by the adapter.
        /// </summary>
        public IReadOnlyList<Breakpoint> Breakpoints { get; }

        public SetBreakpointsResponseBody(List<Breakpoint> bpts = null)
        {
            if (bpts == null)
            {
                Breakpoints = Array.Empty<Breakpoint>();
            }
            else
            {
                Breakpoints = bpts.ToArray();
            }
        }
    }

    // ---- The Session --------------------------------------------------------

    /// <summary>
    /// Base class that implements the VS Code Debug Adapter Protocol request/response loop.
    /// </summary>
    public abstract class DebugSession : ProtocolServer
    {
        private readonly bool _debuggerLinesStartAt1;
        private readonly bool _debuggerPathsAreUri;
        private bool _clientLinesStartAt1 = true;
        private bool _clientPathsAreUri = true;

        protected DebugSession(bool debuggerLinesStartAt1, bool debuggerPathsAreUri = false)
        {
            _debuggerLinesStartAt1 = debuggerLinesStartAt1;
            _debuggerPathsAreUri = debuggerPathsAreUri;
        }

        /// <summary>
        /// Sends a successful response to VS Code, optionally including a body.
        /// </summary>
        public void SendResponse(Response response, ResponseBody body = null)
        {
            if (body != null)
            {
                response.SetBody(body);
            }
            SendMessage(response);
        }

        /// <summary>
        /// Sends an error response with a formatted <see cref="Message"/> payload.
        /// </summary>
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

        /// <summary>
        /// Routes incoming requests to concrete adapter overrides.
        /// </summary>
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

        /// <summary>
        /// Handles the <c>initialize</c> request and returns adapter capabilities.
        /// </summary>
        public abstract void Initialize(Response response, Table args);

        /// <summary>
        /// Handles launch-style requests (attach-to-process semantics already covered elsewhere).
        /// </summary>
        public abstract void Launch(Response response, Table arguments);

        /// <summary>
        /// Handles attach requests from VS Code.
        /// </summary>
        public abstract void Attach(Response response, Table arguments);

        /// <summary>
        /// Performs adapter-specific cleanup when VS Code disconnects.
        /// </summary>
        public abstract void Disconnect(Response response, Table arguments);

        /// <summary>
        /// Optional override that applies function breakpoints.
        /// </summary>
        public virtual void SetFunctionBreakpoints(Response response, Table arguments) { }

        /// <summary>
        /// Optional override that applies exception breakpoints.
        /// </summary>
        public virtual void SetExceptionBreakpoints(Response response, Table arguments) { }

        /// <summary>
        /// Applies source line breakpoints for the specified file.
        /// </summary>
        public abstract void SetBreakpoints(Response response, Table arguments);

        /// <summary>
        /// Resumes execution.
        /// </summary>
        public abstract void Continue(Response response, Table arguments);

        /// <summary>
        /// Executes a step-over.
        /// </summary>
        public abstract void Next(Response response, Table arguments);

        /// <summary>
        /// Executes a step-in.
        /// </summary>
        public abstract void StepIn(Response response, Table arguments);

        /// <summary>
        /// Executes a step-out.
        /// </summary>
        public abstract void StepOut(Response response, Table arguments);

        /// <summary>
        /// Requests that execution pause at the next opportunity.
        /// </summary>
        public abstract void Pause(Response response, Table arguments);

        /// <summary>
        /// Returns the current call stack.
        /// </summary>
        public abstract void StackTrace(Response response, Table arguments);

        /// <summary>
        /// Returns the scopes for the selected frame.
        /// </summary>
        public abstract void Scopes(Response response, Table arguments);

        /// <summary>
        /// Returns the variables for a given scope/reference.
        /// </summary>
        public abstract void Variables(Response response, Table arguments);

        /// <summary>
        /// Optionally serves source contents to VS Code when the debugger owns the file.
        /// </summary>
        public virtual void Source(Response response, Table arguments)
        {
            SendErrorResponse(response, 1020, "Source not supported");
        }

        /// <summary>
        /// Returns the thread list.
        /// </summary>
        public abstract void Threads(Response response, Table arguments);

        /// <summary>
        /// Evaluates an expression (watches, REPL, hover).
        /// </summary>
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
