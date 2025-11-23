namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Debugging;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;

    // This part is practically written procedural style - it looks more like C than C#.
    // This is intentional so to avoid this-calls and virtual-calls as much as possible.
    // Same reason for the "sealed" declaration.
    internal sealed partial class Processor
    {
        internal Instruction FindMeta(ref int baseAddress)
        {
            Instruction meta = _rootChunk.Code[baseAddress];

            // skip nops
            while (meta.OpCode == OpCode.Nop)
            {
                baseAddress++;
                meta = _rootChunk.Code[baseAddress];
            }

            if (meta.OpCode != OpCode.Meta)
            {
                return null;
            }

            return meta;
        }

        internal void AttachDebugger(IDebugger debugger)
        {
            _debug.DebuggerAttached = debugger;
            _debug.LineBasedBreakPoints =
                (debugger.GetDebuggerCaps() & DebuggerCaps.HasLineBasedBreakpoints) != 0;
            debugger.SetDebugService(new DebugService(_script, this));
        }

        internal void AttachDebuggerForTests(IDebugger debugger, bool lineBasedBreakpoints)
        {
            _debug.DebuggerAttached = debugger;
            _debug.LineBasedBreakPoints = lineBasedBreakpoints;
        }

        internal void ConfigureDebuggerActionForTests(
            DebuggerAction.ActionType action,
            int actionTarget,
            int executionStackDepth,
            SourceRef lastHighlight
        )
        {
            _debug.DebuggerCurrentAction = action;
            _debug.DebuggerCurrentActionTarget = actionTarget;
            _debug.ExecutionStackDepthAtStep = executionStackDepth;
            _debug.LastHlRef = lastHighlight;
        }

        internal DebuggerAction.ActionType GetDebuggerActionForTests()
        {
            return _debug.DebuggerCurrentAction;
        }

        internal int GetDebuggerActionTargetForTests()
        {
            return _debug.DebuggerCurrentActionTarget;
        }

        internal SourceRef GetLastHighlightForTests()
        {
            return _debug.LastHlRef;
        }

        internal void ListenDebuggerForTests(Instruction instr, int instructionPtr)
        {
            ListenDebugger(instr, instructionPtr);
        }

        internal IReadOnlyList<SourceRef> GetBreakpointsForTests()
        {
            return _debug.BreakPoints.ToArray();
        }

        internal void ClearBreakpointsForTests()
        {
            _debug.BreakPoints.Clear();
        }

        internal bool DebuggerEnabled
        {
            get { return _debug.DebuggerEnabled; }
            set { _debug.DebuggerEnabled = value; }
        }

        private void ListenDebugger(Instruction instr, int instructionPtr)
        {
            bool isOnDifferentRef = false;

            if (instr.SourceCodeRef != null && _debug.LastHlRef != null)
            {
                if (_debug.LineBasedBreakPoints)
                {
                    isOnDifferentRef =
                        instr.SourceCodeRef.SourceIdx != _debug.LastHlRef.SourceIdx
                        || instr.SourceCodeRef.FromLine != _debug.LastHlRef.FromLine;
                }
                else
                {
                    isOnDifferentRef = instr.SourceCodeRef != _debug.LastHlRef;
                }
            }
            else if (_debug.LastHlRef == null)
            {
                isOnDifferentRef = instr.SourceCodeRef != null;
            }

            if (
                _debug.DebuggerAttached.IsPauseRequested()
                || (
                    instr.SourceCodeRef != null
                    && instr.SourceCodeRef.Breakpoint
                    && isOnDifferentRef
                )
            )
            {
                _debug.DebuggerCurrentAction = default;
                _debug.DebuggerCurrentActionTarget = -1;
            }

            switch (_debug.DebuggerCurrentAction)
            {
                case DebuggerAction.ActionType.Run:
                    if (_debug.LineBasedBreakPoints)
                    {
                        _debug.LastHlRef = instr.SourceCodeRef;
                    }

                    return;
                case DebuggerAction.ActionType.ByteCodeStepOver:
                    if (_debug.DebuggerCurrentActionTarget != instructionPtr)
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.ByteCodeStepOut:
                case DebuggerAction.ActionType.StepOut:
                    if (_executionStack.Count >= _debug.ExecutionStackDepthAtStep)
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.StepIn:
                    if (
                        (_executionStack.Count >= _debug.ExecutionStackDepthAtStep)
                        && (instr.SourceCodeRef == null || instr.SourceCodeRef == _debug.LastHlRef)
                    )
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.StepOver:
                    if (
                        instr.SourceCodeRef == null
                        || instr.SourceCodeRef == _debug.LastHlRef
                        || _executionStack.Count > _debug.ExecutionStackDepthAtStep
                    )
                    {
                        return;
                    }

                    break;
            }

            RefreshDebugger(false, instructionPtr);

            while (true)
            {
                DebuggerAction action = _debug.DebuggerAttached.GetAction(
                    instructionPtr,
                    instr.SourceCodeRef
                );

                switch (action.Action)
                {
                    case DebuggerAction.ActionType.StepIn:
                    case DebuggerAction.ActionType.StepOver:
                    case DebuggerAction.ActionType.StepOut:
                    case DebuggerAction.ActionType.ByteCodeStepOut:
                        _debug.DebuggerCurrentAction = action.Action;
                        _debug.LastHlRef = instr.SourceCodeRef;
                        _debug.ExecutionStackDepthAtStep = _executionStack.Count;
                        return;
                    case DebuggerAction.ActionType.ByteCodeStepIn:
                        _debug.DebuggerCurrentAction = DebuggerAction.ActionType.ByteCodeStepIn;
                        _debug.DebuggerCurrentActionTarget = -1;
                        return;
                    case DebuggerAction.ActionType.ByteCodeStepOver:
                        _debug.DebuggerCurrentAction = DebuggerAction.ActionType.ByteCodeStepOver;
                        _debug.DebuggerCurrentActionTarget = instructionPtr + 1;
                        return;
                    case DebuggerAction.ActionType.Run:
                        _debug.DebuggerCurrentAction = DebuggerAction.ActionType.Run;
                        _debug.LastHlRef = instr.SourceCodeRef;
                        _debug.DebuggerCurrentActionTarget = -1;
                        return;
                    case DebuggerAction.ActionType.ToggleBreakpoint:
                        ToggleBreakPoint(action, null);
                        RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.ResetBreakpoints:
                        ResetBreakpoints(action);
                        RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.SetBreakpoint:
                        ToggleBreakPoint(action, true);
                        RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.ClearBreakpoint:
                        ToggleBreakPoint(action, false);
                        RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.Refresh:
                        RefreshDebugger(false, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.HardRefresh:
                        RefreshDebugger(true, instructionPtr);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ResetBreakpoints(DebuggerAction action)
        {
            SourceCode src = _script.GetSourceCode(action.SourceId);
            ResetBreakpoints(src, new HashSet<int>(action.Lines));
        }

        internal HashSet<int> ResetBreakpoints(SourceCode src, HashSet<int> lines)
        {
            HashSet<int> result = new();

            foreach (SourceRef srf in src.Refs)
            {
                if (srf.CannotBreakpoint)
                {
                    continue;
                }

                srf.Breakpoint = lines.Contains(srf.FromLine);

                if (srf.Breakpoint)
                {
                    result.Add(srf.FromLine);
                }
            }

            return result;
        }

        private bool ToggleBreakPoint(DebuggerAction action, bool? state)
        {
            SourceCode src = _script.GetSourceCode(action.SourceId);

            bool found = false;
            foreach (SourceRef srf in src.Refs)
            {
                if (srf.CannotBreakpoint)
                {
                    continue;
                }

                if (srf.IncludesLocation(action.SourceId, action.SourceLine, action.SourceCol))
                {
                    found = true;

                    //System.Diagnostics.Debug.WriteLine(string.Format("BRK: found {0} for {1} on contains", srf, srf.Type));

                    if (state == null)
                    {
                        srf.Breakpoint = !srf.Breakpoint;
                    }
                    else
                    {
                        srf.Breakpoint = state.Value;
                    }

                    if (srf.Breakpoint)
                    {
                        _debug.BreakPoints.Add(srf);
                    }
                    else
                    {
                        _debug.BreakPoints.Remove(srf);
                    }
                }
            }

            if (!found)
            {
                int minDistance = int.MaxValue;
                SourceRef nearest = null;

                foreach (SourceRef srf in src.Refs)
                {
                    if (srf.CannotBreakpoint)
                    {
                        continue;
                    }

                    int dist = srf.GetLocationDistance(
                        action.SourceId,
                        action.SourceLine,
                        action.SourceCol
                    );

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = srf;
                    }
                }

                if (nearest != null)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("BRK: found {0} for {1} on distance {2}", nearest, nearest.Type, minDistance));

                    if (state == null)
                    {
                        nearest.Breakpoint = !nearest.Breakpoint;
                    }
                    else
                    {
                        nearest.Breakpoint = state.Value;
                    }

                    if (nearest.Breakpoint)
                    {
                        _debug.BreakPoints.Add(nearest);
                    }
                    else
                    {
                        _debug.BreakPoints.Remove(nearest);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private void RefreshDebugger(bool hard, int instructionPtr)
        {
            SourceRef sref = GetCurrentSourceRef(instructionPtr);
            ScriptExecutionContext context = new(this, null, sref);

            List<DynamicExpression> watchList = _debug.DebuggerAttached.GetWatchItems();
            List<WatchItem> callStack = GetDebuggerCallStack(sref);
            List<WatchItem> watches = RefreshDebuggerWatches(context, watchList);
            List<WatchItem> vstack = RefreshValueStack();
            List<WatchItem> locals = RefreshDebuggerLocals(context);
            List<WatchItem> threads = RefreshDebuggerThreads(context);

            _debug.DebuggerAttached.Update(WatchType.CallStack, callStack);
            _debug.DebuggerAttached.Update(WatchType.Watches, watches);
            _debug.DebuggerAttached.Update(WatchType.VStack, vstack);
            _debug.DebuggerAttached.Update(WatchType.Locals, locals);
            _debug.DebuggerAttached.Update(WatchType.Threads, threads);

            if (hard)
            {
                _debug.DebuggerAttached.RefreshBreakpoints(_debug.BreakPoints);
            }
        }

        private List<WatchItem> RefreshDebuggerThreads(ScriptExecutionContext context)
        {
            List<Processor> coroutinesStack =
                _parent != null ? _parent._coroutinesStack : _coroutinesStack;

            return coroutinesStack
                .Select(c => new WatchItem()
                {
                    Address = c.AssociatedCoroutine.ReferenceId,
                    Name = FormattableString.Invariant(
                        $"coroutine #{c.AssociatedCoroutine.ReferenceId}"
                    ),
                })
                .ToList();
        }

        private List<WatchItem> RefreshValueStack()
        {
            List<WatchItem> lwi = new();
            for (int i = 0; i < Math.Min(32, _valueStack.Count); i++)
            {
                lwi.Add(new WatchItem() { Address = i, Value = _valueStack.Peek(i) });
            }

            return lwi;
        }

        private List<WatchItem> RefreshDebuggerWatches(
            ScriptExecutionContext context,
            List<DynamicExpression> watchList
        )
        {
            return watchList.Select(w => RefreshDebuggerWatch(context, w)).ToList();
        }

        private List<WatchItem> RefreshDebuggerLocals(ScriptExecutionContext context)
        {
            List<WatchItem> locals = new();
            CallStackItem top = _executionStack.Peek();

            if (top != null && top.DebugSymbols != null && top.LocalScope != null)
            {
                int len = Math.Min(top.DebugSymbols.Length, top.LocalScope.Length);

                for (int i = 0; i < len; i++)
                {
                    locals.Add(
                        new WatchItem()
                        {
                            IsError = false,
                            LValue = top.DebugSymbols[i],
                            Value = top.LocalScope[i],
                            Name = top.DebugSymbols[i].NameValue,
                        }
                    );
                }
            }

            return locals;
        }

        private WatchItem RefreshDebuggerWatch(
            ScriptExecutionContext context,
            DynamicExpression dynExpr
        )
        {
            try
            {
                SymbolRef l = dynExpr.FindSymbol(context);
                DynValue v = dynExpr.Evaluate(context);

                return new WatchItem()
                {
                    IsError = dynExpr.IsConstant(),
                    LValue = l,
                    Value = v,
                    Name = dynExpr.ExpressionCode,
                };
            }
            catch (Exception ex)
            {
                return new WatchItem()
                {
                    IsError = true,
                    Value = DynValue.NewString(ex.Message),
                    Name = dynExpr.ExpressionCode,
                };
            }
        }

        internal List<WatchItem> GetDebuggerCallStack(SourceRef startingRef)
        {
            List<WatchItem> wis = new();

            for (int i = 0; i < _executionStack.Count; i++)
            {
                CallStackItem c = _executionStack.Peek(i);

                Instruction instruction = _rootChunk.Code[c.DebugEntryPoint];

                string callname = instruction.OpCode == OpCode.Meta ? instruction.Name : null;

                if (c.ClrFunction != null)
                {
                    wis.Add(
                        new WatchItem()
                        {
                            Address = -1,
                            BasePtr = -1,
                            RetAddress = c.ReturnAddress,
                            Location = startingRef,
                            Name = c.ClrFunction.Name,
                        }
                    );
                }
                else
                {
                    wis.Add(
                        new WatchItem()
                        {
                            Address = c.DebugEntryPoint,
                            BasePtr = c.BasePointer,
                            RetAddress = c.ReturnAddress,
                            Name = callname,
                            Location = startingRef,
                        }
                    );
                }

                startingRef = c.CallingSourceRef;

                if (c.Continuation != null)
                {
                    wis.Add(
                        new WatchItem()
                        {
                            Name = c.Continuation.Name,
                            Location = SourceRef.GetClrLocation(),
                        }
                    );
                }
            }

            return wis;
        }
    }
}
