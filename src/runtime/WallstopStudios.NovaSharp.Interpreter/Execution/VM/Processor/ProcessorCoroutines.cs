namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    // This part is practically written procedural style - it looks more like C than C#.
    // This is intentional so to avoid this-calls and virtual-calls as much as possible.
    // Same reason for the "sealed" declaration.
    /// <content>
    /// Contains coroutine creation, scheduling, and teardown logic.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Creates a new coroutine bound to the specified closure.
        /// </summary>
        /// <param name="closure">Closure to execute when the coroutine starts.</param>
        /// <returns>A coroutine handle.</returns>
        public LuaValue CreateCoroutine(Closure closure)
        {
            // create a processor instance
            Processor p = new(this);

            // Put the closure as first value on the stack, for future reference
            // Use FromClosure to avoid allocation when the closure already has a cached LuaValue
            p._valueStack.Push(LuaValue.FromClosure(closure));

            // Return the coroutine handle
            return LuaValue.NewCoroutine(new Coroutine(p));
        }

        /// <summary>
        /// Reuses an existing processor instance to create a coroutine without re-allocating stacks.
        /// </summary>
        public LuaValue RecycleCoroutine(Processor mainProcessor, Closure closure)
        {
            if (HasPendingCloseHandlers())
            {
                throw new InvalidOperationException(
                    "cannot recycle a coroutine with pending close handlers; call coroutine.close first"
                );
            }

            // Clear the used parts of the stacks to prep for reuse
            _valueStack.ClearUsed();
            _executionStack.ClearUsed();
            _discardResumeResult = false;

            // Create a new processor instance, recycling this one
            Processor p = new(mainProcessor, this);

            // Put the closure as first value on the stack, for future reference
            // Use FromClosure to avoid allocation when the closure already has a cached LuaValue
            p._valueStack.Push(LuaValue.FromClosure(closure));

            // Return the coroutine handle
            return LuaValue.NewCoroutine(new Coroutine(p));
        }

        private bool HasPendingCloseHandlers()
        {
            for (int index = 0; index < _executionStack.Count; index++)
            {
                CallStackItem frame = _executionStack[index];

                if (frame.ToBeClosedIndices != null && frame.ToBeClosedIndices.Count > 0)
                {
                    return true;
                }

                if (frame.BlocksToClose == null)
                {
                    continue;
                }

                for (int blockIndex = 0; blockIndex < frame.BlocksToClose.Count; blockIndex++)
                {
                    if (frame.BlocksToClose[blockIndex].Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the current coroutine state.
        /// </summary>
        public CoroutineState State
        {
            get { return _isClosingCoroutine ? CoroutineState.Running : _state; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Coroutine"/> wrapper associated with this processor.
        /// </summary>
        public Coroutine AssociatedCoroutine { get; set; }

        /// <summary>
        /// Resumes execution, optionally passing arguments to the coroutine entry point or yield continuation.
        /// </summary>
        /// <param name="args">Arguments supplied to <c>coroutine.resume</c>.</param>
        /// <returns>Return values or yield results.</returns>
        /// <exception cref="ScriptRuntimeException">Thrown when resuming invalid states.</exception>
        public LuaValue ResumeCoroutine(LuaValue[] args)
        {
            return ResumeCoroutine(new ClrCallArguments(args));
        }

        /// <summary>
        /// Resumes execution with caller-owned contiguous arguments.
        /// </summary>
        public LuaValue ResumeCoroutine(ReadOnlySpan<LuaValue> args)
        {
            return ResumeCoroutine(new ClrCallArguments(args));
        }

        /// <summary>
        /// Resumes execution with one argument, avoiding public params-array allocation.
        /// </summary>
        public LuaValue ResumeCoroutine(LuaValue arg)
        {
            return ResumeCoroutine(new ClrCallArguments(arg));
        }

        /// <summary>
        /// Resumes execution with two arguments, avoiding public params-array allocation.
        /// </summary>
        public LuaValue ResumeCoroutine(LuaValue arg1, LuaValue arg2)
        {
            return ResumeCoroutine(new ClrCallArguments(arg1, arg2));
        }

        /// <summary>
        /// Resumes execution with three arguments, avoiding public params-array allocation.
        /// </summary>
        public LuaValue ResumeCoroutine(LuaValue arg1, LuaValue arg2, LuaValue arg3)
        {
            return ResumeCoroutine(new ClrCallArguments(arg1, arg2, arg3));
        }

        /// <summary>
        /// Resumes execution with four arguments, avoiding public params-array allocation.
        /// </summary>
        public LuaValue ResumeCoroutine(LuaValue arg1, LuaValue arg2, LuaValue arg3, LuaValue arg4)
        {
            return ResumeCoroutine(new ClrCallArguments(arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Resumes execution with five arguments, avoiding public params-array allocation.
        /// </summary>
        public LuaValue ResumeCoroutine(
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5
        )
        {
            return ResumeCoroutine(new ClrCallArguments(arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Resumes execution with six arguments, avoiding public params-array allocation.
        /// </summary>
        public LuaValue ResumeCoroutine(
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6
        )
        {
            return ResumeCoroutine(new ClrCallArguments(arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Resumes execution with seven arguments, avoiding public params-array allocation.
        /// </summary>
        public LuaValue ResumeCoroutine(
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7
        )
        {
            return ResumeCoroutine(new ClrCallArguments(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        internal void EnterProtectedClrCall()
        {
            _protectedClrCallDepth++;
        }

        internal void LeaveProtectedClrCall()
        {
            _protectedClrCallDepth--;
        }

        /// <summary>
        /// Marks a classic-library callback that is calling back into Lua on behalf of a running
        /// script. Errors from such calls follow Lua callback semantics instead of host boundary
        /// semantics. The marker applies only to the entry frame this call pushes.
        /// </summary>
        internal LibraryCallBoundaryScope EnterLibraryCallBoundary()
        {
            return new LibraryCallBoundaryScope(this, _libraryEntryPending);
        }

        /// <summary>
        /// Scope that marks the next entry frame pushed by a classic-library callback.
        /// </summary>
        internal readonly struct LibraryCallBoundaryScope : IDisposable
        {
            private readonly Processor _processor;
            private readonly bool _previousPending;

            public LibraryCallBoundaryScope(Processor processor, bool previousPending)
            {
                _processor = processor;
                _previousPending = previousPending;
                _processor._libraryEntryPending = true;
            }

            /// <summary>
            /// Ends the classic-library call scope.
            /// </summary>
            public void Dispose()
            {
                _processor._libraryEntryPending = _previousPending;
            }
        }

        private CallStackItemFlags ConsumeCallEntryOrigin()
        {
            CallStackItemFlags flags = default;
            if (_libraryEntryPending)
            {
                _libraryEntryPending = false;
                flags |= CallStackItemFlags.LibraryCallEntryBit;
            }

            if (_vmCloseEntryPending)
            {
                _vmCloseEntryPending = false;
                flags |= CallStackItemFlags.VmCloseCallEntryBit;
            }

            return flags;
        }

        /// <summary>
        /// Marks the next entry frame as the VM-owned invocation of a Lua close handler.
        /// </summary>
        internal VmCloseCallBoundaryScope EnterVmCloseCallBoundary()
        {
            return new VmCloseCallBoundaryScope(this, _vmCloseEntryPending);
        }

        /// <summary>
        /// Scope that marks the next entry frame pushed for a Lua close handler.
        /// </summary>
        internal readonly struct VmCloseCallBoundaryScope : IDisposable
        {
            private readonly Processor _processor;
            private readonly bool _previousPending;

            public VmCloseCallBoundaryScope(Processor processor, bool previousPending)
            {
                _processor = processor;
                _previousPending = previousPending;
                _processor._vmCloseEntryPending = true;
            }

            /// <summary>
            /// Ends the VM close-handler call scope.
            /// </summary>
            public void Dispose()
            {
                _processor._vmCloseEntryPending = _previousPending;
            }
        }

        private LuaValue ResumeCoroutine(ClrCallArguments args)
        {
            EnterProcessor();
            bool executionStarted = false;

            try
            {
                int entrypoint = 0;

                if (_isClosingCoroutine)
                {
                    throw ScriptRuntimeException.CannotResumeNotSuspended(CoroutineState.Running);
                }

                if (
                    _state != CoroutineState.NotStarted
                    && _state != CoroutineState.Suspended
                    && _state != CoroutineState.ForceSuspended
                )
                {
                    throw ScriptRuntimeException.CannotResumeNotSuspended(_state);
                }

                if (_state == CoroutineState.NotStarted)
                {
                    entrypoint = PushClrToScriptStackFrame(
                        CallStackItemFlagsPresets.ResumeEntryPoint,
                        args
                    );
                }
                else if (_state == CoroutineState.Suspended)
                {
                    if (_discardResumeResult)
                    {
                        _discardResumeResult = false;
                        _resumeYieldWithSingleResult = false;
                    }
                    else if (_resumeYieldWithSingleResult)
                    {
                        _resumeYieldWithSingleResult = false;
                        LuaValue first = args.Count == 0 ? LuaValue.Nil : args[0].ToScalar();
                        _valueStack.Push(first.IsVoid() ? LuaValue.Nil : first);
                    }
                    else
                    {
                        _valueStack.Push(args.ToTuple());
                    }

                    entrypoint = _savedInstructionPtr;
                }
                else if (_state == CoroutineState.ForceSuspended)
                {
                    if (args.Count > 0)
                    {
                        throw new ArgumentException(
                            "When resuming a force-suspended coroutine, args must be empty."
                        );
                    }

                    entrypoint = _savedInstructionPtr;
                }

                _state = CoroutineState.Running;
                executionStarted = true;
                LuaValue retVal = ProcessingLoop(entrypoint);

                if (retVal.Type == DataType.YieldRequest)
                {
                    if (retVal.YieldRequest.Forced)
                    {
                        _state = CoroutineState.ForceSuspended;
                        return retVal;
                    }
                    else
                    {
                        _state = CoroutineState.Suspended;
                        _lastCloseError = LuaValue.Nil;
                        _lastCloseFailed = false;
                        return retVal.YieldRequest.ToTuple();
                    }
                }
                else
                {
                    _state = CoroutineState.Dead;
                    _lastCloseError = LuaValue.Nil;
                    _lastCloseFailed = false;
                    return retVal;
                }
            }
            catch (ScriptRuntimeException ex)
            {
                _state = CoroutineState.Dead;

                if (executionStarted)
                {
                    _lastCloseError = ex.GetCaughtErrorObject(
                        LuaVersionDefaults.Resolve(_script.CompatibilityVersion)
                    );
                    _lastCloseFailed = true;
                }

                throw;
            }
            catch (CoroutineSelfCloseException selfCloseException)
                when (selfCloseException.AbortResume)
            {
                LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                    _script.CompatibilityVersion
                );
                ScriptRuntimeException pendingError = selfCloseException.PendingError;
                bool failed = pendingError != null || selfCloseException.CloseError != null;
                LuaValue closeError =
                    selfCloseException.CloseError != null
                        ? selfCloseException.CloseError.GetCaughtErrorObject(version)
                        : LuaValue.Nil;

                _isClosingCoroutine = true;
                try
                {
                    if (UnwindFramesForClose(closeError, out LuaValue unwindError))
                    {
                        closeError = unwindError;
                        failed = true;
                    }
                }
                finally
                {
                    _isClosingCoroutine = false;
                }

                _state = CoroutineState.Dead;
                _lastCloseError = closeError;
                _lastCloseFailed = failed;

                if (failed)
                {
                    ScriptRuntimeException resumeError = pendingError;
                    if (resumeError == null)
                    {
                        resumeError = new ScriptRuntimeException(closeError.ToPrintString(version));
                        resumeError.SetErrorValue(closeError);
                    }

                    throw resumeError;
                }

                return LuaValue.Void;
            }
            catch (Exception)
            {
                // Unhandled exception - move to dead
                _state = CoroutineState.Dead;
                throw;
            }
            finally
            {
                LeaveProcessor();
            }
        }

        /// <summary>
        /// Forces the coroutine to close, unwinding pending blocks and returning Lua-compatible status tuples.
        /// </summary>
        public LuaValue CloseCoroutine()
        {
            Processor caller = ActiveProcessor;
            EnterProcessor();

            try
            {
                if (_state == CoroutineState.Main)
                {
                    throw ScriptRuntimeException.CannotCloseCoroutine(_state);
                }

                if (_isClosingCoroutine)
                {
                    LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                        _script.CompatibilityVersion
                    );
                    if (caller != this)
                    {
                        throw new ScriptRuntimeException("cannot close a normal coroutine");
                    }

                    if (version >= LuaCompatibilityVersion.Lua55 && _closeUnwindBoundaryDepth >= 0)
                    {
                        _lastCloseError = LuaValue.Nil;
                        _selfCloseResetRequested = true;
                        while (_executionStack.Count > _closeUnwindBoundaryDepth)
                        {
                            CallStackItem frame = _executionStack.Peek();
                            if (object.ReferenceEquals(frame, _closeBlockFrame))
                            {
                                // The active close block still has detached
                                // siblings to drain; the outer unwind closes
                                // this frame after they finish.
                                break;
                            }

                            PopToBasePointer();
                            try
                            {
                                CloseAllPendingBlocks(frame, LuaValue.Nil);
                            }
                            finally
                            {
                                CallStackItemPool.Return(frame);
                            }
                        }

                        throw new CoroutineSelfCloseException();
                    }

                    throw ScriptRuntimeException.CannotCloseCoroutine(CoroutineState.Running);
                }

                if (_state == CoroutineState.Running)
                {
                    if (caller != this)
                    {
                        // A running coroutine other than the active one is an ancestor of the
                        // active coroutine, so Lua reports it as "normal".
                        throw new ScriptRuntimeException("cannot close a normal coroutine");
                    }

                    LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                        _script.CompatibilityVersion
                    );
                    if (version >= LuaCompatibilityVersion.Lua55)
                    {
                        // Lua 5.5 closes the running coroutine from its own execution: the
                        // call aborts to the resume boundary after every pending close handler
                        // has run. Handlers belonging to the aborting frame run before the
                        // remaining siblings of the enclosing block; ResumeCoroutine drains
                        // the rest once the CLR stack unwinds.
                        ScriptRuntimeException frameCloseError = DrainHandlerFramesBeforeSelfClose(
                            version
                        );

                        throw new CoroutineSelfCloseException
                        {
                            AbortResume = true,
                            CloseError = frameCloseError,
                        };
                    }

                    throw ScriptRuntimeException.CannotCloseCoroutine(_state);
                }

                if (_state == CoroutineState.Dead)
                {
                    if (_lastCloseFailed && _executionStack.Count > 0)
                    {
                        return CloseDeferredCoroutineFrames();
                    }

                    return BuildCloseResultFromLastError();
                }

                if (_state == CoroutineState.NotStarted)
                {
                    _state = CoroutineState.Dead;
                    _lastCloseError = LuaValue.Nil;
                    _lastCloseFailed = false;
                    return LuaValue.True;
                }

                if (_state != CoroutineState.Suspended && _state != CoroutineState.ForceSuspended)
                {
                    throw ScriptRuntimeException.CannotCloseCoroutine(_state);
                }

                _isClosingCoroutine = true;
                bool failed;
                LuaValue closeError;
                try
                {
                    failed = UnwindFramesForClose(LuaValue.Nil, out closeError);
                }
                finally
                {
                    _isClosingCoroutine = false;
                }

                _lastCloseError = LuaValue.Nil;
                _lastCloseFailed = false;
                _state = CoroutineState.Dead;
                return failed ? LuaValue.NewTuple(LuaValue.False, closeError) : LuaValue.True;
            }
            finally
            {
                LeaveProcessor();
            }
        }

        /// <summary>
        /// Runs the close handlers a failed coroutine kept past its uncaught error.
        /// </summary>
        private LuaValue CloseDeferredCoroutineFrames()
        {
            try
            {
                _isClosingCoroutine = true;
                UnwindFramesForClose(_lastCloseError, out LuaValue closeError);
                return LuaValue.NewTuple(LuaValue.False, closeError);
            }
            finally
            {
                _isClosingCoroutine = false;
                _lastCloseError = LuaValue.Nil;
                _lastCloseFailed = false;
                _state = CoroutineState.Dead;
            }
        }

        /// <summary>
        /// Closes every pending block in stack order, continuing after a handler fails.
        /// </summary>
        private bool UnwindFramesForClose(LuaValue initialError, out LuaValue closeError)
        {
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                _script.CompatibilityVersion
            );
            LuaValue activeError = initialError;
            bool failed = false;
            _selfCloseStarted = false;
            _selfCloseFailed = false;
            _selfCloseError = null;
            _selfCloseErrorSet = false;

            while (_executionStack.Count > 0)
            {
                CallStackItem frame = PopToBasePointer();
                int previousBoundaryDepth = _closeUnwindBoundaryDepth;
                _closeUnwindBoundaryDepth = _executionStack.Count;
                try
                {
                    CloseAllPendingBlocks(frame, activeError);
                }
                catch (ScriptRuntimeException closeException)
                {
                    activeError = closeException.GetCaughtErrorObject(version);
                    failed = true;
                    _selfCloseResetRequested = false;
                }
                finally
                {
                    if (_selfCloseResetRequested)
                    {
                        activeError = LuaValue.Nil;
                        _selfCloseResetRequested = false;
                    }

                    if (_selfCloseFailed)
                    {
                        failed = true;
                        _selfCloseFailed = false;
                    }

                    if (_selfCloseErrorSet)
                    {
                        failed = true;
                        activeError = _selfCloseError.GetCaughtErrorObject(version);
                        _selfCloseError = null;
                        _selfCloseErrorSet = false;
                    }

                    _closeUnwindBoundaryDepth = previousBoundaryDepth;
                    CallStackItemPool.Return(frame);
                }
            }

            _valueStack.Clear();
            closeError = activeError;
            return failed;
        }

        /// <summary>
        /// Builds the result tuple returned by <see cref="CloseCoroutine"/> based on the last error.
        /// </summary>
        private LuaValue BuildCloseResultFromLastError()
        {
            LuaValue error = _lastCloseError;
            bool failed = _lastCloseFailed;
            _lastCloseError = LuaValue.Nil;
            _lastCloseFailed = false;

            if (failed)
            {
                return LuaValue.NewTuple(LuaValue.False, error);
            }

            return LuaValue.True;
        }

        /// <summary>
        /// Drains the coroutine frames that belong to the close handler requesting a Lua 5.5
        /// self-close. Lua closes those nested frames before the siblings of the enclosing block.
        /// </summary>
        /// <returns>The last close failure raised while draining, if any.</returns>
        private ScriptRuntimeException DrainHandlerFramesBeforeSelfClose(
            LuaCompatibilityVersion version
        )
        {
            if (_closeHandlerDepth == 0 || _closeBlockFrame == null)
            {
                return null;
            }

            int boundary =
                _closeBlockFrameDepth >= 0 ? _closeBlockFrameDepth : _executionStack.Count;
            bool previousIsClosing = _isClosingCoroutine;
            int previousUnwindBoundary = _closeUnwindBoundaryDepth;
            _isClosingCoroutine = true;
            _closeUnwindBoundaryDepth = boundary;
            ScriptRuntimeException closeError = null;
            LuaValue frameError = LuaValue.Nil;
            try
            {
                while (_executionStack.Count > boundary)
                {
                    CallStackItem frame = PopToBasePointer();
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                CloseAllPendingBlocks(frame, frameError);
                                break;
                            }
                            catch (CoroutineSelfCloseException)
                            {
                                // A closer requested its own close while the coroutine already
                                // aborts: Lua resets the error seen by the handlers it aborts and
                                // keeps closing the remaining blocks.
                                closeError = null;
                                frameError = LuaValue.Nil;
                                _selfCloseResetRequested = false;
                            }
                            catch (ScriptRuntimeException frameException)
                            {
                                closeError = frameException;
                                frameError = frameException.GetCaughtErrorObject(version);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        CallStackItemPool.Return(frame);
                    }
                }
            }
            finally
            {
                _isClosingCoroutine = previousIsClosing;
                _closeUnwindBoundaryDepth = previousUnwindBoundary;
            }

            return closeError;
        }

        public sealed class CoroutineSelfCloseException : Exception
        {
            public ScriptRuntimeException CloseError { get; set; }

            /// <summary>
            /// Gets or sets the error that was already unwinding when the coroutine requested its
            /// own close. Lua 5.5 reports this error to the resume boundary even when later
            /// handlers run successfully.
            /// </summary>
            public ScriptRuntimeException PendingError { get; set; }

            /// <summary>
            /// Gets or sets whether the self-close originated in ordinary coroutine execution and
            /// must abort the resume boundary after pending close handlers are drained.
            /// </summary>
            public bool AbortResume { get; set; }

            public CoroutineSelfCloseException() { }

            public CoroutineSelfCloseException(string message)
                : base(message) { }

            public CoroutineSelfCloseException(string message, Exception innerException)
                : base(message, innerException) { }
        }
    }
}
