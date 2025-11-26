namespace NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// A class representing a script coroutine
    /// </summary>
    public class Coroutine : RefIdObject, IScriptPrivateResource
    {
        /// <summary>
        /// Possible types of coroutine
        /// </summary>
        public enum CoroutineType
        {
            /// <summary>
            /// Legacy placeholder; prefer an explicit coroutine type.
            /// </summary>
            [Obsolete("Use a concrete CoroutineType value.", false)]
            Unknown = 0,

            /// <summary>
            /// A valid coroutine
            /// </summary>
            Coroutine = 1,

            /// <summary>
            /// A CLR callback assigned to a coroutine.
            /// </summary>
            ClrCallback = 2,

            /// <summary>
            /// A CLR callback assigned to a coroutine and already executed.
            /// </summary>
            ClrCallbackDead = 3,

            /// <summary>
            /// A recycled coroutine
            /// </summary>
            Recycled = 4,
        }

        /// <summary>
        /// Gets the type of coroutine
        /// </summary>
        public CoroutineType Type { get; private set; }

        private readonly CallbackFunction _clrCallback;
        private readonly Processor _processor;

        internal Coroutine(CallbackFunction function)
        {
            Type = CoroutineType.ClrCallback;
            _clrCallback = function;
            OwnerScript = null;
        }

        internal Coroutine(Processor proc)
        {
            Type = CoroutineType.Coroutine;
            _processor = proc;
            _processor.AssociatedCoroutine = this;
            OwnerScript = proc.GetScript();
        }

        /// <summary>
        /// Marks a CLR callback coroutine as completed so future resumes throw meaningful errors.
        /// </summary>
        internal void MarkClrCallbackAsDead()
        {
            if (Type != CoroutineType.ClrCallback)
            {
                throw new InvalidOperationException("State must be CoroutineType.ClrCallback");
            }

            Type = CoroutineType.ClrCallbackDead;
        }

        /// <summary>
        /// Reuses this coroutine's processor for a new closure, returning the recycled coroutine result.
        /// </summary>
        internal DynValue Recycle(Processor mainProcessor, Closure closure)
        {
            Type = CoroutineType.Recycled;
            return _processor.RecycleCoroutine(mainProcessor, closure);
        }

        /// <summary>
        /// Gets this coroutine as a typed enumerable which can be looped over for resuming.
        /// Returns its result as DynValue(s)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        public IEnumerable<DynValue> AsTypedEnumerable()
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            while (
                State == CoroutineState.NotStarted
                || State == CoroutineState.Suspended
                || State == CoroutineState.ForceSuspended
            )
            {
                yield return Resume();
            }
        }

        /// <summary>
        /// Gets this coroutine as a typed enumerable which can be looped over for resuming.
        /// Returns its result as System.Object. Only the first element of tuples is returned.
        /// Only non-CLR coroutines can be resumed with this method. Use an overload of the Resume method accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        public IEnumerable<object> AsEnumerable()
        {
            foreach (DynValue v in AsTypedEnumerable())
            {
                yield return v.ToScalar().ToObject();
            }
        }

        /// <summary>
        /// Gets this coroutine as a typed enumerable which can be looped over for resuming.
        /// Returns its result as the specified type. Only the first element of tuples is returned.
        /// Only non-CLR coroutines can be resumed with this method. Use an overload of the Resume method accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        public IEnumerable<T> AsEnumerable<T>()
        {
            foreach (DynValue v in AsTypedEnumerable())
            {
                yield return v.ToScalar().ToObject<T>();
            }
        }

        /// <summary>
        /// The purpose of this method is to convert a NovaSharp/Lua coroutine to a Unity3D coroutine.
        /// This loops over the coroutine, discarding returned values, and returning null for each invocation.
        /// This means however that the coroutine will be invoked each frame.
        /// Only non-CLR coroutines can be resumed with this method. Use an overload of the Resume method accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        public System.Collections.IEnumerator AsUnityCoroutine()
        {
            foreach (DynValue _ in AsTypedEnumerable())
            {
                yield return null;
            }
        }

        /// <summary>
        /// Resumes the coroutine.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        public DynValue Resume(params DynValue[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            this.CheckScriptOwnership(args);

            if (Type == CoroutineType.Coroutine)
            {
                return _processor.ResumeCoroutine(args);
            }
            else
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }
        }

        /// <summary>
        /// Resumes the coroutine.
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public DynValue Resume(ScriptExecutionContext context, params DynValue[] args)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            this.CheckScriptOwnership(context);
            this.CheckScriptOwnership(args);

            if (Type == CoroutineType.Coroutine)
            {
                return _processor.ResumeCoroutine(args);
            }
            else if (Type == CoroutineType.ClrCallback)
            {
                DynValue ret = _clrCallback.Invoke(context, args);
                MarkClrCallbackAsDead();
                return ret;
            }
            else
            {
                throw ScriptRuntimeException.CannotResumeNotSuspended(CoroutineState.Dead);
            }
        }

        /// <summary>
        /// Resumes the coroutine.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        public DynValue Resume()
        {
            return Resume(Array.Empty<DynValue>());
        }

        /// <summary>
        /// Resumes the coroutine.
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <returns></returns>
        public DynValue Resume(ScriptExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Resume(context, Array.Empty<DynValue>());
        }

        /// <summary>
        /// Resumes the coroutine.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public DynValue Resume(params object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            DynValue[] dargs = new DynValue[args.Length];

            for (int i = 0; i < dargs.Length; i++)
            {
                dargs[i] = DynValue.FromObject(OwnerScript, args[i]);
            }

            return Resume(dargs);
        }

        /// <summary>
        /// Resumes the coroutine
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public DynValue Resume(ScriptExecutionContext context, params object[] args)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            DynValue[] dargs = new DynValue[args.Length];

            for (int i = 0; i < dargs.Length; i++)
            {
                dargs[i] = DynValue.FromObject(context.Script, args[i]);
            }

            return Resume(context, dargs);
        }

        /// <summary>
        /// Gets the coroutine state.
        /// </summary>
        public CoroutineState State
        {
            get
            {
                if (Type == CoroutineType.ClrCallback)
                {
                    return CoroutineState.NotStarted;
                }
                else if (Type == CoroutineType.ClrCallbackDead)
                {
                    return CoroutineState.Dead;
                }
                else
                {
                    return _processor.State;
                }
            }
        }

        /// <summary>
        /// Gets the coroutine stack trace for debug purposes
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <param name="entrySourceRef">The entry source reference.</param>
        /// <returns></returns>
        public WatchItem[] GetStackTrace(int skip, SourceRef entrySourceRef = null)
        {
            if (State != CoroutineState.Running)
            {
                entrySourceRef = _processor.GetCoroutineSuspendedLocation();
            }

            List<WatchItem> stack = _processor.GetDebuggerCallStack(entrySourceRef);
            return stack.Skip(skip).ToArray();
        }

        /// <summary>
        /// Gets the script owning this resource.
        /// </summary>
        /// <value>
        /// The script owning this resource.
        /// </value>
        /// <exception cref="System.NotImplementedException"></exception>
        public Script OwnerScript { get; internal set; }

        /// <summary>
        /// Gets or sets the automatic yield counter.
        /// </summary>
        /// <value>
        /// The automatic yield counter.
        /// </value>
        public long AutoYieldCounter
        {
            get { return _processor.AutoYieldCounter; }
            set { _processor.AutoYieldCounter = value; }
        }

        /// <summary>
        /// Closes the coroutine by running the underlying processor's cleanup logic (Lua 5.4 close semantics).
        /// CLR callback coroutines no-op and return true to mirror Lua's behaviour for already-finished threads.
        /// </summary>
        public DynValue Close()
        {
            if (Type != CoroutineType.Coroutine)
            {
                return DynValue.True;
            }

            return _processor.CloseCoroutine();
        }

        /// <summary>
        /// Exposes the backing processor for unit tests that need to inspect VM state.
        /// Throws when invoked on CLR callback coroutines.
        /// </summary>
        internal Processor GetProcessorForTests()
        {
            if (_processor == null)
            {
                throw new InvalidOperationException(
                    "Cannot retrieve a processor from CLR callback coroutines."
                );
            }

            return _processor;
        }

        /// <summary>
        /// Forcibly overrides the coroutine state (test-only helper).
        /// </summary>
        internal void ForceStateForTests(CoroutineState state)
        {
            if (_processor == null)
            {
                throw new InvalidOperationException(
                    "Cannot override state on CLR callback coroutines."
                );
            }

            _processor.ForceStateForTests(state);
        }
    }
}
