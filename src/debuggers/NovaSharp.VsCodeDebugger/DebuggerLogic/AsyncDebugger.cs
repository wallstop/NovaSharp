namespace NovaSharp.VsCodeDebugger.DebuggerLogic
{
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;

    internal sealed class AsyncDebugger : IDebugger
    {
        private static readonly object SAsyncDebuggerIdLock = new();
        private static int _sAsyncDebuggerIdCounter = 0;

        private readonly object _lock = new();
        private IAsyncDebuggerClient _client;
        private DebuggerAction _pendingAction = null;

        private readonly List<WatchItem>[] _watchItems;
        private readonly Dictionary<int, SourceCode> _sourcesMap = new();
        private readonly Dictionary<int, string> _sourcesOverride = new();
        private readonly Func<SourceCode, string> _sourceFinder;

        public DebugService DebugService { get; private set; }

        public Regex ErrorRegex { get; set; }

        public Script Script { get; private set; }

        public bool PauseRequested { get; set; }

        public string Name { get; set; }

        public int Id { get; private set; }

        public AsyncDebugger(Script script, Func<SourceCode, string> sourceFinder, string name)
        {
            lock (SAsyncDebuggerIdLock)
            {
                Id = _sAsyncDebuggerIdCounter++;
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

        private void Sleep(int v)
        {
#if DOTNET_CORE
            System.Threading.Tasks.Task.Delay(10).Wait();
#else
            System.Threading.Thread.Sleep(10);
#endif
        }

        private DynamicExpression CreateDynExpr(string code)
        {
            try
            {
                return Script.CreateDynamicExpression(code);
            }
            catch (Exception ex)
            {
                return Script.CreateConstantDynamicExpression(code, DynValue.NewString(ex.Message));
            }
        }

        List<DynamicExpression> IDebugger.GetWatchItems()
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
                catch
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

        private string GetFooterForTempFile()
        {
            return "\n\n"
                + "----------------------------------------------------------------------------------------------------------\n"
                + "-- This file has been generated by the debugger as a placeholder for a script snippet stored in memory. --\n"
                + "-- If you restart the host process, the contents of this file are not valid anymore.                    --\n"
                + "----------------------------------------------------------------------------------------------------------\n";
        }

        public string GetSourceFile(int sourceId)
        {
            if (_sourcesOverride.ContainsKey(sourceId))
            {
                return _sourcesOverride[sourceId];
            }
            else if (_sourcesMap.ContainsKey(sourceId))
            {
                return _sourcesMap[sourceId].Name;
            }

            return null;
        }

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

        public List<WatchItem> GetWatches(WatchType watchType)
        {
            return _watchItems[(int)watchType];
        }

        public SourceCode GetSource(int id)
        {
            if (_sourcesMap.ContainsKey(id))
            {
                return _sourcesMap[id];
            }

            return null;
        }

        public SourceCode FindSourceByName(string path)
        {
            // we use case insensitive match - be damned if you have files which differ only by
            // case in the same directory on Unix.
            path = path.Replace('\\', '/').ToUpperInvariant();

            foreach (KeyValuePair<int, string> kvp in _sourcesOverride)
            {
                if (kvp.Value.Replace('\\', '/').ToUpperInvariant() == path)
                {
                    return _sourcesMap[kvp.Key];
                }
            }

            return _sourcesMap.Values.FirstOrDefault(s =>
                s.Name.Replace('\\', '/').ToUpperInvariant() == path
            );
        }

        void IDebugger.SetDebugService(DebugService debugService)
        {
            DebugService = debugService;
        }

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
