namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    // This part is practically written procedural style - it looks more like C than C#.
    // This is intentional so to avoid this-calls and virtual-calls as much as possible.
    // Same reason for the "sealed" declaration.
    internal sealed partial class Processor
    {
        public DynValue Coroutine_Create(Closure closure)
        {
            // create a processor instance
            Processor p = new(this);

            // Put the closure as first value on the stack, for future reference
            p._valueStack.Push(DynValue.NewClosure(closure));

            // Return the coroutine handle
            return DynValue.NewCoroutine(new Coroutine(p));
        }

        public DynValue Coroutine_Recycle(Processor mainProcessor, Closure closure)
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

        public CoroutineState State
        {
            get { return _state; }
        }
        public Coroutine AssociatedCoroutine { get; set; }

        public DynValue Coroutine_Resume(DynValue[] args)
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
                DynValue retVal = Processing_Loop(entrypoint);

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
                        return DynValue.NewTuple(retVal.YieldRequest.ReturnValues);
                    }
                }
                else
                {
                    _state = CoroutineState.Dead;
                    return retVal;
                }
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
    }
}
