namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.PredefinedUserData;

    /// <content>
    /// Hosts the VM instruction loop and per-opcode execution helpers.
    /// </content>
    internal sealed partial class Processor
    {
        private const int YieldSpecialTrap = -99;

        internal long AutoYieldCounter;

        private DynValue ProcessingLoop(int instructionPtr)
        {
            // This is the main loop of the processor, has a weird control flow and needs to be as fast as possible.
            // This sentence is just a convoluted way to say "don't complain about gotos".

            long executedInstructions = 0;
            bool canAutoYield =
                (AutoYieldCounter > 0) && _canYield && (State != CoroutineState.Main);
            bool shouldYieldToCaller = false;

            repeat_execution:

            try
            {
                while (true)
                {
                    shouldYieldToCaller = false;
                    Instruction i = _rootChunk.Code[instructionPtr];

                    if (_debug.DebuggerAttached != null)
                    {
                        ListenDebugger(i, instructionPtr);
                    }

                    ++executedInstructions;

                    if (canAutoYield && executedInstructions > AutoYieldCounter)
                    {
                        _savedInstructionPtr = instructionPtr;
                        return DynValue.NewForcedYieldReq();
                    }

                    ++instructionPtr;

                    switch (i.OpCode)
                    {
                        case OpCode.Nop:
                        case OpCode.Debug:
                        case OpCode.Meta:
                            break;
                        case OpCode.Pop:
                            _valueStack.RemoveLast(i.NumVal);
                            break;
                        case OpCode.Copy:
                            _valueStack.Push(_valueStack.Peek(i.NumVal));
                            break;
                        case OpCode.Swap:
                            ExecSwap(i);
                            break;
                        case OpCode.Literal:
                            _valueStack.Push(i.Value);
                            break;
                        case OpCode.Add:
                            instructionPtr = ExecAdd(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Concat:
                            instructionPtr = ExecConcat(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Neg:
                            instructionPtr = ExecNeg(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Sub:
                            instructionPtr = ExecSub(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Mul:
                            instructionPtr = ExecMul(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Div:
                            instructionPtr = ExecDiv(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Mod:
                            instructionPtr = ExecMod(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.FloorDiv:
                            instructionPtr = ExecFloorDiv(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Power:
                            instructionPtr = ExecPower(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.BitAnd:
                            instructionPtr = ExecBitAnd(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.BitOr:
                            instructionPtr = ExecBitOr(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.BitXor:
                            instructionPtr = ExecBitXor(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Eq:
                            instructionPtr = ExecEq(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.LessEq:
                            instructionPtr = ExecLessEq(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Less:
                            instructionPtr = ExecLess(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Len:
                            instructionPtr = ExecLen(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.BitNot:
                            instructionPtr = ExecBitNot(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Call:
                        case OpCode.ThisCall:
                            instructionPtr = InternalExecCall(
                                i.NumVal,
                                instructionPtr,
                                null,
                                null,
                                i.OpCode == OpCode.ThisCall,
                                i.Name
                            );
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Scalar:
                            _valueStack.Push(_valueStack.Pop().ToScalar());
                            break;
                        case OpCode.Not:
                            ExecNot(i);
                            break;
                        case OpCode.ShiftLeft:
                            instructionPtr = ExecShiftLeft(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.ShiftRight:
                            instructionPtr = ExecShiftRight(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.CNot:
                            ExecCNot(i);
                            break;
                        case OpCode.JfOrPop:
                        case OpCode.JtOrPop:
                            instructionPtr = ExecShortCircuitingOperator(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.JNil:
                            {
                                DynValue v = _valueStack.Pop().ToScalar();

                                if (v.Type == DataType.Nil || v.Type == DataType.Void)
                                {
                                    instructionPtr = i.NumVal;
                                }
                            }
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Jf:
                            instructionPtr = JumpBool(i, false, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Jump:
                            instructionPtr = i.NumVal;
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.MkTuple:
                            ExecMkTuple(i);
                            break;
                        case OpCode.Enter:
                            ExecEnter(i);
                            break;
                        case OpCode.Leave:
                            ExecLeave(i);
                            break;
                        case OpCode.Exit:
                            ExecExit(i);
                            break;
                        case OpCode.Clean:
                            ClearBlockData(i);
                            break;
                        case OpCode.Closure:
                            ExecClosure(i);
                            break;
                        case OpCode.BeginFn:
                            ExecBeginFn(i);
                            break;
                        case OpCode.ToBool:
                            _valueStack.Push(
                                DynValue.NewBoolean(_valueStack.Pop().ToScalar().CastToBool())
                            );
                            break;
                        case OpCode.Args:
                            ExecArgs(i);
                            break;
                        case OpCode.Ret:
                            instructionPtr = ExecRet(i);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            if (instructionPtr < 0)
                            {
                                goto return_to_native_code;
                            }

                            break;
                        case OpCode.Incr:
                            ExecIncr(i);
                            break;
                        case OpCode.ToNum:
                            ExecToNum(i);
                            break;
                        case OpCode.JFor:
                            instructionPtr = ExecJFor(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.NewTable:
                            if (i.NumVal == 0)
                            {
                                _valueStack.Push(DynValue.NewTable(_script));
                            }
                            else
                            {
                                _valueStack.Push(DynValue.NewPrimeTable());
                            }

                            break;
                        case OpCode.IterPrep:
                            ExecIterPrep(i);
                            break;
                        case OpCode.IterUpd:
                            ExecIterUpd(i);
                            break;
                        case OpCode.ExpTuple:
                            ExecExpTuple(i);
                            break;
                        case OpCode.Local:
                            DynValue[] scope = _executionStack.Peek().LocalScope;
                            int index = i.Symbol.IndexValue;
                            _valueStack.Push(scope[index].AsReadOnly());
                            break;
                        case OpCode.Upvalue:
                            _valueStack.Push(
                                _executionStack
                                    .Peek()
                                    .ClosureScope[i.Symbol.IndexValue]
                                    .AsReadOnly()
                            );
                            break;
                        case OpCode.StoreUpv:
                            ExecStoreUpv(i);
                            break;
                        case OpCode.StoreLcl:
                            ExecStoreLcl(i);
                            break;
                        case OpCode.TblInitN:
                            ExecTblInitN(i);
                            break;
                        case OpCode.TblInitI:
                            ExecTblInitI(i);
                            break;
                        case OpCode.Index:
                        case OpCode.IndexN:
                        case OpCode.IndexL:
                            instructionPtr = ExecIndex(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.IndexSet:
                        case OpCode.IndexSetN:
                        case OpCode.IndexSetL:
                            instructionPtr = ExecIndexSet(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            break;
                        case OpCode.Invalid:
                            throw new NotImplementedException($"Invalid opcode : {i.Name}");
                        default:
                            throw new NotImplementedException(
                                $"Execution for {i.OpCode} not implemented yet!"
                            );
                    }

                    if (shouldYieldToCaller)
                    {
                        goto yield_to_calling_coroutine;
                    }
                }

                yield_to_calling_coroutine:

                DynValue yieldRequest = _valueStack.Pop().ToScalar();

                if (_canYield)
                {
                    return yieldRequest;
                }
                else if (State == CoroutineState.Main)
                {
                    throw ScriptRuntimeException.CannotYieldMain();
                }
                else
                {
                    throw ScriptRuntimeException.CannotYield();
                }
            }
            catch (InterpreterException ex)
            {
                FillDebugData(ex, instructionPtr);

                if (!(ex is ScriptRuntimeException exception))
                {
                    ex.Rethrow();
                    throw;
                }

                if (_debug.DebuggerAttached != null)
                {
                    if (_debug.DebuggerAttached.SignalRuntimeException(exception))
                    {
                        if (instructionPtr >= 0 && instructionPtr < _rootChunk.Code.Count)
                        {
                            ListenDebugger(_rootChunk.Code[instructionPtr], instructionPtr);
                        }
                    }
                }

                for (int i = 0; i < _executionStack.Count; i++)
                {
                    CallStackItem c = _executionStack.Peek(i);

                    if (c.ErrorHandlerBeforeUnwind != null)
                    {
                        exception.DecoratedMessage = PerformMessageDecorationBeforeUnwind(
                            c.ErrorHandlerBeforeUnwind,
                            exception.DecoratedMessage,
                            GetCurrentSourceRef(instructionPtr)
                        );
                    }
                }

                DynValue closeError = DynValue.NewString(exception.DecoratedMessage);

                while (_executionStack.Count > 0)
                {
                    CallStackItem csi = PopToBasePointer();

                    CloseAllPendingBlocks(csi, closeError);

                    if (csi.ErrorHandler != null)
                    {
                        instructionPtr = csi.ReturnAddress;

                        if (csi.ClrFunction == null)
                        {
                            int argscnt = (int)(_valueStack.Pop().Number);
                            _valueStack.RemoveLast(argscnt + 1);
                        }

                        DynValue[] cbargs = new DynValue[]
                        {
                            DynValue.NewString(exception.DecoratedMessage),
                        };

                        DynValue handled = csi.ErrorHandler.Invoke(
                            new ScriptExecutionContext(
                                this,
                                csi.ErrorHandler,
                                GetCurrentSourceRef(instructionPtr)
                            ),
                            cbargs
                        );

                        _valueStack.Push(handled);

                        goto repeat_execution;
                    }
                    else if ((csi.Flags & CallStackItemFlags.EntryPoint) != 0)
                    {
                        exception.Rethrow();
                        throw;
                    }
                }

                exception.Rethrow();
                throw;
            }

            return_to_native_code:
            return _valueStack.Pop();
        }

        /// <summary>
        /// Invokes the provided message handler to decorate an error before the stack unwinds.
        /// </summary>
        /// <param name="messageHandler">Function handling the error.</param>
        /// <param name="decoratedMessage">Existing decorated message (if any).</param>
        /// <param name="sourceRef">Source reference associated with the error.</param>
        /// <returns>The new decorated message.</returns>
        internal string PerformMessageDecorationBeforeUnwind(
            DynValue messageHandler,
            string decoratedMessage,
            SourceRef sourceRef
        )
        {
            try
            {
                DynValue[] args = new DynValue[] { DynValue.NewString(decoratedMessage) };
                DynValue ret = DynValue.Nil;

                if (messageHandler.Type == DataType.Function)
                {
                    ret = Call(messageHandler, args);
                }
                else if (messageHandler.Type == DataType.ClrFunction)
                {
                    ScriptExecutionContext ctx = new(this, messageHandler.Callback, sourceRef);
                    ret = messageHandler.Callback.Invoke(ctx, args);
                }
                else
                {
                    throw new ScriptRuntimeException("error handler not set to a function");
                }

                string newmsg = ret.ToPrintString();
                if (newmsg != null)
                {
                    return newmsg;
                }
            }
            catch (ScriptRuntimeException innerEx)
            {
                return innerEx.Message + "." + decoratedMessage;
            }

            return decoratedMessage;
        }

        private void AssignLocal(SymbolRef symref, DynValue value)
        {
            CallStackItem stackframe = _executionStack.Peek();

            DynValue slot = stackframe.LocalScope[symref.IndexValue];
            if (slot == null)
            {
                stackframe.LocalScope[symref.IndexValue] = slot = DynValue.NewNil();
            }

            bool isToBeClosed = IsSymbolToBeClosed(stackframe, symref.IndexValue);

            if (isToBeClosed)
            {
                EnsureToBeClosedValue(symref, value);

                if (!slot.IsNil())
                {
                    DynValue previous = slot.Clone();
                    CloseValue(symref, previous, DynValue.Nil);
                }
            }

            slot.Assign(value);
        }

        private static bool IsSymbolToBeClosed(CallStackItem stackframe, int index)
        {
            return stackframe.ToBeClosedIndices != null
                && stackframe.ToBeClosedIndices.Contains(index);
        }

        private void ExecEnter(Instruction i)
        {
            ClearBlockData(i);

            CallStackItem stackframe = _executionStack.Peek();

            SymbolRef[] closers = i.SymbolList ?? Array.Empty<SymbolRef>();

            if (stackframe.BlocksToClose == null)
            {
                stackframe.BlocksToClose = new List<List<SymbolRef>>();
            }

            stackframe.BlocksToClose.Add(new List<SymbolRef>(closers));

            if (closers.Length > 0)
            {
                if (stackframe.ToBeClosedIndices == null)
                {
                    stackframe.ToBeClosedIndices = new HashSet<int>();
                }

                foreach (SymbolRef sym in closers)
                {
                    stackframe.ToBeClosedIndices.Add(sym.IndexValue);
                }
            }
        }

        private void ExecLeave(Instruction i)
        {
            CallStackItem stackframe = _executionStack.Peek();
            CloseCurrentBlock(stackframe, DynValue.Nil);
            ClearBlockData(i);
        }

        private void ExecExit(Instruction i)
        {
            CallStackItem stackframe = _executionStack.Peek();
            CloseCurrentBlock(stackframe, DynValue.Nil);
            ClearBlockData(i);
        }

        private static bool ShouldIgnoreToBeClosedValue(DynValue value)
        {
            return value == null
                || value.IsNil()
                || (value.Type == DataType.Boolean && value.Boolean == false);
        }

        private void EnsureToBeClosedValue(SymbolRef symbol, DynValue value)
        {
            DynValue candidate = value?.ToScalar() ?? DynValue.Nil;

            if (ShouldIgnoreToBeClosedValue(candidate))
            {
                return;
            }

            DynValue metamethod = GetMetamethodRaw(candidate, "__close");

            if (metamethod == null || metamethod.IsNil())
            {
                throw ScriptRuntimeException.CloseMetamethodExpected(candidate);
            }
        }

        private void CloseValue(SymbolRef symbol, DynValue value, DynValue error)
        {
            DynValue scalar = value?.ToScalar() ?? DynValue.Nil;

            if (ShouldIgnoreToBeClosedValue(scalar))
            {
                return;
            }

            DynValue metamethod = GetMetamethodRaw(scalar, "__close");

            if (metamethod == null || metamethod.IsNil())
            {
                throw ScriptRuntimeException.CloseMetamethodExpected(scalar);
            }

            DynValue err = error ?? DynValue.Nil;

            GetScript().Call(metamethod, scalar, err);
        }

        private void CloseCurrentBlock(CallStackItem stackframe, DynValue error)
        {
            if (stackframe.BlocksToClose == null || stackframe.BlocksToClose.Count == 0)
            {
                return;
            }

            List<SymbolRef> closers = stackframe.BlocksToClose[^1];
            stackframe.BlocksToClose.RemoveAt(stackframe.BlocksToClose.Count - 1);

            if (closers.Count == 0)
            {
                return;
            }

            if (stackframe.ToBeClosedIndices != null)
            {
                foreach (SymbolRef sym in closers)
                {
                    stackframe.ToBeClosedIndices.Remove(sym.IndexValue);
                }
            }

            for (int idx = closers.Count - 1; idx >= 0; idx--)
            {
                SymbolRef sym = closers[idx];
                DynValue slot = stackframe.LocalScope[sym.IndexValue];

                if (slot != null && !slot.IsNil())
                {
                    DynValue previous = slot.Clone();
                    CloseValue(sym, previous, error);
                    slot.Assign(DynValue.Nil);
                }
            }
        }

        private void CloseAllPendingBlocks(CallStackItem stackframe, DynValue error)
        {
            if (stackframe.BlocksToClose == null || stackframe.BlocksToClose.Count == 0)
            {
                return;
            }

            while (stackframe.BlocksToClose.Count > 0)
            {
                List<SymbolRef> closers = stackframe.BlocksToClose[^1];
                stackframe.BlocksToClose.RemoveAt(stackframe.BlocksToClose.Count - 1);

                if (stackframe.ToBeClosedIndices != null && closers.Count > 0)
                {
                    foreach (SymbolRef sym in closers)
                    {
                        stackframe.ToBeClosedIndices.Remove(sym.IndexValue);
                    }
                }

                if (closers.Count == 0)
                {
                    continue;
                }

                for (int idx = closers.Count - 1; idx >= 0; idx--)
                {
                    SymbolRef sym = closers[idx];
                    DynValue slot = stackframe.LocalScope[sym.IndexValue];

                    if (slot != null && !slot.IsNil())
                    {
                        DynValue previous = slot.Clone();
                        CloseValue(sym, previous, error);
                        slot.Assign(DynValue.Nil);
                    }
                }
            }

            stackframe.ToBeClosedIndices?.Clear();
        }

        private void ExecStoreLcl(Instruction i)
        {
            DynValue value = GetStoreValue(i);
            SymbolRef symref = i.Symbol;

            AssignLocal(symref, value);
        }

        private void ExecStoreUpv(Instruction i)
        {
            DynValue value = GetStoreValue(i);
            SymbolRef symref = i.Symbol;

            CallStackItem stackframe = _executionStack.Peek();

            DynValue v = stackframe.ClosureScope[symref.IndexValue];
            if (v == null)
            {
                stackframe.ClosureScope[symref.IndexValue] = v = DynValue.NewNil();
            }

            v.Assign(value);
        }

        private void ExecSwap(Instruction i)
        {
            DynValue v1 = _valueStack.Peek(i.NumVal);
            DynValue v2 = _valueStack.Peek(i.NumVal2);

            _valueStack.Set(i.NumVal, v2);
            _valueStack.Set(i.NumVal2, v1);
        }

        private DynValue GetStoreValue(Instruction i)
        {
            int stackofs = i.NumVal;
            int tupleidx = i.NumVal2;

            DynValue v = _valueStack.Peek(stackofs);

            if (v.Type == DataType.Tuple)
            {
                return (tupleidx < v.Tuple.Length) ? v.Tuple[tupleidx] : DynValue.NewNil();
            }
            else
            {
                return (tupleidx == 0) ? v : DynValue.NewNil();
            }
        }

        private void ExecClosure(Instruction i)
        {
            Closure c = new(
                _script,
                i.NumVal,
                i.SymbolList,
                i.SymbolList.Select(s => GetUpvalueSymbol(s)).ToList()
            );

            _valueStack.Push(DynValue.NewClosure(c));
        }

        private DynValue GetUpvalueSymbol(SymbolRef s)
        {
            if (s.Type == SymbolRefType.Local)
            {
                return _executionStack.Peek().LocalScope[s.IndexValue];
            }
            else if (s.Type == SymbolRefType.Upvalue)
            {
                return _executionStack.Peek().ClosureScope[s.IndexValue];
            }
            else
            {
                throw new Exception("unsupported symbol type");
            }
        }

        private void ExecMkTuple(Instruction i)
        {
            Slice<DynValue> slice = new(_valueStack, _valueStack.Count - i.NumVal, i.NumVal, false);

            DynValue[] v = InternalAdjustTuple(slice);

            _valueStack.RemoveLast(i.NumVal);

            _valueStack.Push(DynValue.NewTuple(v));
        }

        private void ExecToNum(Instruction i)
        {
            double? v = _valueStack.Pop().ToScalar().CastToNumber();
            if (v.HasValue)
            {
                _valueStack.Push(DynValue.NewNumber(v.Value));
            }
            else
            {
                throw ScriptRuntimeException.ConvertToNumberFailed(i.NumVal);
            }
        }

        private void ExecIterUpd(Instruction i)
        {
            DynValue v = _valueStack.Peek(0);
            DynValue t = _valueStack.Peek(1);
            t.Tuple[2] = v;
        }

        private void ExecExpTuple(Instruction i)
        {
            DynValue t = _valueStack.Peek(i.NumVal);

            if (t.Type == DataType.Tuple)
            {
                for (int idx = 0; idx < t.Tuple.Length; idx++)
                {
                    _valueStack.Push(t.Tuple[idx]);
                }
            }
            else
            {
                _valueStack.Push(t);
            }
        }

        private void ExecIterPrep(Instruction i)
        {
            DynValue v = _valueStack.Pop();

            if (v.Type != DataType.Tuple)
            {
                v = DynValue.NewTuple(v, DynValue.Nil, DynValue.Nil);
            }

            DynValue f = v.Tuple.Length >= 1 ? v.Tuple[0] : DynValue.Nil;
            DynValue s = v.Tuple.Length >= 2 ? v.Tuple[1] : DynValue.Nil;
            DynValue var = v.Tuple.Length >= 3 ? v.Tuple[2] : DynValue.Nil;

            // NovaSharp additions - given f, s, var
            // 1) if f is not a function and has a __iterator metamethod, call __iterator to get the triplet
            // 2) if f is a table with no __call metamethod, use a default table iterator

            if (f.Type != DataType.Function && f.Type != DataType.ClrFunction)
            {
                DynValue meta = GetMetamethod(f, "__iterator");

                if (meta != null && !meta.IsNil())
                {
                    if (meta.Type != DataType.Tuple)
                    {
                        v = GetScript().Call(meta, f, s, var);
                    }
                    else
                    {
                        v = meta;
                    }

                    f = v.Tuple.Length >= 1 ? v.Tuple[0] : DynValue.Nil;
                    s = v.Tuple.Length >= 2 ? v.Tuple[1] : DynValue.Nil;
                    var = v.Tuple.Length >= 3 ? v.Tuple[2] : DynValue.Nil;

                    _valueStack.Push(DynValue.NewTuple(f, s, var));
                    return;
                }
                else if (f.Type == DataType.Table)
                {
                    DynValue callmeta = GetMetamethod(f, "__call");

                    if (callmeta == null || callmeta.IsNil())
                    {
                        _valueStack.Push(EnumerableWrapper.ConvertTable(f.Table));
                        return;
                    }
                }
            }

            _valueStack.Push(DynValue.NewTuple(f, s, var));
        }

        private int ExecJFor(Instruction i, int instructionPtr)
        {
            double val = _valueStack.Peek(0).Number;
            double step = _valueStack.Peek(1).Number;
            double stop = _valueStack.Peek(2).Number;

            bool whileCond = (step > 0) ? val <= stop : val >= stop;

            if (!whileCond)
            {
                return i.NumVal;
            }
            else
            {
                return instructionPtr;
            }
        }

        private void ExecIncr(Instruction i)
        {
            DynValue top = _valueStack.Peek(0);
            DynValue btm = _valueStack.Peek(i.NumVal);

            if (top.ReadOnly)
            {
                _valueStack.Pop();

                if (top.ReadOnly)
                {
                    top = top.CloneAsWritable();
                }

                _valueStack.Push(top);
            }

            top.AssignNumber(top.Number + btm.Number);
        }

        private void ExecCNot(Instruction i)
        {
            DynValue v = _valueStack.Pop().ToScalar();
            DynValue not = _valueStack.Pop().ToScalar();

            if (not.Type != DataType.Boolean)
            {
                throw new InternalErrorException("CNOT had non-bool arg");
            }

            if (not.CastToBool())
            {
                _valueStack.Push(DynValue.NewBoolean(!(v.CastToBool())));
            }
            else
            {
                _valueStack.Push(DynValue.NewBoolean(v.CastToBool()));
            }
        }

        private void ExecNot(Instruction i)
        {
            DynValue v = _valueStack.Pop().ToScalar();
            _valueStack.Push(DynValue.NewBoolean(!(v.CastToBool())));
        }

        private void ExecBeginFn(Instruction i)
        {
            CallStackItem cur = _executionStack.Peek();

            cur.DebugSymbols = i.SymbolList;
            cur.LocalScope = new DynValue[i.NumVal];

            ClearBlockData(i);

            if (cur.BlocksToClose == null)
            {
                cur.BlocksToClose = new List<List<SymbolRef>>();
            }
            else
            {
                cur.BlocksToClose.Clear();
            }

            if (cur.ToBeClosedIndices == null)
            {
                cur.ToBeClosedIndices = new HashSet<int>();
            }
            else
            {
                cur.ToBeClosedIndices.Clear();
            }

            SymbolRef[] symbols = i.SymbolList;

            if (symbols == null || symbols.Length == 0)
            {
                return;
            }

            int rootBlockLastIndex = i.NumVal2;
            if (rootBlockLastIndex < 0)
            {
                return;
            }

            List<SymbolRef> rootClosers = null;

            foreach (SymbolRef symbol in symbols)
            {
                if (
                    symbol != null
                    && symbol.IsToBeClosed
                    && symbol.IndexValue <= rootBlockLastIndex
                )
                {
                    rootClosers ??= new List<SymbolRef>();
                    rootClosers.Add(symbol);
                    cur.ToBeClosedIndices.Add(symbol.IndexValue);
                }
            }

            if (rootClosers != null && rootClosers.Count > 0)
            {
                cur.BlocksToClose.Add(rootClosers);
            }
            else if (cur.BlocksToClose.Count == 0)
            {
                cur.BlocksToClose = null;
            }

            if (cur.ToBeClosedIndices.Count == 0)
            {
                cur.ToBeClosedIndices = null;
            }
        }

        private CallStackItem PopToBasePointer()
        {
            CallStackItem csi = _executionStack.Pop();
            if (csi.BasePointer >= 0)
            {
                _valueStack.CropAtCount(csi.BasePointer);
            }

            return csi;
        }

        private int PopExecStackAndCheckVStack(int vstackguard)
        {
            CallStackItem xs = _executionStack.Pop();
            if (vstackguard != xs.BasePointer)
            {
                throw new InternalErrorException("StackGuard violation");
            }

            return xs.ReturnAddress;
        }

        private IList<DynValue> CreateArgsListForFunctionCall(int numargs, int offsFromTop)
        {
            if (numargs == 0)
            {
                return Array.Empty<DynValue>();
            }

            DynValue lastParam = _valueStack.Peek(offsFromTop);

            if (lastParam.Type == DataType.Tuple && lastParam.Tuple.Length > 1)
            {
                List<DynValue> values = new();

                for (int idx = 0; idx < numargs - 1; idx++)
                {
                    values.Add(_valueStack.Peek(numargs - idx - 1 + offsFromTop));
                }

                for (int idx = 0; idx < lastParam.Tuple.Length; idx++)
                {
                    values.Add(lastParam.Tuple[idx]);
                }

                return values;
            }
            else
            {
                return new Slice<DynValue>(
                    _valueStack,
                    _valueStack.Count - numargs - offsFromTop,
                    numargs,
                    false
                );
            }
        }

        private void ExecArgs(Instruction instruction)
        {
            int numargs = (int)_valueStack.Peek(0).Number;

            // unpacks last tuple arguments to simplify a lot of code down under
            IList<DynValue> argsList = CreateArgsListForFunctionCall(numargs, 1);

            for (int i = 0; i < instruction.SymbolList.Length; i++)
            {
                if (i >= argsList.Count)
                {
                    AssignLocal(instruction.SymbolList[i], DynValue.NewNil());
                }
                else if (
                    (i == instruction.SymbolList.Length - 1)
                    && (instruction.SymbolList[i].NameValue == WellKnownSymbols.VARARGS)
                )
                {
                    int len = argsList.Count - i;
                    DynValue[] varargs = new DynValue[len];

                    for (int ii = 0; ii < len; ii++, i++)
                    {
                        varargs[ii] = argsList[i].ToScalar().CloneAsWritable();
                    }

                    AssignLocal(
                        instruction.SymbolList[^1],
                        DynValue.NewTuple(InternalAdjustTuple(varargs))
                    );
                }
                else
                {
                    AssignLocal(
                        instruction.SymbolList[i],
                        argsList[i].ToScalar().CloneAsWritable()
                    );
                }
            }
        }

        private int InternalExecCall(
            int argsCount,
            int instructionPtr,
            CallbackFunction handler = null,
            CallbackFunction Continuation = null,
            bool thisCall = false,
            string debugText = null,
            DynValue unwindHandler = null
        )
        {
            DynValue fn = _valueStack.Peek(argsCount);
            CallStackItemFlags Flags = (thisCall ? CallStackItemFlags.MethodCall : default);

            // if TCO threshold reached
            if (
                (
                    _executionStack.Count > _script.Options.TailCallOptimizationThreshold
                    && _executionStack.Count > 1
                )
                || (
                    _valueStack.Count > _script.Options.TailCallOptimizationThreshold
                    && _valueStack.Count > 1
                )
            )
            {
                // and the "will-be" return address is valid (we don't want to crash here)
                if (instructionPtr >= 0 && instructionPtr < _rootChunk.Code.Count)
                {
                    Instruction i = _rootChunk.Code[instructionPtr];

                    // and we are followed *exactly* by a RET 1
                    if (i.OpCode == OpCode.Ret && i.NumVal == 1)
                    {
                        CallStackItem csi = _executionStack.Peek();

                        // if the current stack item has no "odd" things pending and neither has the new coming one..
                        if (
                            csi.ClrFunction == null
                            && csi.Continuation == null
                            && csi.ErrorHandler == null
                            && csi.ErrorHandlerBeforeUnwind == null
                            && Continuation == null
                            && unwindHandler == null
                            && handler == null
                        )
                        {
                            instructionPtr = PerformTco(instructionPtr, argsCount);
                            Flags |= CallStackItemFlags.TailCall;
                        }
                    }
                }
            }

            if (fn.Type == DataType.ClrFunction)
            {
                //IList<DynValue> args = new Slice<DynValue>(_valueStack, _valueStack.Count - argsCount, argsCount, false);
                IList<DynValue> args = CreateArgsListForFunctionCall(argsCount, 0);
                // we expand tuples before callbacks
                // args = DynValue.ExpandArgumentsToList(args);

                // instructionPtr - 1: instructionPtr already points to the next instruction at this moment
                // but we need the current instruction here
                SourceRef sref = GetCurrentSourceRef(instructionPtr - 1);

                _executionStack.Push(
                    new CallStackItem()
                    {
                        ClrFunction = fn.Callback,
                        ReturnAddress = instructionPtr,
                        CallingSourceRef = sref,
                        BasePointer = -1,
                        ErrorHandler = handler,
                        Continuation = Continuation,
                        ErrorHandlerBeforeUnwind = unwindHandler,
                        Flags = Flags,
                    }
                );

                DynValue ret = fn.Callback.Invoke(
                    new ScriptExecutionContext(this, fn.Callback, sref),
                    args,
                    isMethodCall: thisCall
                );
                _valueStack.RemoveLast(argsCount + 1);
                _valueStack.Push(ret);

                _executionStack.Pop();

                return InternalCheckForTailRequests(null, instructionPtr);
            }
            else if (fn.Type == DataType.Function)
            {
                _valueStack.Push(DynValue.NewNumber(argsCount));
                _executionStack.Push(
                    new CallStackItem()
                    {
                        BasePointer = _valueStack.Count,
                        ReturnAddress = instructionPtr,
                        DebugEntryPoint = fn.Function.EntryPointByteCodeLocation,
                        CallingSourceRef = GetCurrentSourceRef(instructionPtr - 1), // See right above in GetCurrentSourceRef(instructionPtr - 1)
                        ClosureScope = fn.Function.ClosureContext,
                        ErrorHandler = handler,
                        Continuation = Continuation,
                        ErrorHandlerBeforeUnwind = unwindHandler,
                        Flags = Flags,
                    }
                );
                return fn.Function.EntryPointByteCodeLocation;
            }

            // fallback to __call metamethod
            DynValue m = GetMetamethod(fn, "__call");

            if (m != null && m.IsNotNil())
            {
                DynValue[] tmp = new DynValue[argsCount + 1];
                for (int i = 0; i < argsCount + 1; i++)
                {
                    tmp[i] = _valueStack.Pop();
                }

                _valueStack.Push(m);

                for (int i = argsCount; i >= 0; i--)
                {
                    _valueStack.Push(tmp[i]);
                }

                return InternalExecCall(argsCount + 1, instructionPtr, handler, Continuation);
            }

            throw ScriptRuntimeException.AttemptToCallNonFunc(fn.Type, debugText);
        }

        private int PerformTco(int instructionPtr, int argsCount)
        {
            DynValue[] args = new DynValue[argsCount + 1];

            // Remove all cur args and func ptr
            for (int i = 0; i <= argsCount; i++)
            {
                args[i] = _valueStack.Pop();
            }

            // perform a fake RET
            CallStackItem csi = PopToBasePointer();
            int retpoint = csi.ReturnAddress;
            int argscnt = (int)(_valueStack.Pop().Number);
            _valueStack.RemoveLast(argscnt + 1);

            // Re-push all cur args and func ptr
            for (int i = argsCount; i >= 0; i--)
            {
                _valueStack.Push(args[i]);
            }

            return retpoint;
        }

        private int ExecRet(Instruction i)
        {
            CallStackItem csi = _executionStack.Peek();
            int retpoint = csi.ReturnAddress;

            DynValue returnValue;

            if (i.NumVal == 0)
            {
                returnValue = DynValue.Void;
            }
            else if (i.NumVal == 1)
            {
                returnValue = _valueStack.Pop();
            }
            else
            {
                throw new InternalErrorException("RET supports only 0 and 1 ret val scenarios");
            }

            CloseAllPendingBlocks(csi, DynValue.Nil);

            CallStackItem popped = PopToBasePointer();
            System.Diagnostics.Debug.Assert(object.ReferenceEquals(csi, popped));

            int argscnt = (int)(_valueStack.Pop().Number);
            _valueStack.RemoveLast(argscnt + 1);

            _valueStack.Push(returnValue);

            if (i.NumVal == 1)
            {
                retpoint = InternalCheckForTailRequests(i, retpoint);
            }

            if (csi.Continuation != null)
            {
                _valueStack.Push(
                    csi.Continuation.Invoke(
                        new ScriptExecutionContext(this, csi.Continuation, i.SourceCodeRef),
                        new DynValue[1] { _valueStack.Pop() }
                    )
                );
            }

            return retpoint;
        }

        private int InternalCheckForTailRequests(Instruction i, int instructionPtr)
        {
            DynValue tail = _valueStack.Peek(0);

            if (tail.Type == DataType.TailCallRequest)
            {
                _valueStack.Pop(); // discard tail call request

                TailCallData tcd = tail.TailCallData;

                _valueStack.Push(tcd.Function);

                for (int ii = 0; ii < tcd.Args.Length; ii++)
                {
                    _valueStack.Push(tcd.Args[ii]);
                }

                return InternalExecCall(
                    tcd.Args.Length,
                    instructionPtr,
                    tcd.ErrorHandler,
                    tcd.Continuation,
                    false,
                    null,
                    tcd.ErrorHandlerBeforeUnwind
                );
            }
            else if (tail.Type == DataType.YieldRequest)
            {
                _savedInstructionPtr = instructionPtr;
                return YieldSpecialTrap;
            }

            return instructionPtr;
        }

        private int JumpBool(Instruction i, bool expectedValueForJump, int instructionPtr)
        {
            DynValue op = _valueStack.Pop().ToScalar();

            if (op.CastToBool() == expectedValueForJump)
            {
                return i.NumVal;
            }

            return instructionPtr;
        }

        private int ExecShortCircuitingOperator(Instruction i, int instructionPtr)
        {
            bool expectedValToShortCircuit = i.OpCode == OpCode.JtOrPop;

            DynValue op = _valueStack.Peek().ToScalar();

            if (op.CastToBool() == expectedValToShortCircuit)
            {
                return i.NumVal;
            }
            else
            {
                _valueStack.Pop();
                return instructionPtr;
            }
        }

        private int ExecAdd(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(DynValue.NewNumber(ln.Value + rn.Value));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__add", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
        }

        private int ExecSub(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(DynValue.NewNumber(ln.Value - rn.Value));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__sub", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
        }

        private int ExecMul(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(DynValue.NewNumber(ln.Value * rn.Value));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__mul", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
        }

        private int ExecMod(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                double mod = Math.IEEERemainder(ln.Value, rn.Value);
                if (mod < 0)
                {
                    mod += rn.Value;
                }

                _valueStack.Push(DynValue.NewNumber(mod));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__mod", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
        }

        private int ExecDiv(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(DynValue.NewNumber(ln.Value / rn.Value));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__div", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
        }

        private int ExecFloorDiv(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(DynValue.NewNumber(Math.Floor(ln.Value / rn.Value)));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__idiv", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
        }

        private int ExecPower(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(DynValue.NewNumber(Math.Pow(ln.Value, rn.Value)));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__pow", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
        }

        private int ExecBitAnd(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(l, r, "__band", (x, y) => x & y, instructionPtr);
        }

        private int ExecBitOr(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(l, r, "__bor", (x, y) => x | y, instructionPtr);
        }

        private int ExecBitXor(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(l, r, "__bxor", (x, y) => x ^ y, instructionPtr);
        }

        private int ExecShiftLeft(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(
                l,
                r,
                "__shl",
                (x, y) => LuaIntegerHelper.ShiftLeft(x, y),
                instructionPtr
            );
        }

        private int ExecShiftRight(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(
                l,
                r,
                "__shr",
                (x, y) => LuaIntegerHelper.ShiftRight(x, y),
                instructionPtr
            );
        }

        private int ExecBitNot(Instruction i, int instructionPtr)
        {
            DynValue value = _valueStack.Pop().ToScalar();

            if (LuaIntegerHelper.TryGetInteger(value, out long operand))
            {
                _valueStack.Push(DynValue.NewNumber(~operand));
                return instructionPtr;
            }

            int ip = InternalInvokeUnaryMetaMethod(value, "__bnot", instructionPtr);
            if (ip >= 0)
            {
                return ip;
            }

            throw ScriptRuntimeException.BitwiseOnNonInteger(value);
        }

        private int ExecBitwiseBinary(
            DynValue l,
            DynValue r,
            string metamethodName,
            Func<long, long, long> operation,
            int instructionPtr
        )
        {
            bool leftOk = LuaIntegerHelper.TryGetInteger(l, out long left);
            bool rightOk = LuaIntegerHelper.TryGetInteger(r, out long right);

            if (leftOk && rightOk)
            {
                _valueStack.Push(DynValue.NewNumber(operation(left, right)));
                return instructionPtr;
            }

            int ip = InternalInvokeBinaryMetaMethod(l, r, metamethodName, instructionPtr);
            if (ip >= 0)
            {
                return ip;
            }

            DynValue invalid = leftOk ? r : l;
            throw ScriptRuntimeException.BitwiseOnNonInteger(invalid);
        }

        private int ExecNeg(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            double? rn = r.CastToNumber();

            if (rn.HasValue)
            {
                _valueStack.Push(DynValue.NewNumber(-rn.Value));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeUnaryMetaMethod(r, "__unm", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ArithmeticOnNonNumber(r);
                }
            }
        }

        private int ExecEq(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            // first we do a brute force equals over the references
            if (ReferenceEquals(r, l))
            {
                _valueStack.Push(DynValue.True);
                return instructionPtr;
            }

            // then if they are userdatas, attempt meta
            if (l.Type == DataType.UserData || r.Type == DataType.UserData)
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__eq", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
            }

            // then if types are different, ret false
            if (r.Type != l.Type)
            {
                if (
                    (l.Type == DataType.Nil && r.Type == DataType.Void)
                    || (l.Type == DataType.Void && r.Type == DataType.Nil)
                )
                {
                    _valueStack.Push(DynValue.True);
                }
                else
                {
                    _valueStack.Push(DynValue.False);
                }

                return instructionPtr;
            }

            // then attempt metatables for tables
            if (
                (l.Type == DataType.Table)
                && (GetMetatable(l) != null)
                && (GetMetatable(l) == GetMetatable(r))
            )
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__eq", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
            }

            // else perform standard comparison
            _valueStack.Push(DynValue.NewBoolean(r.Equals(l)));
            return instructionPtr;
        }

        private int ExecLess(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            if (l.Type == DataType.Number && r.Type == DataType.Number)
            {
                _valueStack.Push(DynValue.NewBoolean(l.Number < r.Number));
            }
            else if (l.Type == DataType.String && r.Type == DataType.String)
            {
                int comparison = string.Compare(l.String, r.String, StringComparison.Ordinal);
                _valueStack.Push(DynValue.NewBoolean(comparison < 0));
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__lt", instructionPtr);
                if (ip < 0)
                {
                    throw ScriptRuntimeException.CompareInvalidType(l, r);
                }
                else
                {
                    return ip;
                }
            }

            return instructionPtr;
        }

        private int ExecLessEq(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            if (l.Type == DataType.Number && r.Type == DataType.Number)
            {
                _valueStack.Push(DynValue.False);
                _valueStack.Push(DynValue.NewBoolean(l.Number <= r.Number));
            }
            else if (l.Type == DataType.String && r.Type == DataType.String)
            {
                _valueStack.Push(DynValue.False);
                int comparison = string.Compare(l.String, r.String, StringComparison.Ordinal);
                _valueStack.Push(DynValue.NewBoolean(comparison <= 0));
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(
                    l,
                    r,
                    "__le",
                    instructionPtr,
                    DynValue.False
                );
                if (ip < 0)
                {
                    ip = InternalInvokeBinaryMetaMethod(
                        r,
                        l,
                        "__lt",
                        instructionPtr,
                        DynValue.True
                    );

                    if (ip < 0)
                    {
                        throw ScriptRuntimeException.CompareInvalidType(l, r);
                    }
                    else
                    {
                        return ip;
                    }
                }
                else
                {
                    return ip;
                }
            }

            return instructionPtr;
        }

        private int ExecLen(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();

            if (r.Type == DataType.String)
            {
                _valueStack.Push(DynValue.NewNumber(r.String.Length));
            }
            else
            {
                int ip = InternalInvokeUnaryMetaMethod(r, "__len", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else if (r.Type == DataType.Table)
                {
                    _valueStack.Push(DynValue.NewNumber(r.Table.Length));
                }
                else
                {
                    throw ScriptRuntimeException.LenOnInvalidType(r);
                }
            }

            return instructionPtr;
        }

        private int ExecConcat(Instruction i, int instructionPtr)
        {
            DynValue r = _valueStack.Pop().ToScalar();
            DynValue l = _valueStack.Pop().ToScalar();

            string rs = r.CastToString();
            string ls = l.CastToString();

            if (rs != null && ls != null)
            {
                _valueStack.Push(DynValue.NewString(ls + rs));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, "__concat", instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else
                {
                    throw ScriptRuntimeException.ConcatOnNonString(l, r);
                }
            }
        }

        private void ExecTblInitI(Instruction i)
        {
            // stack: tbl - val
            DynValue val = _valueStack.Pop();
            DynValue tbl = _valueStack.Peek();

            if (tbl.Type != DataType.Table)
            {
                throw new InternalErrorException("Unexpected type in table ctor : {0}", tbl);
            }

            tbl.Table.InitNextArrayKeys(val, i.NumVal != 0);
        }

        private void ExecTblInitN(Instruction i)
        {
            // stack: tbl - key - val
            DynValue val = _valueStack.Pop();
            DynValue key = _valueStack.Pop();
            DynValue tbl = _valueStack.Peek();

            if (tbl.Type != DataType.Table)
            {
                throw new InternalErrorException("Unexpected type in table ctor : {0}", tbl);
            }

            tbl.Table.Set(key, val.ToScalar());
        }

        private int ExecIndexSet(Instruction i, int instructionPtr)
        {
            int nestedMetaOps = 100; // sanity check, to avoid potential infinite loop here

            // stack: vals.. - base - index
            bool isNameIndex = i.OpCode == OpCode.IndexSetN;
            bool isMultiIndex = (i.OpCode == OpCode.IndexSetL);

            DynValue originalIdx = i.Value ?? _valueStack.Pop();
            DynValue idx = originalIdx.ToScalar();
            DynValue obj = _valueStack.Pop().ToScalar();
            DynValue value = GetStoreValue(i);
            DynValue h = null;

            while (nestedMetaOps > 0)
            {
                --nestedMetaOps;

                if (obj.Type == DataType.Table)
                {
                    if (!isMultiIndex)
                    {
                        if (!obj.Table.Get(idx).IsNil())
                        {
                            obj.Table.Set(idx, value);
                            return instructionPtr;
                        }
                    }

                    h = GetMetamethodRaw(obj, "__newindex");

                    if (h == null || h.IsNil())
                    {
                        if (isMultiIndex)
                        {
                            throw new ScriptRuntimeException(
                                "cannot multi-index a table. userdata expected"
                            );
                        }

                        obj.Table.Set(idx, value);
                        return instructionPtr;
                    }
                }
                else if (obj.Type == DataType.UserData)
                {
                    UserData ud = obj.UserData;

                    if (
                        !ud.Descriptor.SetIndex(
                            GetScript(),
                            ud.Object,
                            originalIdx,
                            value,
                            isNameIndex
                        )
                    )
                    {
                        throw ScriptRuntimeException.UserDataMissingField(
                            ud.Descriptor.Name,
                            idx.String
                        );
                    }

                    return instructionPtr;
                }
                else
                {
                    h = GetMetamethodRaw(obj, "__newindex");

                    if (h == null || h.IsNil())
                    {
                        throw ScriptRuntimeException.IndexType(obj);
                    }
                }

                if (h.Type == DataType.Function || h.Type == DataType.ClrFunction)
                {
                    if (isMultiIndex)
                    {
                        throw new ScriptRuntimeException(
                            "cannot multi-index through metamethods. userdata expected"
                        );
                    }

                    _valueStack.Pop(); // burn extra value ?

                    _valueStack.Push(h);
                    _valueStack.Push(obj);
                    _valueStack.Push(idx);
                    _valueStack.Push(value);
                    return InternalExecCall(3, instructionPtr);
                }
                else
                {
                    obj = h;
                    h = null;
                }
            }
            throw ScriptRuntimeException.LoopInNewIndex();
        }

        private int ExecIndex(Instruction i, int instructionPtr)
        {
            int nestedMetaOps = 100; // sanity check, to avoid potential infinite loop here

            // stack: base - index
            bool isNameIndex = i.OpCode == OpCode.IndexN;

            bool isMultiIndex = (i.OpCode == OpCode.IndexL);

            DynValue originalIdx = i.Value ?? _valueStack.Pop();
            DynValue idx = originalIdx.ToScalar();
            DynValue obj = _valueStack.Pop().ToScalar();

            DynValue h = null;

            while (nestedMetaOps > 0)
            {
                --nestedMetaOps;

                if (obj.Type == DataType.Table)
                {
                    if (!isMultiIndex)
                    {
                        DynValue v = obj.Table.Get(idx);

                        if (!v.IsNil())
                        {
                            _valueStack.Push(v.AsReadOnly());
                            return instructionPtr;
                        }
                    }

                    h = GetMetamethodRaw(obj, "__index");

                    if (h == null || h.IsNil())
                    {
                        if (isMultiIndex)
                        {
                            throw new ScriptRuntimeException(
                                "cannot multi-index a table. userdata expected"
                            );
                        }

                        _valueStack.Push(DynValue.Nil);
                        return instructionPtr;
                    }
                }
                else if (obj.Type == DataType.UserData)
                {
                    UserData ud = obj.UserData;

                    DynValue v = ud.Descriptor.Index(
                        GetScript(),
                        ud.Object,
                        originalIdx,
                        isNameIndex
                    );

                    if (v == null)
                    {
                        throw ScriptRuntimeException.UserDataMissingField(
                            ud.Descriptor.Name,
                            idx.String
                        );
                    }

                    _valueStack.Push(v.AsReadOnly());
                    return instructionPtr;
                }
                else
                {
                    h = GetMetamethodRaw(obj, "__index");

                    if (h == null || h.IsNil())
                    {
                        throw ScriptRuntimeException.IndexType(obj);
                    }
                }

                if (h.Type == DataType.Function || h.Type == DataType.ClrFunction)
                {
                    if (isMultiIndex)
                    {
                        throw new ScriptRuntimeException(
                            "cannot multi-index through metamethods. userdata expected"
                        );
                    }

                    _valueStack.Push(h);
                    _valueStack.Push(obj);
                    _valueStack.Push(idx);
                    return InternalExecCall(2, instructionPtr);
                }
                else
                {
                    obj = h;
                    h = null;
                }
            }

            throw ScriptRuntimeException.LoopInIndex();
        }
    }
}
