namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
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
            Instruction meta = _rootChunk.code[baseAddress];

            // skip nops
            while (meta.OpCode == OpCode.Nop)
            {
                baseAddress++;
                meta = _rootChunk.code[baseAddress];
            }

            if (meta.OpCode != OpCode.Meta)
            {
                return null;
            }

            return meta;
        }

        internal void AttachDebugger(IDebugger debugger)
        {
            _debug.debuggerAttached = debugger;
            _debug.lineBasedBreakPoints =
                (debugger.GetDebuggerCaps() & DebuggerCaps.HasLineBasedBreakpoints) != 0;
            debugger.SetDebugService(new DebugService(_script, this));
        }

        internal bool DebuggerEnabled
        {
            get { return _debug.debuggerEnabled; }
            set { _debug.debuggerEnabled = value; }
        }

        private void ListenDebugger(Instruction instr, int instructionPtr)
        {
            bool isOnDifferentRef = false;

            if (instr.SourceCodeRef != null && _debug.lastHlRef != null)
            {
                if (_debug.lineBasedBreakPoints)
                {
                    isOnDifferentRef =
                        instr.SourceCodeRef.SourceIdx != _debug.lastHlRef.SourceIdx
                        || instr.SourceCodeRef.FromLine != _debug.lastHlRef.FromLine;
                }
                else
                {
                    isOnDifferentRef = instr.SourceCodeRef != _debug.lastHlRef;
                }
            }
            else if (_debug.lastHlRef == null)
            {
                isOnDifferentRef = instr.SourceCodeRef != null;
            }

            if (
                _debug.debuggerAttached.IsPauseRequested()
                || (
                    instr.SourceCodeRef != null
                    && instr.SourceCodeRef.breakpoint
                    && isOnDifferentRef
                )
            )
            {
                _debug.debuggerCurrentAction = DebuggerAction.ActionType.None;
                _debug.debuggerCurrentActionTarget = -1;
            }

            switch (_debug.debuggerCurrentAction)
            {
                case DebuggerAction.ActionType.Run:
                    if (_debug.lineBasedBreakPoints)
                    {
                        _debug.lastHlRef = instr.SourceCodeRef;
                    }

                    return;
                case DebuggerAction.ActionType.ByteCodeStepOver:
                    if (_debug.debuggerCurrentActionTarget != instructionPtr)
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.ByteCodeStepOut:
                case DebuggerAction.ActionType.StepOut:
                    if (_executionStack.Count >= _debug.exStackDepthAtStep)
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.StepIn:
                    if (
                        (_executionStack.Count >= _debug.exStackDepthAtStep)
                        && (instr.SourceCodeRef == null || instr.SourceCodeRef == _debug.lastHlRef)
                    )
                    {
                        return;
                    }

                    break;
                case DebuggerAction.ActionType.StepOver:
                    if (
                        instr.SourceCodeRef == null
                        || instr.SourceCodeRef == _debug.lastHlRef
                        || _executionStack.Count > _debug.exStackDepthAtStep
                    )
                    {
                        return;
                    }

                    break;
            }

            RefreshDebugger(false, instructionPtr);

            while (true)
            {
                DebuggerAction action = _debug.debuggerAttached.GetAction(
                    instructionPtr,
                    instr.SourceCodeRef
                );

                switch (action.Action)
                {
                    case DebuggerAction.ActionType.StepIn:
                    case DebuggerAction.ActionType.StepOver:
                    case DebuggerAction.ActionType.StepOut:
                    case DebuggerAction.ActionType.ByteCodeStepOut:
                        _debug.debuggerCurrentAction = action.Action;
                        _debug.lastHlRef = instr.SourceCodeRef;
                        _debug.exStackDepthAtStep = _executionStack.Count;
                        return;
                    case DebuggerAction.ActionType.ByteCodeStepIn:
                        _debug.debuggerCurrentAction = DebuggerAction.ActionType.ByteCodeStepIn;
                        _debug.debuggerCurrentActionTarget = -1;
                        return;
                    case DebuggerAction.ActionType.ByteCodeStepOver:
                        _debug.debuggerCurrentAction = DebuggerAction.ActionType.ByteCodeStepOver;
                        _debug.debuggerCurrentActionTarget = instructionPtr + 1;
                        return;
                    case DebuggerAction.ActionType.Run:
                        _debug.debuggerCurrentAction = DebuggerAction.ActionType.Run;
                        _debug.lastHlRef = instr.SourceCodeRef;
                        _debug.debuggerCurrentActionTarget = -1;
                        return;
                    case DebuggerAction.ActionType.ToggleBreakpoint:
                        ToggleBreakPoint(action, null);
                        RefreshDebugger(true, instructionPtr);
                        break;
                    case DebuggerAction.ActionType.ResetBreakpoints:
                        ResetBreakPoints(action);
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
                    case DebuggerAction.ActionType.None:
                    default:
                        break;
                }
            }
        }

        private void ResetBreakPoints(DebuggerAction action)
        {
            SourceCode src = _script.GetSourceCode(action.SourceId);
            ResetBreakPoints(src, new HashSet<int>(action.Lines));
        }

        internal HashSet<int> ResetBreakPoints(SourceCode src, HashSet<int> lines)
        {
            HashSet<int> result = new();

            foreach (SourceRef srf in src.Refs)
            {
                if (srf.CannotBreakpoint)
                {
                    continue;
                }

                srf.breakpoint = lines.Contains(srf.FromLine);

                if (srf.breakpoint)
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
                        srf.breakpoint = !srf.breakpoint;
                    }
                    else
                    {
                        srf.breakpoint = state.Value;
                    }

                    if (srf.breakpoint)
                    {
                        _debug.breakPoints.Add(srf);
                    }
                    else
                    {
                        _debug.breakPoints.Remove(srf);
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
                        nearest.breakpoint = !nearest.breakpoint;
                    }
                    else
                    {
                        nearest.breakpoint = state.Value;
                    }

                    if (nearest.breakpoint)
                    {
                        _debug.breakPoints.Add(nearest);
                    }
                    else
                    {
                        _debug.breakPoints.Remove(nearest);
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

            List<DynamicExpression> watchList = _debug.debuggerAttached.GetWatchItems();
            List<WatchItem> callStack = Debugger_GetCallStack(sref);
            List<WatchItem> watches = Debugger_RefreshWatches(context, watchList);
            List<WatchItem> vstack = Debugger_RefreshVStack();
            List<WatchItem> locals = Debugger_RefreshLocals(context);
            List<WatchItem> threads = Debugger_RefreshThreads(context);

            _debug.debuggerAttached.Update(WatchType.CallStack, callStack);
            _debug.debuggerAttached.Update(WatchType.Watches, watches);
            _debug.debuggerAttached.Update(WatchType.VStack, vstack);
            _debug.debuggerAttached.Update(WatchType.Locals, locals);
            _debug.debuggerAttached.Update(WatchType.Threads, threads);

            if (hard)
            {
                _debug.debuggerAttached.RefreshBreakpoints(_debug.breakPoints);
            }
        }

        private List<WatchItem> Debugger_RefreshThreads(ScriptExecutionContext context)
        {
            List<Processor> coroutinesStack =
                _parent != null ? _parent._coroutinesStack : _coroutinesStack;

            return coroutinesStack
                .Select(c => new WatchItem()
                {
                    Address = c.AssociatedCoroutine.ReferenceId,
                    Name = "coroutine #" + c.AssociatedCoroutine.ReferenceId.ToString(),
                })
                .ToList();
        }

        private List<WatchItem> Debugger_RefreshVStack()
        {
            List<WatchItem> lwi = new();
            for (int i = 0; i < Math.Min(32, _valueStack.Count); i++)
            {
                lwi.Add(new WatchItem() { Address = i, Value = _valueStack.Peek(i) });
            }

            return lwi;
        }

        private List<WatchItem> Debugger_RefreshWatches(
            ScriptExecutionContext context,
            List<DynamicExpression> watchList
        )
        {
            return watchList.Select(w => Debugger_RefreshWatch(context, w)).ToList();
        }

        private List<WatchItem> Debugger_RefreshLocals(ScriptExecutionContext context)
        {
            List<WatchItem> locals = new();
            CallStackItem top = _executionStack.Peek();

            if (top != null && top.debugSymbols != null && top.localScope != null)
            {
                int len = Math.Min(top.debugSymbols.Length, top.localScope.Length);

                for (int i = 0; i < len; i++)
                {
                    locals.Add(
                        new WatchItem()
                        {
                            IsError = false,
                            LValue = top.debugSymbols[i],
                            Value = top.localScope[i],
                            Name = top.debugSymbols[i].i_Name,
                        }
                    );
                }
            }

            return locals;
        }

        private WatchItem Debugger_RefreshWatch(
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
                    Name = dynExpr.expressionCode,
                };
            }
            catch (Exception ex)
            {
                return new WatchItem()
                {
                    IsError = true,
                    Value = DynValue.NewString(ex.Message),
                    Name = dynExpr.expressionCode,
                };
            }
        }

        internal List<WatchItem> Debugger_GetCallStack(SourceRef startingRef)
        {
            List<WatchItem> wis = new();

            for (int i = 0; i < _executionStack.Count; i++)
            {
                CallStackItem c = _executionStack.Peek(i);

                Instruction instruction = _rootChunk.code[c.debugEntryPoint];

                string callname = instruction.OpCode == OpCode.Meta ? instruction.Name : null;

                if (c.clrFunction != null)
                {
                    wis.Add(
                        new WatchItem()
                        {
                            Address = -1,
                            BasePtr = -1,
                            RetAddress = c.returnAddress,
                            Location = startingRef,
                            Name = c.clrFunction.Name,
                        }
                    );
                }
                else
                {
                    wis.Add(
                        new WatchItem()
                        {
                            Address = c.debugEntryPoint,
                            BasePtr = c.basePointer,
                            RetAddress = c.returnAddress,
                            Name = callname,
                            Location = startingRef,
                        }
                    );
                }

                startingRef = c.callingSourceRef;

                if (c.continuation != null)
                {
                    wis.Add(
                        new WatchItem()
                        {
                            Name = c.continuation.Name,
                            Location = SourceRef.GetClrLocation(),
                        }
                    );
                }
            }

            return wis;
        }
    }
}
