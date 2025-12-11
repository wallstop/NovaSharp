namespace WallstopStudios.NovaSharp.VsCodeDebugger.DebuggerLogic
{
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// Implements the <see cref="IDebugger"/> contract for the VS Code adapter, coordinating
    /// Script pauses and forwarding state to an <see cref="IAsyncDebuggerClient"/>.
    /// </summary>
    internal sealed class AsyncDebugger : IDebugger
    {
        private static readonly object SAsyncDebuggerIdLock = new();
        private static int AsyncDebuggerIdCounter;

        private readonly object _lock = new();
        private IAsyncDebuggerClient _client;
        private DebuggerAction _pendingAction;

        private readonly List<WatchItem>[] _watchItems;
        private readonly Dictionary<int, SourceCode> _sourcesMap = new();
        private readonly Dictionary<int, string> _sourcesOverride = new();
        private readonly Func<SourceCode, string> _sourceFinder;

        /// <summary>
        /// Gets the <see cref="DebugService"/> instance exposed by the runtime.
        /// </summary>
        public DebugService DebugService { get; private set; }

        /// <summary>
        /// Gets or sets the regular expression that decides whether runtime exceptions trigger a pause.
        /// </summary>
        public Regex ErrorRegex { get; set; }

        /// <summary>
        /// Gets the script currently being debugged.
        /// </summary>
        public Script Script { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether a pause is pending.
        /// </summary>
        public bool PauseRequested { get; set; }

        /// <summary>
        /// Gets or sets the debugger-friendly session name (displayed in VS Code).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the unique identifier assigned to this async debugger instance.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncDebugger"/> class.
        /// </summary>
        /// <param name="script">Script to debug.</param>
        /// <param name="sourceFinder">Callback used to map <see cref="SourceCode"/> to filesystem paths.</param>
        /// <param name="name">Human-readable session name.</param>
        public AsyncDebugger(Script script, Func<SourceCode, string> sourceFinder, string name)
        {
            lock (SAsyncDebuggerIdLock)
            {
                Id = AsyncDebuggerIdCounter++;
            }

            _sourceFinder = sourceFinder;
            ErrorRegex = new Regex(@"\A.*\Z");
            Script = script;
            _watchItems = new List<WatchItem>[(int)WatchType.MaxValue];
            Name = name;

            for (int i = 0; i < _watchItems.Length; i++)
            {
                _watchItems[i] = new List<WatchItem>(64);
            }
        }

        /// <summary>
        /// Gets or sets the debugger client that receives callbacks for state changes.
        /// </summary>
        public IAsyncDebuggerClient Client
        {
            get { return _client; }
            set
            {
                lock (_lock)
                {
                    if (_client != null && _client != value)
                    {
                        _client.Unbind();
                    }

                    if (value != null)
                    {
                        for (int i = 0; i < Script.SourceCodeCount; i++)
                        {
                            if (_sourcesMap.ContainsKey(i))
                            {
                                value.OnSourceCodeChanged(i);
                            }
                        }
                    }

                    _client = value;
                }
            }
        }

        DebuggerAction IDebugger.GetAction(int ip, SourceRef sourceref)
        {
            PauseRequested = false;

            lock (_lock)
            {
                if (Client != null)
                {
                    Client.SendStopEvent();
                }
            }

            while (true)
            {
                lock (_lock)
                {
                    if (Client == null)
                    {
                        return new DebuggerAction(Script?.TimeProvider)
                        {
                            Action = DebuggerAction.ActionType.Run,
                        };
                    }

                    if (_pendingAction != null)
                    {
                        DebuggerAction action = _pendingAction;
                        _pendingAction = null;
                        return action;
                    }
                }

                Sleep(10);
            }
        }

        /// <summary>
        /// Enqueues a debugger action requested by the VS Code client.
        /// </summary>
        /// <param name="action">The action to process.</param>
        public void QueueAction(DebuggerAction action)
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_pendingAction == null)
                    {
                        _pendingAction = action;
                        break;
                    }
                }

                Sleep(10);
            }
        }

        private static void Sleep(int milliseconds)
        {
#if DOTNET_CORE
            System.Threading.Tasks.Task.Delay(milliseconds).Wait();
#else
            System.Threading.Thread.Sleep(milliseconds);
#endif
        }

        private DynamicExpression CreateDynExpr(string code)
        {
            try
            {
                return Script.CreateDynamicExpression(code);
            }
            catch (InterpreterException ex)
            {
                string message = ex.DecoratedMessage ?? ex.Message;
                return Script.CreateConstantDynamicExpression(code, DynValue.NewString(message));
            }
        }

        IReadOnlyList<DynamicExpression> IDebugger.GetWatchItems()
        {
            return new List<DynamicExpression>();
        }

        bool IDebugger.IsPauseRequested()
        {
            return PauseRequested;
        }

        void IDebugger.RefreshBreakpoints(IEnumerable<SourceRef> refs) { }

        void IDebugger.SetByteCode(string[] byteCode) { }

        void IDebugger.SetSourceCode(SourceCode sourceCode)
        {
            _sourcesMap[sourceCode.SourceId] = sourceCode;

            bool invalidFile = false;

            string file = _sourceFinder(sourceCode);

            if (!string.IsNullOrEmpty(file))
            {
                try
                {
                    if (!File.Exists(file))
                    {
                        invalidFile = true;
                    }
                }
                catch (IOException)
                {
                    invalidFile = true;
                }
                catch (UnauthorizedAccessException)
                {
                    invalidFile = true;
                }
                catch (ArgumentException)
                {
                    invalidFile = true;
                }
                catch (NotSupportedException)
                {
                    invalidFile = true;
                }
            }
            else
            {
                invalidFile = true;
            }

            if (invalidFile)
            {
                file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".lua");
                File.WriteAllText(file, sourceCode.Code + GetFooterForTempFile());
                _sourcesOverride[sourceCode.SourceId] = file;
            }
            else if (file != sourceCode.Name)
            {
                _sourcesOverride[sourceCode.SourceId] = file;
            }

            lock (_lock)
            {
                if (Client != null)
                {
                    Client.OnSourceCodeChanged(sourceCode.SourceId);
                }
            }
        }

        private static string GetFooterForTempFile()
        {
            return "\n\n"
                + "----------------------------------------------------------------------------------------------------------\n"
                + "-- This file has been generated by the debugger as a placeholder for a script snippet stored in memory. --\n"
                + "-- If you restart the host process, the contents of this file are not valid anymore.                    --\n"
                + "----------------------------------------------------------------------------------------------------------\n";
        }

        /// <summary>
        /// Gets the path used to locate the specified source identifier on disk.
        /// </summary>
        /// <param name="sourceId">Lua source identifier.</param>
        /// <returns>Absolute path to the cached file, or the script name when no override exists.</returns>
        public string GetSourceFile(int sourceId)
        {
            if (_sourcesOverride.TryGetValue(sourceId, out string overridePath))
            {
                return overridePath;
            }

            if (_sourcesMap.TryGetValue(sourceId, out SourceCode mappedSource))
            {
                return mappedSource.Name;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the debugger had to materialize an on-disk file for the given script chunk.
        /// </summary>
        /// <param name="sourceId">Source identifier.</param>
        /// <returns><c>true</c> when the path was generated by the debugger.</returns>
        public bool IsSourceOverride(int sourceId)
        {
            return (_sourcesOverride.ContainsKey(sourceId));
        }

        void IDebugger.SignalExecutionEnded()
        {
            lock (_lock)
            {
                if (Client != null)
                {
                    Client.OnExecutionEnded();
                }
            }
        }

        bool IDebugger.SignalRuntimeException(ScriptRuntimeException ex)
        {
            lock (_lock)
            {
                if (Client == null)
                {
                    return false;
                }
            }

            Client.OnException(ex);
            PauseRequested = ErrorRegex.IsMatch(ex.Message);
            return PauseRequested;
        }

        void IDebugger.Update(WatchType watchType, IEnumerable<WatchItem> items)
        {
            List<WatchItem> list = _watchItems[(int)watchType];

            list.Clear();
            list.AddRange(items);

            lock (_lock)
            {
                if (Client != null)
                {
                    Client.OnWatchesUpdated(watchType);
                }
            }
        }

        /// <summary>
        /// Returns the cached watch data for a specific channel (call stack, locals, watches, etc.).
        /// </summary>
        /// <param name="watchType">Watch category.</param>
        /// <returns>The mutable watch list backing the specified type.</returns>
        public List<WatchItem> GetWatches(WatchType watchType)
        {
            return _watchItems[(int)watchType];
        }

        /// <summary>
        /// Retrieves the <see cref="SourceCode"/> entry associated with the identifier, if known.
        /// </summary>
        /// <param name="id">Source identifier.</param>
        /// <returns>The cached source or <c>null</c> when missing.</returns>
        public SourceCode GetSource(int id)
        {
            if (_sourcesMap.TryGetValue(id, out SourceCode source))
            {
                return source;
            }

            return null;
        }

        /// <summary>
        /// Locates a cached source entry by its canonicalized path (case-insensitive, slash-normalized).
        /// </summary>
        /// <param name="path">Path provided by the debugger.</param>
        /// <returns>The first matching source entry, or <c>null</c> when not found.</returns>
        public SourceCode FindSourceByName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string normalizedPath = path.NormalizeDirectorySeparators('/');

            foreach (KeyValuePair<int, string> kvp in _sourcesOverride)
            {
                string candidate = kvp.Value.NormalizeDirectorySeparators('/');
                if (string.Equals(candidate, normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return _sourcesMap[kvp.Key];
                }
            }

            return _sourcesMap.Values.FirstOrDefault(s =>
                string.Equals(
                    s.Name.NormalizeDirectorySeparators('/'),
                    normalizedPath,
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }

        void IDebugger.SetDebugService(DebugService debugService)
        {
            DebugService = debugService;
        }

        /// <summary>
        /// Evaluates an arbitrary Lua expression in the context of the running script.
        /// </summary>
        /// <param name="expression">Expression text.</param>
        /// <returns>The resulting <see cref="DynValue"/>.</returns>
        public DynValue Evaluate(string expression)
        {
            DynamicExpression expr = CreateDynExpr(expression);
            return expr.Evaluate();
        }

        DebuggerCaps IDebugger.GetDebuggerCaps()
        {
            return DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;
        }
    }
}

#endif
