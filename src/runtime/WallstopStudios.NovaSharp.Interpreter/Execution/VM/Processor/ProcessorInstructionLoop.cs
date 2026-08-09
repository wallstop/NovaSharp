namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using Cysharp.Text;
    using Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.PredefinedUserData;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <content>
    /// Hosts the VM instruction loop and per-opcode execution helpers.
    /// </content>
    internal sealed partial class Processor
    {
        private const int YieldSpecialTrap = -99;

        private readonly struct TailCallFrameReuseState
        {
            public TailCallFrameReuseState(
                int returnAddress,
                CallbackFunction errorHandler,
                CallbackFunction continuation,
                LuaValue errorHandlerBeforeUnwind,
                bool hasErrorHandlerBeforeUnwind
            )
            {
                ReturnAddress = returnAddress;
                ErrorHandler = errorHandler;
                Continuation = continuation;
                ErrorHandlerBeforeUnwind = errorHandlerBeforeUnwind;
                HasErrorHandlerBeforeUnwind = hasErrorHandlerBeforeUnwind;
            }

            public int ReturnAddress { get; }

            public CallbackFunction ErrorHandler { get; }

            public CallbackFunction Continuation { get; }

            public LuaValue ErrorHandlerBeforeUnwind { get; }

            public bool HasErrorHandlerBeforeUnwind { get; }
        }

        /// <summary>
        /// Gets or sets how many instructions the VM may execute before automatically yielding.
        /// </summary>
        internal long AutoYieldCounter { get; set; }

        private LuaValue ProcessingLoop(int instructionPtr)
        {
            // This is the main loop of the processor, has a weird control flow and needs to be as fast as possible.
            // This sentence is just a convoluted way to say "don't complain about gotos".

            long executedInstructions = 0;
            bool canAutoYield =
                (AutoYieldCounter > 0) && _canYield && (State != CoroutineState.Main);
            bool shouldYieldToCaller = false;
            SandboxOptions sandbox = _script.Options.Sandbox;
            bool hasSandboxInstructionLimit = sandbox.HasInstructionLimit;
            long sandboxMaxInstructions = sandbox.MaxInstructions;
            bool hasSandboxMemoryLimit = sandbox.HasMemoryLimit;
            long sandboxMaxMemoryBytes = sandbox.MaxMemoryBytes;
            AllocationTracker allocationTracker = _script.AllocationTracker;
            const int MemoryCheckInterval = 1024;

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
                        return LuaValue.NewForcedYieldReq();
                    }

                    // Check sandbox instruction limit
                    if (hasSandboxInstructionLimit && executedInstructions > sandboxMaxInstructions)
                    {
                        Func<Script, long, bool> callback = sandbox.OnInstructionLimitExceeded;
                        if (callback == null || !callback(_script, executedInstructions))
                        {
                            throw new SandboxViolationException(
                                SandboxViolationType.InstructionLimitExceeded,
                                sandboxMaxInstructions,
                                executedInstructions
                            );
                        }
                        // Callback allowed continuation - reset counter
                        executedInstructions = 0;
                    }

                    // Check sandbox memory limit (less frequently to reduce overhead)
                    if (
                        hasSandboxMemoryLimit
                        && (executedInstructions & (MemoryCheckInterval - 1)) == 0
                    )
                    {
                        long currentMemory = allocationTracker.CurrentBytes;
                        if (currentMemory > sandboxMaxMemoryBytes)
                        {
                            Func<Script, long, bool> callback = sandbox.OnMemoryLimitExceeded;
                            if (callback == null || !callback(_script, currentMemory))
                            {
                                throw new SandboxViolationException(
                                    SandboxViolationType.MemoryLimitExceeded,
                                    sandboxMaxMemoryBytes,
                                    currentMemory
                                );
                            }
                        }
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
                                i.Name,
                                allowTailCallFrameReuse: true
                            );
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            if (instructionPtr < 0 && instructionPtr != YieldSpecialTrap)
                            {
                                goto return_to_native_code;
                            }

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
                                LuaValue v = _valueStack.Pop().ToScalar();

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
                            ExecEnter(i, instructionPtr);
                            break;
                        case OpCode.Leave:
                            ExecLeave(i, instructionPtr);
                            break;
                        case OpCode.Exit:
                            ExecExit(i, instructionPtr);
                            break;
                        case OpCode.Clean:
                            ClearBlockData(i, instructionPtr);
                            break;
                        case OpCode.Closure:
                            ExecClosure(i);
                            break;
                        case OpCode.BeginFn:
                            ExecBeginFn(i, instructionPtr);
                            break;
                        case OpCode.ToBool:
                            _valueStack.Push(
                                LuaValue.FromBoolean(_valueStack.Pop().ToScalar().CastToBool())
                            );
                            break;
                        case OpCode.Args:
                            ExecArgs(i);
                            break;
                        case OpCode.Ret:
                            instructionPtr = ExecRet(i, instructionPtr);
                            shouldYieldToCaller = instructionPtr == YieldSpecialTrap;

                            if (instructionPtr < 0 && instructionPtr != YieldSpecialTrap)
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
                                _valueStack.Push(LuaValue.NewTable(_script));
                            }
                            else
                            {
                                _valueStack.Push(LuaValue.NewPrimeTable());
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
                            ValueSlot[] scope = _executionStack.Peek().LocalScope;
                            int index = i.Symbol.IndexValue;
                            _valueStack.Push(scope[index].Value);
                            break;
                        case OpCode.UpValue:
                            _valueStack.Push(
                                _executionStack.Peek().ClosureScope[i.Symbol.IndexValue]
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
                            throw new NotImplementedException(
                                ZString.Concat("Invalid opcode : ", i.Name)
                            );
                        default:
                            throw new NotImplementedException(
                                ZString.Concat("Execution for ", i.OpCode, " not implemented yet!")
                            );
                    }

                    if (shouldYieldToCaller)
                    {
                        goto yield_to_calling_coroutine;
                    }
                }

                yield_to_calling_coroutine:

                LuaValue yieldRequest = _valueStack.Pop().ToScalar();

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

                DecorateNearestErrorHandlerBeforeUnwind(exception, instructionPtr);

                ScriptRuntimeException activeException = exception;
                while (_executionStack.Count > 0)
                {
                    CallStackItem csi = PopToBasePointer();
                    bool returnedToPool = false;

                    try
                    {
                        LuaValue closeError = LuaValue.NewString(activeException.DecoratedMessage);
                        try
                        {
                            CloseAllPendingBlocks(
                                csi,
                                closeError,
                                instructionPtr,
                                decorateCloseErrorsBeforeUnwind: true
                            );
                        }
                        catch (ScriptRuntimeException closeException)
                        {
                            activeException = closeException;
                        }

                        if (csi.ErrorHandler != null)
                        {
                            instructionPtr = csi.ReturnAddress;

                            if (csi.ClrFunction == null)
                            {
                                int argscnt = (int)(_valueStack.Pop().Number);
                                _valueStack.RemoveLast(argscnt + 1);
                            }

                            CallbackFunction errorHandler = csi.ErrorHandler;
                            SourceRef sourceRef = GetCurrentSourceRef(instructionPtr);
                            CallStackItemPool.Return(csi);
                            returnedToPool = true;

                            // Use pooled array for error handler invocation
                            using (
                                PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(
                                    1,
                                    out LuaValue[] cbargs
                                )
                            )
                            {
                                cbargs[0] = LuaValue.NewString(activeException.DecoratedMessage);

                                LuaValue handled = errorHandler.Invoke(
                                    new ScriptExecutionContext(this, errorHandler, sourceRef),
                                    cbargs
                                );

                                _valueStack.Push(handled);
                            }

                            goto repeat_execution;
                        }

                        if ((csi.Flags & CallStackItemFlags.EntryPoint) != 0)
                        {
                            // Mirror the normal-return cleanup: a Lua EntryPoint frame keeps its CLR entry
                            // layout (function slot + argument count) below the base pointer, which
                            // PopToBasePointer does not crop. Remove it before leaving the VM or a CLR
                            // caller that catches this error (e.g. a pcall CLR target) is left with orphaned
                            // value-stack slots.
                            if (csi.ClrFunction == null)
                            {
                                int argscnt = (int)(_valueStack.Pop().Number);
                                _valueStack.RemoveLast(argscnt + 1);
                            }

                            activeException.Rethrow();
                            throw;
                        }
                    }
                    finally
                    {
                        if (!returnedToPool)
                        {
                            CallStackItemPool.Return(csi);
                        }
                    }
                }

                activeException.Rethrow();
                throw;
            }

            return_to_native_code:
            return _valueStack.Pop();
        }

        private void DecorateNearestErrorHandlerBeforeUnwind(
            ScriptRuntimeException exception,
            int instructionPtr
        )
        {
            DecorateNearestErrorHandlerBeforeUnwind(exception, instructionPtr, currentFrame: null);
        }

        private void DecorateNearestErrorHandlerBeforeUnwind(
            ScriptRuntimeException exception,
            int instructionPtr,
            CallStackItem currentFrame
        )
        {
            if (exception.ErrorHandlerBeforeUnwindDecorated)
            {
                return;
            }

            if (TryDecorateErrorHandlerBeforeUnwind(currentFrame, exception, instructionPtr))
            {
                return;
            }

            int framesToScan = _executionStack.Count;
            if (_errorHandlerBeforeUnwindScanBoundaryDepth >= 0)
            {
                framesToScan = Math.Max(
                    0,
                    _executionStack.Count - _errorHandlerBeforeUnwindScanBoundaryDepth
                );
            }

            for (int i = 0; i < framesToScan; i++)
            {
                CallStackItem frame = _executionStack.Peek(i);
                if (TryDecorateErrorHandlerBeforeUnwind(frame, exception, instructionPtr))
                {
                    return;
                }
            }
        }

        private bool TryDecorateErrorHandlerBeforeUnwind(
            CallStackItem frame,
            ScriptRuntimeException exception,
            int instructionPtr
        )
        {
            if (frame == null || frame.ErrorHandler == null)
            {
                return false;
            }

            if (frame.HasErrorHandlerBeforeUnwind)
            {
                LuaValue messageHandler = frame.ErrorHandlerBeforeUnwind;
                if (frame.ErrorHandlerBeforeUnwindInProgress)
                {
                    return true;
                }

                frame.ErrorHandlerBeforeUnwindInProgress = true;
                try
                {
                    exception.DecoratedMessage = PerformMessageDecorationBeforeUnwind(
                        messageHandler,
                        exception.DecoratedMessage,
                        GetCurrentSourceRef(instructionPtr)
                    );
                    exception.ErrorHandlerBeforeUnwindDecorated = true;
                }
                finally
                {
                    frame.ErrorHandlerBeforeUnwindInProgress = false;
                }
            }

            return true;
        }

        /// <summary>
        /// Invokes the provided message handler to decorate an error before the stack unwinds.
        /// </summary>
        /// <param name="messageHandler">Function handling the error.</param>
        /// <param name="decoratedMessage">Existing decorated message (if any).</param>
        /// <param name="sourceRef">Source reference associated with the error.</param>
        /// <returns>The new decorated message.</returns>
        internal string PerformMessageDecorationBeforeUnwind(
            LuaValue messageHandler,
            string decoratedMessage,
            SourceRef sourceRef
        )
        {
            try
            {
                // Use pooled array for message handler invocation
                using (
                    PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(
                        1,
                        out LuaValue[] args
                    )
                )
                {
                    args[0] = LuaValue.NewString(decoratedMessage);
                    LuaValue ret = LuaValue.Nil;

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
            }
            catch (ScriptRuntimeException)
            {
                // When the error handler fails for any reason (including when it's not callable
                // in Lua 5.1/5.2, or when it throws an error in any version), the result is
                // always "error in error handling" per the Lua specification.
                return "error in error handling";
            }

            return decoratedMessage;
        }

        private void AssignLocal(SymbolRef symref, LuaValue value)
        {
            CallStackItem stackframe = _executionStack.Peek();

            ValueSlot slot = stackframe.LocalScope[symref.IndexValue];
            if (slot == null)
            {
                stackframe.LocalScope[symref.IndexValue] = slot = new ValueSlot();
            }

            bool isToBeClosed = IsSymbolToBeClosed(stackframe, symref.IndexValue);

            if (isToBeClosed)
            {
                EnsureToBeClosedValue(symref, value);

                LuaValue previous = slot.Value;
                if (!previous.IsNil)
                {
                    CloseValue(symref, previous, LuaValue.Nil, instructionPtr: -1);
                }
            }

            slot.Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSymbolToBeClosed(CallStackItem stackframe, int index)
        {
            return stackframe.ToBeClosedIndices != null
                && stackframe.ToBeClosedIndices.Contains(index);
        }

        private void ExecEnter(Instruction i, int instructionPtr)
        {
            ClearBlockData(i, instructionPtr);

            CallStackItem stackframe = _executionStack.Peek();

            SymbolRef[] closers = i.SymbolList ?? Array.Empty<SymbolRef>();

            if (stackframe.BlocksToClose == null)
            {
                stackframe.BlocksToClose = ListPool<List<SymbolRef>>.Rent();
            }

            List<SymbolRef> closersList = ListPool<SymbolRef>.Rent(closers.Length);
            for (int idx = 0; idx < closers.Length; idx++)
            {
                closersList.Add(closers[idx]);
            }
            stackframe.BlocksToClose.Add(closersList);

            if (closers.Length > 0)
            {
                if (stackframe.ToBeClosedIndices == null)
                {
                    stackframe.ToBeClosedIndices = HashSetPool<int>.Rent();
                }

                foreach (SymbolRef sym in closers)
                {
                    stackframe.ToBeClosedIndices.Add(sym.IndexValue);
                }
            }
        }

        private void ExecLeave(Instruction i, int instructionPtr)
        {
            CallStackItem stackframe = _executionStack.Peek();
            CloseCurrentBlock(stackframe, LuaValue.Nil, instructionPtr);
            ClearBlockData(i, instructionPtr);
        }

        private void ExecExit(Instruction i, int instructionPtr)
        {
            CallStackItem stackframe = _executionStack.Peek();
            CloseCurrentBlock(stackframe, LuaValue.Nil, instructionPtr);
            ClearBlockData(i, instructionPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldIgnoreToBeClosedValue(LuaValue value)
        {
            return value.IsNil || (value.Type == DataType.Boolean && value.Boolean == false);
        }

        private void EnsureToBeClosedValue(SymbolRef symbol, LuaValue value)
        {
            LuaValue candidate = value.ToScalar();

            if (ShouldIgnoreToBeClosedValue(candidate))
            {
                return;
            }

            LuaValue metamethod = GetMetamethodRaw(candidate, Metamethods.Close);

            if (metamethod.IsNil)
            {
                throw ScriptRuntimeException.CloseMetamethodExpected(candidate);
            }
        }

        private void CloseValue(
            SymbolRef symbol,
            LuaValue value,
            LuaValue error,
            int instructionPtr
        )
        {
            LuaValue scalar = value.ToScalar();

            if (ShouldIgnoreToBeClosedValue(scalar))
            {
                return;
            }

            LuaValue metamethod = GetMetamethodRaw(scalar, Metamethods.Close);

            if (metamethod.IsNil)
            {
                throw ScriptRuntimeException.CloseMetamethodExpected(scalar);
            }

            LuaValue err = error;

            if (metamethod.Type == DataType.Function)
            {
                Call(metamethod, scalar, err);
                return;
            }

            if (metamethod.Type == DataType.ClrFunction)
            {
                CallbackFunction callback = metamethod.Callback;
                ScriptExecutionContext context = new(
                    this,
                    callback,
                    GetCurrentSourceRef(instructionPtr)
                );
                if (callback.HasArgumentViewCallback)
                {
                    callback.InvokeArgumentViewFixed(context, scalar, err);
                }
                else
                {
                    callback.InvokeLegacyFixed(context, scalar, err);
                }
                return;
            }

            throw ScriptRuntimeException.CloseMetamethodExpected(scalar);
        }

        private void CloseCurrentBlock(CallStackItem stackframe, LuaValue error, int instructionPtr)
        {
            if (stackframe.BlocksToClose == null || stackframe.BlocksToClose.Count == 0)
            {
                return;
            }

            List<SymbolRef> closers = stackframe.BlocksToClose[^1];
            stackframe.BlocksToClose.RemoveAt(stackframe.BlocksToClose.Count - 1);

            if (closers.Count == 0)
            {
                ListPool<SymbolRef>.Return(closers);
                return;
            }

            if (stackframe.ToBeClosedIndices != null)
            {
                foreach (SymbolRef sym in closers)
                {
                    stackframe.ToBeClosedIndices.Remove(sym.IndexValue);
                }
            }

            LuaValue activeError = error;
            ScriptRuntimeException closeException = null;
            for (int idx = closers.Count - 1; idx >= 0; idx--)
            {
                SymbolRef sym = closers[idx];
                ValueSlot slot = stackframe.LocalScope[sym.IndexValue];

                closeException = CloseValueAndTrackError(
                    sym,
                    slot,
                    ref activeError,
                    closeException,
                    stackframe,
                    instructionPtr,
                    decorateCloseErrorsBeforeUnwind: true
                );
            }

            ListPool<SymbolRef>.Return(closers);

            if (closeException != null)
            {
                throw closeException;
            }
        }

        private void CloseAllPendingBlocks(CallStackItem stackframe, LuaValue error)
        {
            CloseAllPendingBlocks(
                stackframe,
                error,
                instructionPtr: -1,
                decorateCloseErrorsBeforeUnwind: false
            );
        }

        private void CloseAllPendingBlocks(
            CallStackItem stackframe,
            LuaValue error,
            int instructionPtr,
            bool decorateCloseErrorsBeforeUnwind
        )
        {
            if (stackframe.BlocksToClose == null || stackframe.BlocksToClose.Count == 0)
            {
                return;
            }

            LuaValue activeError = error;
            ScriptRuntimeException closeException = null;
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
                    ListPool<SymbolRef>.Return(closers);
                    continue;
                }

                for (int idx = closers.Count - 1; idx >= 0; idx--)
                {
                    SymbolRef sym = closers[idx];
                    ValueSlot slot = stackframe.LocalScope[sym.IndexValue];

                    closeException = CloseValueAndTrackError(
                        sym,
                        slot,
                        ref activeError,
                        closeException,
                        stackframe,
                        instructionPtr,
                        decorateCloseErrorsBeforeUnwind
                    );
                }

                ListPool<SymbolRef>.Return(closers);
            }

            stackframe.ToBeClosedIndices?.Clear();

            if (closeException != null)
            {
                throw closeException;
            }
        }

        private ScriptRuntimeException CloseValueAndTrackError(
            SymbolRef symbol,
            ValueSlot slot,
            ref LuaValue activeError,
            ScriptRuntimeException activeException,
            CallStackItem stackframe,
            int instructionPtr,
            bool decorateCloseErrorsBeforeUnwind
        )
        {
            if (slot == null)
            {
                return activeException;
            }

            LuaValue previous = slot.Value;
            if (previous.IsNil)
            {
                return activeException;
            }

            int previousBoundaryDepth = _errorHandlerBeforeUnwindScanBoundaryDepth;
            _errorHandlerBeforeUnwindScanBoundaryDepth = _executionStack.Count;
            try
            {
                CloseValue(symbol, previous, activeError, instructionPtr);
            }
            catch (ScriptRuntimeException closeException)
            {
                if (closeException.CallStack == null)
                {
                    FillDebugData(closeException, instructionPtr);
                }

                if (decorateCloseErrorsBeforeUnwind)
                {
                    DecorateNearestErrorHandlerBeforeUnwind(
                        closeException,
                        instructionPtr,
                        stackframe
                    );
                }

                activeError = LuaValue.NewString(closeException.DecoratedMessage);
                activeException = closeException;
            }
            finally
            {
                _errorHandlerBeforeUnwindScanBoundaryDepth = previousBoundaryDepth;
                slot.Value = LuaValue.Nil;
            }

            return activeException;
        }

        private void ExecStoreLcl(Instruction i)
        {
            LuaValue value = GetStoreValue(i);
            SymbolRef symref = i.Symbol;

            AssignLocal(symref, value);
        }

        private void ExecStoreUpv(Instruction i)
        {
            LuaValue value = GetStoreValue(i);
            SymbolRef symref = i.Symbol;

            CallStackItem stackframe = _executionStack.Peek();

            stackframe.ClosureScope.GetSlot(symref.IndexValue).Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecSwap(Instruction i)
        {
            LuaValue v1 = _valueStack.Peek(i.NumVal);
            LuaValue v2 = _valueStack.Peek(i.NumVal2);

            _valueStack.Set(i.NumVal, v2);
            _valueStack.Set(i.NumVal2, v1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaValue GetStoreValue(Instruction i)
        {
            int stackofs = i.NumVal;
            int tupleidx = i.NumVal2;

            LuaValue v = _valueStack.Peek(stackofs);

            if (v.Type == DataType.Tuple)
            {
                return (tupleidx < v.Tuple.Length) ? v.Tuple[tupleidx] : LuaValue.Nil;
            }
            else
            {
                return (tupleidx == 0) ? v : LuaValue.Nil;
            }
        }

        private void ExecClosure(Instruction i)
        {
            using (
                ListPool<ValueSlot>.Get(i.SymbolList.Length, out List<ValueSlot> resolvedSymbols)
            )
            {
                foreach (SymbolRef symbol in i.SymbolList)
                {
                    resolvedSymbols.Add(GetUpValueSymbol(symbol));
                }

                Closure c = new(_script, i.NumVal, i.SymbolList, resolvedSymbols);

                _valueStack.Push(LuaValue.NewClosure(c));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueSlot GetUpValueSymbol(SymbolRef s)
        {
            if (s.Type == SymbolRefType.Local)
            {
                CallStackItem frame = _executionStack.Peek();
                ValueSlot slot = frame.LocalScope[s.IndexValue];
                if (slot == null)
                {
                    // Capturing a local that has not been assigned yet still has to share the cell
                    // the eventual assignment will write to, so materialize it now.
                    frame.LocalScope[s.IndexValue] = slot = new ValueSlot();
                }

                return slot;
            }
            else if (s.Type == SymbolRefType.UpValue)
            {
                return _executionStack.Peek().ClosureScope.GetSlot(s.IndexValue);
            }
            throw new InvalidOperationException("Unsupported symbol type in closure capture.");
        }

        private void ExecMkTuple(Instruction i)
        {
            Slice<LuaValue> slice = new(_valueStack, _valueStack.Count - i.NumVal, i.NumVal, false);

            LuaValue[] v = InternalAdjustTuple(slice);

            _valueStack.RemoveLast(i.NumVal);

            _valueStack.Push(LuaValue.NewTuple(v));
        }

        private void ExecToNum(Instruction i)
        {
            // Use CastToLuaNumber to preserve integer/float subtype for precise arithmetic.
            // Using CastToNumber (double) loses integer precision for large values near maxinteger,
            // causing for-loops to infinite loop due to floating-point precision limits.
            LuaNumber? v = _valueStack
                .Pop()
                .ToScalar()
                .CastToLuaNumber(_script.Options.CompatibilityVersion);
            if (v.HasValue)
            {
                _valueStack.Push(LuaValue.NewNumber(v.Value));
            }
            else
            {
                throw ScriptRuntimeException.ConvertToNumberFailed(i.NumVal);
            }
        }

        private void ExecIterUpd(Instruction i)
        {
            LuaValue v = _valueStack.Peek(0);
            LuaValue t = _valueStack.Peek(1);
            t.Tuple[2] = v;
        }

        private void ExecExpTuple(Instruction i)
        {
            LuaValue t = _valueStack.Peek(i.NumVal);

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
            LuaValue v = _valueStack.Pop();

            if (v.Type != DataType.Tuple)
            {
                v = LuaValue.NewTuple(v, LuaValue.Nil, LuaValue.Nil);
            }

            LuaValue f = v.Tuple.Length >= 1 ? v.Tuple[0] : LuaValue.Nil;
            LuaValue s = v.Tuple.Length >= 2 ? v.Tuple[1] : LuaValue.Nil;
            LuaValue var = v.Tuple.Length >= 3 ? v.Tuple[2] : LuaValue.Nil;

            // NovaSharp additions - given f, s, var
            // 1) if f is not a function and has a __iterator metamethod, call __iterator to get the triplet
            // 2) if f is a table with no __call metamethod, use a default table iterator

            if (f.Type != DataType.Function && f.Type != DataType.ClrFunction)
            {
                LuaValue meta = GetMetamethod(f, Metamethods.Iterator);

                if (!meta.IsNil)
                {
                    if (meta.Type != DataType.Tuple)
                    {
                        v = GetScript().CallValues(meta, f, s, var);
                    }
                    else
                    {
                        v = meta;
                    }

                    f = v.Tuple.Length >= 1 ? v.Tuple[0] : LuaValue.Nil;
                    s = v.Tuple.Length >= 2 ? v.Tuple[1] : LuaValue.Nil;
                    var = v.Tuple.Length >= 3 ? v.Tuple[2] : LuaValue.Nil;

                    _valueStack.Push(LuaValue.NewTuple(f, s, var));
                    return;
                }
                else if (f.Type == DataType.Table)
                {
                    LuaValue callmeta = GetMetamethod(f, Metamethods.Call);

                    if (callmeta.IsNil)
                    {
                        _valueStack.Push(EnumerableWrapper.ConvertTable(f.Table));
                        return;
                    }
                }
            }

            _valueStack.Push(LuaValue.NewTuple(f, s, var));
        }

        private int ExecJFor(Instruction i, int instructionPtr)
        {
            // Use LuaNumber to preserve integer precision for large values (e.g., near maxinteger).
            // Using .Number (double) causes precision loss and infinite loops for large integer bounds.
            LuaNumber val = _valueStack.Peek(0).LuaNumber;
            LuaNumber step = _valueStack.Peek(1).LuaNumber;
            LuaNumber stop = _valueStack.Peek(2).LuaNumber;

            // Lua for-loop condition: if step > 0 then val <= stop else val >= stop
            // Use LuaNumber comparison to avoid precision loss with large step values
            bool stepPositive = LuaNumber.LessThan(LuaNumber.Zero, step);

            // For integer for-loops in Lua 5.3+, detect overflow to prevent infinite loops.
            // Per Lua 5.4 §3.3.5: "the control variable never wraps around".
            // If step is positive and val < stop but we're past the first iteration (val has been incremented),
            // we need to detect if the previous increment caused overflow.
            // Overflow detection: if step > 0 and val < start (represented as stop - n*step equivalent),
            // or more simply: if step > 0 and val wrapped to negative when it should be positive.
            if (val.IsInteger && step.IsInteger && stop.IsInteger)
            {
                long stepVal = step.AsInteger;
                long valVal = val.AsInteger;
                long stopVal = stop.AsInteger;

                // Detect overflow: for positive step, if val < (stop - large_positive) we likely wrapped
                // More precisely: if step > 0, increment causes overflow when val > maxint - step
                // After overflow, val will be negative (wrapped from max to min).
                // If step > 0 and val < 0 and stop > 0, this indicates overflow occurred.
                // Similarly for step < 0.
                if (stepVal > 0)
                {
                    // Positive step: loop should go from low to high
                    // If val suddenly became much smaller than stop (wrapped), stop the loop
                    // Detection: if previous val was > current val after increment, overflow occurred
                    // Since we don't track previous val, use: if step > 0 but val > stop, it's definitely done.
                    // But also: if overflow occurred, val wrapped around.
                    // Conservative detection: if the distance from val to stop is larger than it should be.
                    // Simplest: use checked arithmetic or detect sign change when it shouldn't happen.
                    // If stopVal >= 0 and valVal < 0 and step > 0, we wrapped from positive to negative.
                    if (stopVal >= 0 && valVal < 0)
                    {
                        // Overflow occurred - loop should terminate
                        return i.NumVal;
                    }
                }
                else if (stepVal < 0)
                {
                    // Negative step: loop should go from high to low
                    // If stopVal <= 0 and valVal > 0, we wrapped from negative to positive
                    if (stopVal <= 0 && valVal > 0)
                    {
                        // Overflow occurred - loop should terminate
                        return i.NumVal;
                    }
                }
            }

            bool whileCond = stepPositive
                ? LuaNumber.LessThanOrEqual(val, stop)
                : LuaNumber.LessThanOrEqual(stop, val);

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
            LuaValue top = _valueStack.Peek(0);
            LuaValue btm = _valueStack.Peek(i.NumVal);

            if (top.Type != DataType.Number)
            {
                throw new InternalErrorException("Can't assign number to type {0}", top.Type);
            }

            // Use LuaNumber.Add to preserve integer precision for large values.
            // Raw double addition causes precision loss near maxinteger, leading to infinite loops.
            // The counter is replaced rather than mutated: values are immutable, so the previous
            // counter may already have been stored into the loop variable's slot or a table.
            _valueStack.Set(0, LuaValue.NewNumber(LuaNumber.Add(top.LuaNumber, btm.LuaNumber)));
        }

        private void ExecCNot(Instruction i)
        {
            LuaValue v = _valueStack.Pop().ToScalar();
            LuaValue not = _valueStack.Pop().ToScalar();

            if (not.Type != DataType.Boolean)
            {
                throw new InternalErrorException("CNOT had non-bool arg");
            }

            if (not.CastToBool())
            {
                _valueStack.Push(LuaValue.FromBoolean(!(v.CastToBool())));
            }
            else
            {
                _valueStack.Push(LuaValue.FromBoolean(v.CastToBool()));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecNot(Instruction i)
        {
            LuaValue v = _valueStack.Pop().ToScalar();
            _valueStack.Push(LuaValue.FromBoolean(!(v.CastToBool())));
        }

        private void ExecBeginFn(Instruction i, int instructionPtr)
        {
            CallStackItem cur = _executionStack.Peek();

            cur.DebugSymbols = i.SymbolList;
            cur.LocalScope = SystemArrayPool<ValueSlot>.Rent(i.NumVal);

            ClearBlockData(i, instructionPtr);

            if (cur.BlocksToClose == null)
            {
                cur.BlocksToClose = ListPool<List<SymbolRef>>.Rent();
            }
            else
            {
                // Return all inner lists before clearing the outer list
                foreach (List<SymbolRef> innerList in cur.BlocksToClose)
                {
                    ListPool<SymbolRef>.Return(innerList);
                }
                cur.BlocksToClose.Clear();
            }

            if (cur.ToBeClosedIndices == null)
            {
                cur.ToBeClosedIndices = HashSetPool<int>.Rent();
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
                    rootClosers ??= ListPool<SymbolRef>.Rent();
                    rootClosers.Add(symbol);
                    cur.ToBeClosedIndices.Add(symbol.IndexValue);
                }
            }

            if (rootClosers != null && rootClosers.Count > 0)
            {
                cur.BlocksToClose.Add(rootClosers);
            }
            else
            {
                // Return unused pooled list if we allocated one but it's empty
                ListPool<SymbolRef>.Return(rootClosers);
                if (cur.BlocksToClose.Count == 0)
                {
                    ListPool<List<SymbolRef>>.Return(cur.BlocksToClose);
                    cur.BlocksToClose = null;
                }
            }

            if (cur.ToBeClosedIndices.Count == 0)
            {
                HashSetPool<int>.Return(cur.ToBeClosedIndices);
                cur.ToBeClosedIndices = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private CallStackItem PopToBasePointer()
        {
            CallStackItem csi = _executionStack.Pop();
            if (csi.BasePointer >= 0)
            {
                _valueStack.CropAtCount(csi.BasePointer);
            }

            return csi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PopExecStackAndCheckVStack(int vstackguard)
        {
            CallStackItem xs = _executionStack.Pop();
            if (vstackguard != xs.BasePointer)
            {
                CallStackItemPool.Return(xs);
                throw new InternalErrorException("StackGuard violation");
            }

            int returnAddress = xs.ReturnAddress;
            CallStackItemPool.Return(xs);
            return returnAddress;
        }

        private IList<LuaValue> CreateArgsListForFunctionCall(int numargs, int offsFromTop)
        {
            if (numargs == 0)
            {
                return Array.Empty<LuaValue>();
            }

            LuaValue lastParam = _valueStack.Peek(offsFromTop);

            // Handle tuple expansion: when the last argument is a tuple, it needs special handling.
            // In Lua, when a function/varargs returns multiple values (or no values), those values
            // are expanded in the argument list, not passed as a single tuple argument.
            // - Empty tuple (0 elements): contributes 0 arguments
            // - Single-element tuple: contributes 1 argument (the element)
            // - Multi-element tuple: contributes N arguments (all elements)
            if (lastParam.Type == DataType.Tuple)
            {
                int tupleLength = lastParam.Tuple.Length;

                // Empty tuple: skip it entirely, return only the preceding arguments
                if (tupleLength == 0)
                {
                    if (numargs == 1)
                    {
                        return Array.Empty<LuaValue>();
                    }

                    // Return all arguments except the last (empty) tuple
                    return new Slice<LuaValue>(
                        _valueStack,
                        _valueStack.Count - numargs - offsFromTop,
                        numargs - 1,
                        false
                    );
                }

                // Single-element tuple: use just the first element
                if (tupleLength == 1)
                {
                    // Note: We can't use ListPool here because the caller needs the list
                    // to persist until ExecArgs completes.
                    List<LuaValue> values = new(numargs);

                    for (int idx = 0; idx < numargs - 1; idx++)
                    {
                        values.Add(_valueStack.Peek(numargs - idx - 1 + offsFromTop));
                    }

                    values.Add(lastParam.Tuple[0]);
                    return values;
                }

                // Multi-element tuple: expand all elements
                // Note: We can't use ListPool here because the caller needs the list
                // to persist until ExecArgs completes.
                List<LuaValue> expandedValues = new(numargs - 1 + tupleLength);

                for (int idx = 0; idx < numargs - 1; idx++)
                {
                    expandedValues.Add(_valueStack.Peek(numargs - idx - 1 + offsFromTop));
                }

                for (int idx = 0; idx < tupleLength; idx++)
                {
                    expandedValues.Add(lastParam.Tuple[idx]);
                }

                return expandedValues;
            }
            else
            {
                return new Slice<LuaValue>(
                    _valueStack,
                    _valueStack.Count - numargs - offsFromTop,
                    numargs,
                    false
                );
            }
        }

        private readonly ref struct VmCallArguments
        {
            private readonly ReadOnlySpan<LuaValue> _storedArguments;
            private readonly LuaValue[] _expandedTail;
            private readonly int _prefixCount;

            internal VmCallArguments(ReadOnlySpan<LuaValue> storedArguments)
            {
                _storedArguments = storedArguments;

                if (storedArguments.Length > 0 && storedArguments[^1].Type == DataType.Tuple)
                {
                    _expandedTail = storedArguments[^1].Tuple;
                    _prefixCount = storedArguments.Length - 1;
                    Count = _prefixCount + _expandedTail.Length;
                }
                else
                {
                    _expandedTail = null;
                    _prefixCount = storedArguments.Length;
                    Count = storedArguments.Length;
                }
            }

            internal int Count { get; }

            internal LuaValue this[int index]
            {
                get
                {
                    if ((uint)index >= (uint)Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    return index < _prefixCount
                        ? _storedArguments[index]
                        : _expandedTail[index - _prefixCount];
                }
            }
        }

        private VmCallArguments CreateArgsViewForFunctionCall(int numargs, int offsFromTop)
        {
            int start = _valueStack.Count - numargs - offsFromTop;
            if (!_valueStack.TryGetSpan(start, numargs, out ReadOnlySpan<LuaValue> arguments))
            {
                throw new InternalErrorException("Invalid function argument stack range");
            }

            return new VmCallArguments(arguments);
        }

        private void ExecArgs(Instruction instruction)
        {
            int numargs = (int)_valueStack.Peek(0).Number;

            // unpacks last tuple arguments to simplify a lot of code down under
            VmCallArguments argsList = CreateArgsViewForFunctionCall(numargs, 1);

            for (int i = 0; i < instruction.SymbolList.Length; i++)
            {
                // Check for varargs FIRST - this must be checked before i >= argsList.Count
                // because varargs with 0 arguments should create an empty tuple, not assign nil.
                // BUG FIX: Previously, when argsList.Count == 0 and i == 0, the "i >= argsList.Count"
                // branch was taken, incorrectly assigning nil to the varargs symbol instead of
                // creating an empty varargs tuple.
                if (
                    (i == instruction.SymbolList.Length - 1)
                    && (instruction.SymbolList[i].NameValue == WellKnownSymbols.VARARGS)
                )
                {
                    int len = Math.Max(0, argsList.Count - i);

                    // Handle empty varargs specially: use EmptyTuple to distinguish
                    // "zero arguments" from "one nil argument". This is critical for
                    // select("#", ...) to return 0 when no varargs are passed.
                    if (len == 0)
                    {
                        AssignLocal(instruction.SymbolList[^1], LuaValue.EmptyTuple);
                    }
                    else
                    {
                        LuaValue[] pooledVarargs = DynValueArrayPool.Rent(len);

                        for (int ii = 0; ii < len; ii++, i++)
                        {
                            pooledVarargs[ii] = argsList[i].ToScalar();
                        }

                        LuaValue[] varargs = DynValueArrayPool.ToArrayAndReturn(pooledVarargs, len);
                        AssignLocal(
                            instruction.SymbolList[^1],
                            LuaValue.NewTuple(InternalAdjustTuple(varargs))
                        );
                    }
                }
                else if (i >= argsList.Count)
                {
                    AssignLocal(instruction.SymbolList[i], LuaValue.Nil);
                }
                else
                {
                    AssignLocal(instruction.SymbolList[i], argsList[i].ToScalar());
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
            bool allowTailCallFrameReuse = false
        )
        {
            return InternalExecCall(
                argsCount,
                instructionPtr,
                handler,
                Continuation,
                thisCall,
                debugText,
                LuaValue.Nil,
                hasUnwindHandler: false,
                allowTailCallFrameReuse
            );
        }

        private int InternalExecCall(
            int argsCount,
            int instructionPtr,
            CallbackFunction handler,
            CallbackFunction Continuation,
            bool thisCall,
            string debugText,
            LuaValue unwindHandler,
            bool hasUnwindHandler,
            bool allowTailCallFrameReuse
        )
        {
            LuaValue fn = _valueStack.Peek(argsCount);
            CallStackItemFlags Flags = (thisCall ? CallStackItemFlags.MethodCall : default);
            bool canReuseTailCallFrame =
                allowTailCallFrameReuse
                && CanReuseFrameForKnownTailCallSite(
                    instructionPtr,
                    handler,
                    Continuation,
                    hasUnwindHandler
                );
            bool canReuseLuaTailCallFrame = canReuseTailCallFrame && fn.Type == DataType.Function;
            SourceRef callingSourceRef = GetCurrentSourceRef(instructionPtr - 1);

            if (!canReuseTailCallFrame || fn.Type == DataType.ClrFunction)
            {
                CheckSandboxCallStackDepth();
            }

            if (fn.Type == DataType.ClrFunction)
            {
                CallbackFunction callback = fn.Callback;

                CallStackItem frame = RentCallFrame();
                frame.ClrFunction = callback;
                frame.ReturnAddress = instructionPtr;
                frame.CallingSourceRef = callingSourceRef;
                frame.BasePointer = -1;
                frame.ErrorHandler = handler;
                frame.Continuation = Continuation;
                frame.SetErrorHandlerBeforeUnwind(unwindHandler, hasUnwindHandler);
                frame.Flags = Flags;
                _executionStack.Push(frame);

                LuaValue ret;
                if (callback.HasArgumentViewNoContextCallback)
                {
                    ret = callback.InvokeArgumentViewStack(
                        _script,
                        _valueStack,
                        _valueStack.Count - argsCount,
                        argsCount,
                        isMethodCall: thisCall
                    );
                }
                else
                {
                    ScriptExecutionContext context = new(this, callback, callingSourceRef);
                    if (callback.HasArgumentViewCallback)
                    {
                        ret = callback.InvokeArgumentViewStack(
                            context,
                            _valueStack,
                            _valueStack.Count - argsCount,
                            argsCount,
                            isMethodCall: thisCall
                        );
                    }
                    else
                    {
                        ret = InvokeLegacyCallbackFromStack(
                            callback,
                            context,
                            argsCount,
                            isMethodCall: thisCall
                        );
                    }
                }

                _valueStack.RemoveLast(argsCount + 1);
                _valueStack.Push(ret);

                CallStackItemPool.Return(_executionStack.Pop());

                return InternalCheckForTailRequests(null, instructionPtr, allowTailCallFrameReuse);
            }
            else if (fn.Type == DataType.Function)
            {
                CallbackFunction effectiveHandler = handler;
                CallbackFunction effectiveContinuation = Continuation;
                LuaValue effectiveUnwindHandler = unwindHandler;
                bool hasEffectiveUnwindHandler = hasUnwindHandler;

                if (canReuseLuaTailCallFrame)
                {
                    TailCallFrameReuseState reuseState = PerformTco(instructionPtr, argsCount);
                    instructionPtr = reuseState.ReturnAddress;
                    effectiveHandler = reuseState.ErrorHandler;
                    effectiveContinuation = reuseState.Continuation;
                    effectiveUnwindHandler = reuseState.ErrorHandlerBeforeUnwind;
                    hasEffectiveUnwindHandler = reuseState.HasErrorHandlerBeforeUnwind;
                    Flags |= CallStackItemFlags.TailCall;
                }

                // Push arguments before renting so a value-stack overflow throws with nothing rented; the
                // rent then only happens once both the value and execution stacks are known to have room.
                _valueStack.Push(LuaValue.FromNumber(argsCount));
                CallStackItem frame = RentCallFrame();
                frame.BasePointer = _valueStack.Count;
                frame.ReturnAddress = instructionPtr;
                frame.DebugEntryPoint = fn.Function.EntryPointByteCodeLocation;
                frame.CallingSourceRef = callingSourceRef;
                frame.ClosureScope = fn.Function.ClosureContext;
                frame.Function = fn;
                frame.ErrorHandler = effectiveHandler;
                frame.Continuation = effectiveContinuation;
                frame.SetErrorHandlerBeforeUnwind(
                    effectiveUnwindHandler,
                    hasEffectiveUnwindHandler
                );
                frame.Flags = Flags;
                _executionStack.Push(frame);
                return fn.Function.EntryPointByteCodeLocation;
            }

            // fallback to __call metamethod
            LuaValue m = GetMetamethod(fn, Metamethods.Call);

            if (m.IsNotNil() && CanCallMetamethod(m))
            {
                // Use pooled array for __call metamethod invocation
                using PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(
                    argsCount + 1,
                    out LuaValue[] tmp
                );

                for (int i = 0; i < argsCount + 1; i++)
                {
                    tmp[i] = _valueStack.Pop();
                }

                _valueStack.Push(m);

                for (int i = argsCount; i >= 0; i--)
                {
                    _valueStack.Push(tmp[i]);
                }

                return InternalExecCall(
                    argsCount + 1,
                    instructionPtr,
                    handler,
                    Continuation,
                    false,
                    null,
                    unwindHandler: unwindHandler,
                    hasUnwindHandler: hasUnwindHandler,
                    allowTailCallFrameReuse: allowTailCallFrameReuse
                );
            }

            throw ScriptRuntimeException.AttemptToCallNonFunc(fn.Type, debugText);
        }

        private bool CanReuseFrameForKnownTailCallSite(
            int instructionPtr,
            CallbackFunction handler,
            CallbackFunction continuation,
            bool hasUnwindHandler
        )
        {
            if (!IsTailCallReturnSite(instructionPtr))
            {
                return false;
            }

            CallStackItem currentFrame = _executionStack.Peek();
            return CanReuseFrameForLuaTailCall(
                currentFrame,
                handler,
                continuation,
                hasUnwindHandler
            );
        }

        private bool IsTailCallReturnSite(int instructionPtr)
        {
            if (instructionPtr < 0 || instructionPtr >= _rootChunk.Code.Count)
            {
                return false;
            }

            Instruction next = _rootChunk.Code[instructionPtr];
            return next.OpCode == OpCode.Ret && next.NumVal == 1;
        }

        private static bool CanReuseFrameForLuaTailCall(
            CallStackItem currentFrame,
            CallbackFunction handler,
            CallbackFunction continuation,
            bool hasUnwindHandler
        )
        {
            return currentFrame.ClrFunction == null
                && handler == null
                && continuation == null
                && !hasUnwindHandler
                && !HasPendingCloseHandlers(currentFrame);
        }

        private static bool HasPendingCloseHandlers(CallStackItem frame)
        {
            if (frame.ToBeClosedIndices != null && frame.ToBeClosedIndices.Count > 0)
            {
                return true;
            }

            if (frame.BlocksToClose == null)
            {
                return false;
            }

            foreach (List<SymbolRef> block in frame.BlocksToClose)
            {
                if (block != null && block.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private LuaValue InvokeLegacyCallbackFromStack(
            CallbackFunction callback,
            ScriptExecutionContext context,
            int argsCount,
            bool isMethodCall
        )
        {
            if (argsCount > 0 && _valueStack.Peek(0).Type == DataType.Tuple)
            {
                IList<LuaValue> tupleExpandedArgs = CreateArgsListForFunctionCall(argsCount, 0);
                return callback.InvokeLegacy(context, tupleExpandedArgs, isMethodCall);
            }

            switch (argsCount)
            {
                case 0:
                    return callback.InvokeLegacyFixed(context, isMethodCall);
                case 1:
                    return callback.InvokeLegacyFixed(context, _valueStack.Peek(0), isMethodCall);
                case 2:
                    return callback.InvokeLegacyFixed(
                        context,
                        _valueStack.Peek(1),
                        _valueStack.Peek(0),
                        isMethodCall
                    );
                case 3:
                    return callback.InvokeLegacyFixed(
                        context,
                        _valueStack.Peek(2),
                        _valueStack.Peek(1),
                        _valueStack.Peek(0),
                        isMethodCall
                    );
                case 4:
                    return callback.InvokeLegacyFixed(
                        context,
                        _valueStack.Peek(3),
                        _valueStack.Peek(2),
                        _valueStack.Peek(1),
                        _valueStack.Peek(0),
                        isMethodCall
                    );
                case 5:
                    return callback.InvokeLegacyFixed(
                        context,
                        _valueStack.Peek(4),
                        _valueStack.Peek(3),
                        _valueStack.Peek(2),
                        _valueStack.Peek(1),
                        _valueStack.Peek(0),
                        isMethodCall
                    );
                case 6:
                    return callback.InvokeLegacyFixed(
                        context,
                        _valueStack.Peek(5),
                        _valueStack.Peek(4),
                        _valueStack.Peek(3),
                        _valueStack.Peek(2),
                        _valueStack.Peek(1),
                        _valueStack.Peek(0),
                        isMethodCall
                    );
                case 7:
                    return callback.InvokeLegacyFixed(
                        context,
                        _valueStack.Peek(6),
                        _valueStack.Peek(5),
                        _valueStack.Peek(4),
                        _valueStack.Peek(3),
                        _valueStack.Peek(2),
                        _valueStack.Peek(1),
                        _valueStack.Peek(0),
                        isMethodCall
                    );
                default:
                    IList<LuaValue> args = CreateArgsListForFunctionCall(argsCount, 0);
                    return callback.InvokeLegacy(context, args, isMethodCall);
            }
        }

        private bool CanCallMetamethod(LuaValue metamethod)
        {
            return LuaVersionDefaults.Resolve(_script.Options.CompatibilityVersion)
                    >= LuaCompatibilityVersion.Lua54
                || metamethod.Type == DataType.Function
                || metamethod.Type == DataType.ClrFunction;
        }

        private TailCallFrameReuseState PerformTco(int instructionPtr, int argsCount)
        {
            System.Diagnostics.Debug.Assert(!HasPendingCloseHandlers(_executionStack.Peek()));

            // Use pooled array for tail call optimization
            using PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(
                argsCount + 1,
                out LuaValue[] args
            );

            // Remove all cur args and func ptr
            for (int i = 0; i <= argsCount; i++)
            {
                args[i] = _valueStack.Pop();
            }

            // perform a fake RET
            CallStackItem csi = PopToBasePointer();
            try
            {
                TailCallFrameReuseState reuseState = new(
                    csi.ReturnAddress,
                    csi.ErrorHandler,
                    csi.Continuation,
                    csi.ErrorHandlerBeforeUnwind,
                    csi.HasErrorHandlerBeforeUnwind
                );
                int argscnt = (int)(_valueStack.Pop().Number);
                _valueStack.RemoveLast(argscnt + 1);

                // Re-push all cur args and func ptr
                for (int i = argsCount; i >= 0; i--)
                {
                    _valueStack.Push(args[i]);
                }

                return reuseState;
            }
            finally
            {
                CallStackItemPool.Return(csi);
            }
        }

        private int ExecRet(Instruction i, int instructionPtr)
        {
            CallStackItem csi = _executionStack.Peek();
            int retpoint = csi.ReturnAddress;

            LuaValue returnValue;

            if (i.NumVal == 0)
            {
                returnValue = LuaValue.Void;
            }
            else if (i.NumVal == 1)
            {
                returnValue = _valueStack.Pop();
            }
            else
            {
                throw new InternalErrorException("RET supports only 0 and 1 ret val scenarios");
            }

            CloseAllPendingBlocks(
                csi,
                LuaValue.Nil,
                instructionPtr,
                decorateCloseErrorsBeforeUnwind: true
            );

            CallStackItem popped = PopToBasePointer();
            System.Diagnostics.Debug.Assert(object.ReferenceEquals(csi, popped));

            CallbackFunction continuation = null;

            try
            {
                int argscnt = (int)(_valueStack.Pop().Number);
                _valueStack.RemoveLast(argscnt + 1);

                _valueStack.Push(returnValue);

                if (i.NumVal == 1)
                {
                    retpoint = InternalCheckForTailRequests(
                        i,
                        retpoint,
                        allowTailCallFrameReuse: false
                    );
                }

                continuation = csi.Continuation;
            }
            finally
            {
                CallStackItemPool.Return(csi);
            }

            if (continuation != null)
            {
                ScriptExecutionContext executionContext = new(this, continuation, i.SourceCodeRef);
                LuaValue continuationArgument = _valueStack.Pop();
                // Argument-view continuations stay array-backed so TryGetSpan preserves
                // the public behavior of CallbackFunction.Invoke.
                LuaValue continuationReturn = continuation.HasArgumentViewCallback
                    ? continuation.Invoke(executionContext, new LuaValue[] { continuationArgument })
                    : continuation.InvokeLegacyFixed(executionContext, continuationArgument);

                _valueStack.Push(continuationReturn);
            }

            return retpoint;
        }

        private int InternalCheckForTailRequests(
            Instruction i,
            int instructionPtr,
            bool allowTailCallFrameReuse
        )
        {
            LuaValue tail = _valueStack.Peek(0);

            if (tail.Type == DataType.TailCallRequest)
            {
                _valueStack.Pop(); // discard tail call request

                TailCallData tcd = tail.TailCallData;

                _valueStack.Push(tcd.Function);

                ReadOnlySpan<LuaValue> tailArgs = tcd.ArgsSpan;
                for (int ii = 0; ii < tailArgs.Length; ii++)
                {
                    _valueStack.Push(tailArgs[ii]);
                }

                return InternalExecCall(
                    tailArgs.Length,
                    instructionPtr,
                    tcd.ErrorHandler,
                    tcd.Continuation,
                    false,
                    null,
                    tcd.ErrorHandlerBeforeUnwindValue,
                    tcd.HasErrorHandlerBeforeUnwind,
                    allowTailCallFrameReuse
                );
            }
            else if (tail.Type == DataType.YieldRequest)
            {
                _savedInstructionPtr = instructionPtr;
                return YieldSpecialTrap;
            }

            return instructionPtr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int JumpBool(Instruction i, bool expectedValueForJump, int instructionPtr)
        {
            LuaValue op = _valueStack.Pop().ToScalar();

            if (op.CastToBool() == expectedValueForJump)
            {
                return i.NumVal;
            }

            return instructionPtr;
        }

        private int ExecShortCircuitingOperator(Instruction i, int instructionPtr)
        {
            bool expectedValToShortCircuit = i.OpCode == OpCode.JtOrPop;

            LuaValue op = _valueStack.Peek().ToScalar();

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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (TryGetNumberOperands(l, r, out LuaNumber lnFast, out LuaNumber rnFast))
            {
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Add(lnFast, rnFast)));
                return instructionPtr;
            }

            LuaNumber? rn = CastToLuaNumberForArithmetic(r);
            LuaNumber? ln = CastToLuaNumberForArithmetic(l);

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Add(ln.Value, rn.Value)));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Add, instructionPtr);
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (TryGetNumberOperands(l, r, out LuaNumber lnFast, out LuaNumber rnFast))
            {
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Subtract(lnFast, rnFast)));
                return instructionPtr;
            }

            LuaNumber? rn = CastToLuaNumberForArithmetic(r);
            LuaNumber? ln = CastToLuaNumberForArithmetic(l);

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Subtract(ln.Value, rn.Value)));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Sub, instructionPtr);
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (TryGetNumberOperands(l, r, out LuaNumber lnFast, out LuaNumber rnFast))
            {
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Multiply(lnFast, rnFast)));
                return instructionPtr;
            }

            LuaNumber? rn = CastToLuaNumberForArithmetic(r);
            LuaNumber? ln = CastToLuaNumberForArithmetic(l);

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Multiply(ln.Value, rn.Value)));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Mul, instructionPtr);
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (TryGetNumberOperands(l, r, out LuaNumber lnFast, out LuaNumber rnFast))
            {
                _valueStack.Push(
                    LuaValue.NewNumber(
                        LuaNumber.Modulo(lnFast, rnFast, _script.Options.CompatibilityVersion)
                    )
                );
                return instructionPtr;
            }

            LuaNumber? rn = CastToLuaNumberForArithmetic(r);
            LuaNumber? ln = CastToLuaNumberForArithmetic(l);

            if (ln.HasValue && rn.HasValue)
            {
                _valueStack.Push(
                    LuaValue.NewNumber(
                        LuaNumber.Modulo(ln.Value, rn.Value, _script.Options.CompatibilityVersion)
                    )
                );
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Mod, instructionPtr);
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (TryGetNumberOperands(l, r, out LuaNumber lnFast, out LuaNumber rnFast))
            {
                // Regular division always returns float per Lua spec
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Divide(lnFast, rnFast)));
                return instructionPtr;
            }

            LuaNumber? rn = CastToLuaNumberForArithmetic(r);
            LuaNumber? ln = CastToLuaNumberForArithmetic(l);

            if (ln.HasValue && rn.HasValue)
            {
                // Regular division always returns float per Lua spec
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Divide(ln.Value, rn.Value)));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Div, instructionPtr);
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (TryGetNumberOperands(l, r, out LuaNumber lnFast, out LuaNumber rnFast))
            {
                // LuaNumber.FloorDivide handles Lua 5.3+ semantics:
                // - Integer // integer with div-by-zero throws error
                // - Float // float with div-by-zero returns inf/-inf
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.FloorDivide(lnFast, rnFast)));
                return instructionPtr;
            }

            LuaNumber? rn = CastToLuaNumberForArithmetic(r);
            LuaNumber? ln = CastToLuaNumberForArithmetic(l);

            if (ln.HasValue && rn.HasValue)
            {
                // LuaNumber.FloorDivide handles Lua 5.3+ semantics:
                // - Integer // integer with div-by-zero throws error
                // - Float // float with div-by-zero returns inf/-inf
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.FloorDivide(ln.Value, rn.Value)));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.IDiv, instructionPtr);
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (TryGetNumberOperands(l, r, out LuaNumber lnFast, out LuaNumber rnFast))
            {
                // Power always returns float per Lua spec
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Power(lnFast, rnFast)));
                return instructionPtr;
            }

            LuaNumber? rn = CastToLuaNumberForArithmetic(r);
            LuaNumber? ln = CastToLuaNumberForArithmetic(l);

            if (ln.HasValue && rn.HasValue)
            {
                // Power always returns float per Lua spec
                _valueStack.Push(LuaValue.NewNumber(LuaNumber.Power(ln.Value, rn.Value)));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Pow, instructionPtr);
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(l, r, Metamethods.Band, (x, y) => x & y, instructionPtr);
        }

        private int ExecBitOr(Instruction i, int instructionPtr)
        {
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(l, r, Metamethods.Bor, (x, y) => x | y, instructionPtr);
        }

        private int ExecBitXor(Instruction i, int instructionPtr)
        {
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(l, r, Metamethods.Bxor, (x, y) => x ^ y, instructionPtr);
        }

        private int ExecShiftLeft(Instruction i, int instructionPtr)
        {
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(
                l,
                r,
                Metamethods.Shl,
                (x, y) => LuaIntegerHelper.ShiftLeft(x, y),
                instructionPtr
            );
        }

        private int ExecShiftRight(Instruction i, int instructionPtr)
        {
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();
            return ExecBitwiseBinary(
                l,
                r,
                Metamethods.Shr,
                (x, y) => LuaIntegerHelper.ShiftRight(x, y),
                instructionPtr
            );
        }

        private int ExecBitNot(Instruction i, int instructionPtr)
        {
            LuaValue value = _valueStack.Pop().ToScalar();

            if (LuaIntegerHelper.TryGetInteger(value, out long operand))
            {
                _valueStack.Push(LuaValue.NewInteger(~operand));
                return instructionPtr;
            }

            int ip = InternalInvokeUnaryMetaMethod(value, Metamethods.Bnot, instructionPtr);
            if (ip >= 0)
            {
                return ip;
            }

            throw ScriptRuntimeException.BitwiseOnNonInteger(value);
        }

        private int ExecBitwiseBinary(
            LuaValue l,
            LuaValue r,
            string metamethodName,
            Func<long, long, long> operation,
            int instructionPtr
        )
        {
            bool leftOk = LuaIntegerHelper.TryGetInteger(l, out long left);
            bool rightOk = LuaIntegerHelper.TryGetInteger(r, out long right);

            if (leftOk && rightOk)
            {
                _valueStack.Push(LuaValue.NewInteger(operation(left, right)));
                return instructionPtr;
            }

            int ip = InternalInvokeBinaryMetaMethod(l, r, metamethodName, instructionPtr);
            if (ip >= 0)
            {
                return ip;
            }

            LuaValue invalid = leftOk ? r : l;
            throw ScriptRuntimeException.BitwiseOnNonInteger(invalid);
        }

        private int ExecNeg(Instruction i, int instructionPtr)
        {
            LuaValue r = _valueStack.Pop().ToScalar();
            if (r.Type == DataType.Number)
            {
                LuaNumber result = NegateNumberForVersion(r.LuaNumber);

                _valueStack.Push(LuaValue.NewNumber(result));
                return instructionPtr;
            }

            LuaNumber? rn = CastToLuaNumberForArithmetic(r);

            if (rn.HasValue)
            {
                LuaNumber result = NegateNumberForVersion(rn.Value);

                _valueStack.Push(LuaValue.NewNumber(result));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeUnaryMetaMethod(r, Metamethods.Unm, instructionPtr);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetNumberOperands(
            LuaValue l,
            LuaValue r,
            out LuaNumber ln,
            out LuaNumber rn
        )
        {
            if (l.Type == DataType.Number && r.Type == DataType.Number)
            {
                ln = l.LuaNumber;
                rn = r.LuaNumber;
                return true;
            }

            ln = default;
            rn = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaNumber NegateNumberForVersion(LuaNumber value)
        {
            LuaNumber result = LuaNumber.Negate(value);

            // Lua 5.1/5.2: negating integer zero should produce float negative zero.
            // In 5.3+, integers are a distinct subtype, so -0 stays integer 0.
            if (
                result.IsInteger
                && result.AsInteger == 0
                && _script.Options.CompatibilityVersion < LuaCompatibilityVersion.Lua53
            )
            {
                return LuaNumber.FromFloat(-0.0);
            }

            return result;
        }

        private int ExecEq(Instruction i, int instructionPtr)
        {
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            // Raw identity is conclusive before __eq. Compare the referenced Lua payload, never
            // the LuaValue wrapper: LuaValue is an inline value and has no wrapper identity.
            if (r.HasSameReferenceIdentity(l))
            {
                _valueStack.Push(LuaValue.True);
                return instructionPtr;
            }

            // then if they are userdatas, attempt meta
            if (l.Type == DataType.UserData || r.Type == DataType.UserData)
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Eq, instructionPtr);
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
                    _valueStack.Push(LuaValue.True);
                }
                else
                {
                    _valueStack.Push(LuaValue.False);
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
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Eq, instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
            }

            // else perform standard comparison
            _valueStack.Push(LuaValue.NewBoolean(r.Equals(l)));
            return instructionPtr;
        }

        private int ExecLess(Instruction i, int instructionPtr)
        {
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (l.Type == DataType.Number && r.Type == DataType.Number)
            {
                // Use LuaNumber comparison to preserve integer precision at boundaries
                _valueStack.Push(LuaValue.NewBoolean(LuaNumber.LessThan(l.LuaNumber, r.LuaNumber)));
            }
            else if (l.Type == DataType.String && r.Type == DataType.String)
            {
                int comparison = string.Compare(l.String, r.String, StringComparison.Ordinal);
                _valueStack.Push(LuaValue.NewBoolean(comparison < 0));
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Lt, instructionPtr);
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            if (l.Type == DataType.Number && r.Type == DataType.Number)
            {
                _valueStack.Push(LuaValue.False);
                // Use LuaNumber comparison to preserve integer precision at boundaries
                _valueStack.Push(
                    LuaValue.NewBoolean(LuaNumber.LessThanOrEqual(l.LuaNumber, r.LuaNumber))
                );
            }
            else if (l.Type == DataType.String && r.Type == DataType.String)
            {
                _valueStack.Push(LuaValue.False);
                int comparison = string.Compare(l.String, r.String, StringComparison.Ordinal);
                _valueStack.Push(LuaValue.NewBoolean(comparison <= 0));
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(
                    l,
                    r,
                    Metamethods.Le,
                    instructionPtr,
                    LuaValue.False
                );
                if (ip < 0)
                {
                    // Lua 5.5 removes the fallback from __lt to emulate __le.
                    // In earlier versions (5.1-5.4), if __le is not defined, we try __lt with swapped arguments.
                    // Note: Lua 5.4 manual §8.1 claims this was removed in 5.4, but actual Lua 5.4.x still supports it.
                    // Lua 5.5 (verified against lua5.5) actually removes this fallback behavior.
                    // Latest mode follows current target (Lua 5.4.x) behavior which allows the fallback.
                    Compatibility.LuaCompatibilityVersion version = _script
                        .Options
                        .CompatibilityVersion;
                    bool allowLtFallback = version != Compatibility.LuaCompatibilityVersion.Lua55;

                    if (allowLtFallback)
                    {
                        ip = InternalInvokeBinaryMetaMethod(
                            r,
                            l,
                            Metamethods.Lt,
                            instructionPtr,
                            LuaValue.True
                        );
                    }

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
            LuaValue r = _valueStack.Pop().ToScalar();

            if (r.Type == DataType.String)
            {
                _valueStack.Push(LuaValue.FromNumber(r.String.Length));
            }
            else
            {
                int ip = InternalInvokeUnaryMetaMethod(r, Metamethods.Len, instructionPtr);
                if (ip >= 0)
                {
                    return ip;
                }
                else if (r.Type == DataType.Table)
                {
                    _valueStack.Push(LuaValue.FromNumber(r.Table.Length));
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
            LuaValue r = _valueStack.Pop().ToScalar();
            LuaValue l = _valueStack.Pop().ToScalar();

            // Use version-aware CastToString for correct number formatting
            // Lua 5.1/5.2: integer-like floats format as "42"
            // Lua 5.3+: integer-like floats format as "42.0"
            LuaCompatibilityVersion version = _script.Options.CompatibilityVersion;
            string rs = r.CastToString(version);
            string ls = l.CastToString(version);

            if (rs != null && ls != null)
            {
                _valueStack.Push(LuaValue.NewConcatenatedString(ls, rs));
                return instructionPtr;
            }
            else
            {
                int ip = InternalInvokeBinaryMetaMethod(l, r, Metamethods.Concat, instructionPtr);
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
            LuaValue val = _valueStack.Pop();
            LuaValue tbl = _valueStack.Peek();

            if (tbl.Type != DataType.Table)
            {
                throw new InternalErrorException("Unexpected type in table ctor : {0}", tbl);
            }

            tbl.Table.InitNextArrayKeys(val, i.NumVal != 0);
        }

        private void ExecTblInitN(Instruction i)
        {
            // stack: tbl - key - val
            LuaValue val = _valueStack.Pop();
            LuaValue key = _valueStack.Pop();
            LuaValue tbl = _valueStack.Peek();

            if (tbl.Type != DataType.Table)
            {
                throw new InternalErrorException("Unexpected type in table ctor : {0}", tbl);
            }

            tbl.Table.InitNextKey(key, val);
        }

        private int ExecIndexSet(Instruction i, int instructionPtr)
        {
            int nestedMetaOps = 100; // sanity check, to avoid potential infinite loop here

            // stack: vals.. - base - index
            bool isNameIndex = i.OpCode == OpCode.IndexSetN;
            bool isMultiIndex = (i.OpCode == OpCode.IndexSetL);

            LuaValue originalIdx = i.HasValue ? i.Value : _valueStack.Pop();
            LuaValue idx = originalIdx.ToScalar();
            LuaValue obj = _valueStack.Pop().ToScalar();
            LuaValue value = GetStoreValue(i);
            LuaValue h = default;

            while (nestedMetaOps > 0)
            {
                --nestedMetaOps;

                if (obj.Type == DataType.Table)
                {
                    if (!isMultiIndex)
                    {
                        if (!obj.Table.GetValue(idx).IsNil)
                        {
                            obj.Table.SetValue(idx, value);
                            return instructionPtr;
                        }
                    }

                    h = GetMetamethodRaw(obj, Metamethods.NewIndex);

                    if (h.IsNil)
                    {
                        if (isMultiIndex)
                        {
                            throw new ScriptRuntimeException(
                                "cannot multi-index a table. userdata expected"
                            );
                        }

                        obj.Table.SetValue(idx, value);
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
                    h = GetMetamethodRaw(obj, Metamethods.NewIndex);

                    if (h.IsNil)
                    {
                        string varDesc = _script.Options.LuaCompatibleErrors ? i.Name : null;
                        throw ScriptRuntimeException.IndexType(obj, varDesc);
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
                    h = LuaValue.Nil;
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

            LuaValue originalIdx = i.HasValue ? i.Value : _valueStack.Pop();
            LuaValue idx = originalIdx.ToScalar();
            LuaValue obj = _valueStack.Pop().ToScalar();

            LuaValue h = default;

            while (nestedMetaOps > 0)
            {
                --nestedMetaOps;

                if (obj.Type == DataType.Table)
                {
                    if (!isMultiIndex)
                    {
                        LuaValue v = obj.Table.GetValue(idx);

                        if (!v.IsNil)
                        {
                            _valueStack.Push(v);
                            return instructionPtr;
                        }
                    }

                    h = GetMetamethodRaw(obj, Metamethods.Index);

                    if (h.IsNil)
                    {
                        if (isMultiIndex)
                        {
                            throw new ScriptRuntimeException(
                                "cannot multi-index a table. userdata expected"
                            );
                        }

                        _valueStack.Push(LuaValue.Nil);
                        return instructionPtr;
                    }
                }
                else if (obj.Type == DataType.UserData)
                {
                    UserData ud = obj.UserData;

                    if (
                        !UserDataAccess.TryIndex(
                            ud.Descriptor,
                            GetScript(),
                            ud.Object,
                            originalIdx,
                            isNameIndex,
                            out LuaValue v
                        )
                    )
                    {
                        throw ScriptRuntimeException.UserDataMissingField(
                            ud.Descriptor.Name,
                            idx.String
                        );
                    }

                    _valueStack.Push(v);
                    return instructionPtr;
                }
                else
                {
                    h = GetMetamethodRaw(obj, Metamethods.Index);

                    if (h.IsNil)
                    {
                        string varDesc = _script.Options.LuaCompatibleErrors ? i.Name : null;
                        throw ScriptRuntimeException.IndexType(obj, varDesc);
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
                    h = LuaValue.Nil;
                }
            }

            throw ScriptRuntimeException.LoopInIndex();
        }

        /// <summary>
        /// Checks whether the current call stack depth exceeds the sandbox limit.
        /// Throws <see cref="SandboxViolationException"/> if the limit is exceeded and
        /// the callback does not allow continuation.
        /// </summary>
        private void CheckSandboxCallStackDepth()
        {
            SandboxOptions sandbox = _script.Options.Sandbox;
            if (!sandbox.HasCallStackDepthLimit)
            {
                return;
            }

            int currentDepth = _executionStack.Count;
            int maxDepth = sandbox.MaxCallStackDepth;

            if (currentDepth >= maxDepth)
            {
                Func<Script, int, bool> callback = sandbox.OnRecursionLimitExceeded;
                if (callback == null || !callback(_script, currentDepth))
                {
                    throw new SandboxViolationException(
                        SandboxViolationType.RecursionLimitExceeded,
                        maxDepth,
                        currentDepth
                    );
                }
            }
        }

        /// <summary>
        /// Rents a pooled call frame after verifying the execution stack is below its configured ceiling.
        /// Checking before renting means an overflow throws with nothing rented, so the pooled frame is never
        /// leaked on the (pcall-catchable) overflow path. The subsequent push then cannot exceed the ceiling.
        /// </summary>
        /// <returns>A pooled call frame the caller must push and eventually return.</returns>
        private CallStackItem RentCallFrame()
        {
            int maxCapacity = _executionStack.MaxCapacity;
            if (maxCapacity > 0 && _executionStack.Count >= maxCapacity)
            {
                throw ScriptRuntimeException.StackOverflow();
            }

            return CallStackItemPool.Rent();
        }

        /// <summary>
        /// Converts a LuaValue to a LuaNumber for arithmetic operations.
        /// In Lua 5.4+, strings are NOT automatically coerced to numbers by the arithmetic operators;
        /// instead, coercion happens via the string metatable's arithmetic metamethods.
        /// In Lua 5.1-5.3, strings are automatically coerced to numbers.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// The LuaNumber if conversion is possible without violating version semantics;
        /// null if the value cannot be used as a number in arithmetic.
        /// </returns>
        private LuaNumber? CastToLuaNumberForArithmetic(LuaValue value)
        {
            DataType type = value.Type;

            // Numbers always convert
            if (type == DataType.Number)
            {
                return value.LuaNumber;
            }

            // Strings: version-dependent behavior
            // In Lua 5.4+, strings are NOT coerced by the arithmetic operators themselves;
            // the coercion happens via string metatable metamethods (__add, __sub, etc.)
            if (type == DataType.String)
            {
                LuaCompatibilityVersion version = _script.Options.CompatibilityVersion;
                if (version >= LuaCompatibilityVersion.Lua54)
                {
                    // 5.4+: Do NOT coerce strings here; fall through to metamethod lookup
                    return null;
                }

                // 5.1-5.3: Auto-coerce strings to numbers
                if (LuaNumber.TryParse(value.String, version, out LuaNumber result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}
