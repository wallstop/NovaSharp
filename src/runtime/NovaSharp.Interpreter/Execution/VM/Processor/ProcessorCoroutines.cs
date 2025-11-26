namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

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
        public DynValue CreateCoroutine(Closure closure)
        {
            // create a processor instance
            Processor p = new(this);

            // Put the closure as first value on the stack, for future reference
            p._valueStack.Push(DynValue.NewClosure(closure));

            // Return the coroutine handle
            return DynValue.NewCoroutine(new Coroutine(p));
        }

        /// <summary>
        /// Reuses an existing processor instance to create a coroutine without re-allocating stacks.
        /// </summary>
        public DynValue RecycleCoroutine(Processor mainProcessor, Closure closure)
        {
            // Clear the used parts of the stacks to prep for reuse
            _valueStack.ClearUsed();
            _executionStack.ClearUsed();

            // Create a new processor instance, recycling this one
            Processor p = new(mainProcessor, this);

            // Put the closure as first value on the stack, for future reference
            p._valueStack.Push(DynValue.NewClosure(closure));

            // Return the coroutine handle
            return DynValue.NewCoroutine(new Coroutine(p));
        }

        /// <summary>
        /// Gets the current coroutine state.
        /// </summary>
        public CoroutineState State
        {
            get { return _state; }
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
        public DynValue ResumeCoroutine(DynValue[] args)
        {
            EnterProcessor();

            try
            {
                int entrypoint = 0;

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
                        CallStackItemFlags.ResumeEntryPoint,
                        null,
                        args
                    );
                }
                else if (_state == CoroutineState.Suspended)
                {
                    _valueStack.Push(DynValue.NewTuple(args));
                    entrypoint = _savedInstructionPtr;
                }
                else if (_state == CoroutineState.ForceSuspended)
                {
                    if (args != null && args.Length > 0)
                    {
                        throw new ArgumentException(
                            "When resuming a force-suspended coroutine, args must be empty."
                        );
                    }

                    entrypoint = _savedInstructionPtr;
                }

                _state = CoroutineState.Running;
                DynValue retVal = ProcessingLoop(entrypoint);

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
                        _lastCloseError = DynValue.Nil;
                        return DynValue.NewTuple(retVal.YieldRequest.ReturnValues);
                    }
                }
                else
                {
                    _state = CoroutineState.Dead;
                    _lastCloseError = DynValue.Nil;
                    return retVal;
                }
            }
            catch (ScriptRuntimeException ex)
            {
                _state = CoroutineState.Dead;
                _lastCloseError = DynValue.NewString(ex.DecoratedMessage);
                throw;
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
        public DynValue CloseCoroutine()
        {
            EnterProcessor();

            try
            {
                if (_state == CoroutineState.Main)
                {
                    throw ScriptRuntimeException.CannotCloseCoroutine(_state);
                }

                if (_state == CoroutineState.Running)
                {
                    throw ScriptRuntimeException.CannotCloseCoroutine(_state);
                }

                if (_state == CoroutineState.Dead)
                {
                    return BuildCloseResultFromLastError();
                }

                if (_state == CoroutineState.NotStarted)
                {
                    _state = CoroutineState.Dead;
                    _lastCloseError = DynValue.Nil;
                    return DynValue.True;
                }

                if (_state != CoroutineState.Suspended && _state != CoroutineState.ForceSuspended)
                {
                    throw ScriptRuntimeException.CannotCloseCoroutine(_state);
                }

                try
                {
                    while (_executionStack.Count > 0)
                    {
                        CallStackItem frame = PopToBasePointer();
                        CloseAllPendingBlocks(frame, DynValue.Nil);
                    }

                    _valueStack.Clear();
                    _lastCloseError = DynValue.Nil;
                    _state = CoroutineState.Dead;
                    return DynValue.True;
                }
                catch (ScriptRuntimeException ex)
                {
                    DynValue error = DynValue.NewString(ex.DecoratedMessage);
                    _lastCloseError = error;
                    _state = CoroutineState.Dead;
                    return DynValue.NewTuple(DynValue.False, error);
                }
            }
            finally
            {
                LeaveProcessor();
            }
        }

        /// <summary>
        /// Builds the result tuple returned by <see cref="CloseCoroutine"/> based on the last error.
        /// </summary>
        private DynValue BuildCloseResultFromLastError()
        {
            if (_lastCloseError != null && !_lastCloseError.IsNil())
            {
                return DynValue.NewTuple(DynValue.False, _lastCloseError);
            }

            return DynValue.True;
        }
    }
}
