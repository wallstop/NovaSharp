#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NovaSharp.VsCodeDebugger.DebuggerLogic;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Debugging;
using NovaSharp.VsCodeDebugger.SDK;

namespace NovaSharp.VsCodeDebugger
{
    /// <summary>
    /// Class implementing a debugger allowing attaching from a Visual Studio Code debugging session.
    /// </summary>
    public class NovaSharpVsCodeDebugServer : IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<AsyncDebugger> _debuggerList = new List<AsyncDebugger>();
        private AsyncDebugger _current = null;
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private bool _started = false;
        private readonly int _port;

        /// <summary>
        /// Initializes a new instance of the <see cref="NovaSharpVsCodeDebugServer" /> class.
        /// </summary>
        /// <param name="port">The port on which the debugger listens. It's recommended to use 41912.</param>
        public NovaSharpVsCodeDebugServer(int port = 41912)
        {
            _port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NovaSharpVsCodeDebugServer" /> class with a default script.
        /// Note that for this specific script, it will NOT attach the debugger to the script.
        /// </summary>
        /// <param name="script">The script object to debug.</param>
        /// <param name="port">The port on which the debugger listens. It's recommended to use 41912 unless you are going to keep more than one script object around.</param>
        /// <param name="sourceFinder">A function which gets in input a source code and returns the path to
        /// source file to use. It can return null and in that case (or if the file cannot be found)
        /// a temporary file will be generated on the fly.</param>
        [Obsolete("Use the constructor taking only a port, and the 'Attach' method instead.")]
        public NovaSharpVsCodeDebugServer(
            Script script,
            int port,
            Func<SourceCode, string> sourceFinder = null
        )
        {
            _port = port;
            _current = new AsyncDebugger(script, sourceFinder ?? (s => s.Name), "Default script");
            _debuggerList.Add(_current);
        }

        /// <summary>
        /// Attaches the specified script to the debugger
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="name">The name of the script.</param>
        /// <param name="sourceFinder">A function which gets in input a source code and returns the path to
        /// source file to use. It can return null and in that case (or if the file cannot be found)
        /// a temporary file will be generated on the fly.</param>
        /// <exception cref="ArgumentException">If the script has already been attached to this debugger.</exception>
        public void AttachToScript(
            Script script,
            string name,
            Func<SourceCode, string> sourceFinder = null
        )
        {
            lock (_lock)
            {
                if (_debuggerList.Any(d => d.Script == script))
                {
                    throw new ArgumentException("Script already attached to this debugger.");
                }

                AsyncDebugger debugger = new AsyncDebugger(
                    script,
                    sourceFinder ?? (s => s.Name),
                    name
                );
                script.AttachDebugger(debugger);
                _debuggerList.Add(debugger);

                if (_current == null)
                {
                    _current = debugger;
                }
            }
        }

        /// <summary>
        /// Gets a list of the attached debuggers by id and name
        /// </summary>
        public IEnumerable<KeyValuePair<int, string>> GetAttachedDebuggersByIdAndName()
        {
            lock (_lock)
            {
                return _debuggerList
                    .OrderBy(d => d.Id)
                    .Select(d => new KeyValuePair<int, string>(d.Id, d.Name))
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the current script by ID (see GetAttachedDebuggersByIdAndName).
        /// New vscode connections will attach to this debugger ID. Changing the current ID does NOT disconnect
        /// connected clients.
        /// </summary>
        public int? CurrentId
        {
            get
            {
                lock (_lock)
                {
                    return _current != null ? _current.Id : (int?)null;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value == null)
                    {
                        _current = null;
                        return;
                    }

                    AsyncDebugger current = (_debuggerList.FirstOrDefault(d => d.Id == value));

                    if (current == null)
                    {
                        throw new ArgumentException("Cannot find debugger with given Id.");
                    }

                    _current = current;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current script. New vscode connections will attach to this script. Changing the current script does NOT disconnect
        /// connected clients.
        /// </summary>
        public Script Current
        {
            get
            {
                lock (_lock)
                {
                    return _current != null ? _current.Script : null;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value == null)
                    {
                        _current = null;
                        return;
                    }

                    AsyncDebugger current = (_debuggerList.FirstOrDefault(d => d.Script == value));

                    if (current == null)
                    {
                        throw new ArgumentException(
                            "Cannot find debugger with given script associated."
                        );
                    }

                    _current = current;
                }
            }
        }

        /// <summary>
        /// Detaches the specified script. The debugger attached to that script will get disconnected.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <exception cref="ArgumentException">Thrown if the script cannot be found.</exception>
        public void Detach(Script script)
        {
            lock (_lock)
            {
                AsyncDebugger removed = _debuggerList.FirstOrDefault(d => d.Script == script);

                if (removed == null)
                {
                    throw new ArgumentException("Cannot detach script - not found.");
                }

                removed.Client = null;

                _debuggerList.Remove(removed);

                if (_current == removed)
                {
                    if (_debuggerList.Count > 0)
                    {
                        _current = _debuggerList[_debuggerList.Count - 1];
                    }
                    else
                    {
                        _current = null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a delegate which will be called when logging messages are generated
        /// </summary>
        public Action<string> Logger { get; set; }

        /// <summary>
        /// Gets the debugger object. Obsolete, use the new interface using the Attach method instead.
        /// </summary>
        [Obsolete("Use the Attach method instead.")]
        public IDebugger GetDebugger()
        {
            lock (_lock)
            {
                return _current;
            }
        }

        /// <summary>
        /// Stops listening
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot stop; server was not started.</exception>
        public void Dispose()
        {
            _stopEvent.Set();
        }

        /// <summary>
        /// Starts listening on the localhost for incoming connections.
        /// </summary>
        public NovaSharpVsCodeDebugServer Start()
        {
            lock (_lock)
            {
                if (_started)
                {
                    throw new InvalidOperationException(
                        "Cannot start; server has already been started."
                    );
                }

                _stopEvent.Reset();

                TcpListener serverSocket = null;
                serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), _port);
                serverSocket.Start();

                SpawnThread(
                    "VsCodeDebugServer_" + _port.ToString(),
                    () => ListenThread(serverSocket)
                );

                _started = true;

                return this;
            }
        }

        private void ListenThread(TcpListener serverSocket)
        {
            try
            {
                while (!_stopEvent.WaitOne(0))
                {
#if DOTNET_CORE
                    Task<Socket> task = serverSocket.AcceptSocketAsync();
                    task.Wait();
                    Socket clientSocket = task.Result;
#else
                    var clientSocket = serverSocket.AcceptSocket();
#endif

                    string sessionId = Guid.NewGuid().ToString("N");
                    Log(
                        "[{0}] : Accepted connection from client {1}",
                        sessionId,
                        clientSocket.RemoteEndPoint
                    );

                    SpawnThread(
                        "VsCodeDebugSession_" + sessionId,
                        () =>
                        {
                            using (NetworkStream networkStream = new NetworkStream(clientSocket))
                            {
                                try
                                {
                                    RunSession(sessionId, networkStream);
                                }
                                catch (Exception ex)
                                {
                                    Log("[{0}] : Error : {1}", ex.Message);
                                }
                            }

#if DOTNET_CORE
                            clientSocket.Dispose();
#else
                            clientSocket.Close();
#endif
                            Log("[{0}] : Client connection closed", sessionId);
                        }
                    );
                }
            }
            catch (Exception e)
            {
                Log("Fatal error in listening thread : {0}", e.Message);
            }
            finally
            {
                if (serverSocket != null)
                {
                    serverSocket.Stop();
                }
            }
        }

        private void RunSession(string sessionId, NetworkStream stream)
        {
            DebugSession debugSession = null;

            lock (_lock)
            {
                if (_current != null)
                {
                    debugSession = new NovaSharpDebugSession(this, _current);
                }
                else
                {
                    debugSession = new EmptyDebugSession(this);
                }
            }

            debugSession.ProcessLoop(stream, stream);
        }

        private void Log(string format, params object[] args)
        {
            Action<string> logger = Logger;

            if (logger != null)
            {
                string msg = string.Format(format, args);
                logger(msg);
            }
        }

        private static void SpawnThread(string name, Action threadProc)
        {
#if DOTNET_CORE
            Task.Run(() => threadProc());
#else
            new System.Threading.Thread(() => threadProc())
            {
                IsBackground = true,
                Name = name,
            }.Start();
#endif
        }
    }
}

#else
using System;
using System.Collections.Generic;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Debugging;

namespace NovaSharp.VsCodeDebugger
{
    public class NovaSharpVsCodeDebugServer : IDisposable
    {
        public NovaSharpVsCodeDebugServer(int port = 41912) { }

        [Obsolete("Use the constructor taking only a port, and the 'Attach' method instead.")]
        public NovaSharpVsCodeDebugServer(
            Script script,
            int port,
            Func<SourceCode, string> sourceFinder = null
        ) { }

        public void AttachToScript(
            Script script,
            string name,
            Func<SourceCode, string> sourceFinder = null
        ) { }

        public IEnumerable<KeyValuePair<int, string>> GetAttachedDebuggersByIdAndName()
        {
            yield break;
        }

        public int? CurrentId
        {
            get { return null; }
            set { }
        }

        public Script Current
        {
            get { return null; }
            set { }
        }

        /// <summary>
        /// Detaches the specified script. The debugger attached to that script will get disconnected.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <exception cref="ArgumentException">Thrown if the script cannot be found.</exception>
        public void Detach(Script script) { }

        public Action<string> Logger { get; set; }

        [Obsolete("Use the Attach method instead.")]
        public IDebugger GetDebugger()
        {
            return null;
        }

        public void Dispose() { }

        public NovaSharpVsCodeDebugServer Start()
        {
            return this;
        }
    }
}
#endif
