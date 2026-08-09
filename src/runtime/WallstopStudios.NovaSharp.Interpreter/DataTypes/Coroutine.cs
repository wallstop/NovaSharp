namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// A class representing a script coroutine
    /// </summary>
    public class Coroutine : RefIdObject, IScriptPrivateResource
    {
        // Estimated base memory overhead for a Coroutine (object header, fields, Processor reference).
        // Conservative estimate: object header (16-24 bytes) + fields + Processor with stacks.
        // Processor contains value stack, execution stack, and various state.
        private const int BaseCoroutineOverhead = 512;

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
            OwnerScript = function.OwnerScript;
        }

        internal Coroutine(Processor proc)
        {
            Type = CoroutineType.Coroutine;
            _processor = proc;
            _processor.AssociatedCoroutine = this;
            OwnerScript = proc.GetScript();

            // Only track user-created coroutines, not the main processor's pseudo-coroutine
            if (proc.State != CoroutineState.Main)
            {
                OwnerScript?.RegisterCoroutineForMemoryStatistics(this);
                TrackAllocation(OwnerScript);
            }
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
        internal LuaValue Recycle(Processor mainProcessor, Closure closure)
        {
            Type = CoroutineType.Recycled;
            return _processor.RecycleCoroutine(mainProcessor, closure);
        }

        /// <summary>
        /// Gets this coroutine as a typed enumerable which can be looped over for resuming.
        /// Returns its result as LuaValue(s)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        public IEnumerable<LuaValue> AsTypedEnumerable()
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
            foreach (LuaValue v in AsTypedEnumerable())
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
            foreach (LuaValue v in AsTypedEnumerable())
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
            foreach (LuaValue _ in AsTypedEnumerable())
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
        public LuaValue Resume(LuaValue[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return ResumeValues(args.AsSpan());
        }

        /// <summary>
        /// Resumes the coroutine with caller-owned contiguous arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        public LuaValue Resume(ReadOnlySpan<LuaValue> args)
        {
            return ResumeValues(args);
        }

        internal LuaValue ResumeValues(ReadOnlySpan<LuaValue> args)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            this.CheckScriptOwnership(args);
            return _processor.ResumeCoroutine(args);
        }

        /// <summary>
        /// Resumes the coroutine with one argument.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        internal LuaValue ResumeValues(LuaValue arg)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            this.CheckScriptOwnership(arg);
            return _processor.ResumeCoroutine(arg);
        }

        /// <summary>
        /// Resumes the coroutine with two arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        internal LuaValue ResumeValues(LuaValue arg1, LuaValue arg2)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            return _processor.ResumeCoroutine(arg1, arg2);
        }

        /// <summary>
        /// Resumes the coroutine with three arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        internal LuaValue ResumeValues(LuaValue arg1, LuaValue arg2, LuaValue arg3)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            return _processor.ResumeCoroutine(arg1, arg2, arg3);
        }

        /// <summary>
        /// Resumes the coroutine with four arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        internal LuaValue ResumeValues(LuaValue arg1, LuaValue arg2, LuaValue arg3, LuaValue arg4)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            return _processor.ResumeCoroutine(arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Resumes the coroutine with five arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        internal LuaValue ResumeValues(
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5
        )
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            return _processor.ResumeCoroutine(arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// Resumes the coroutine with six arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <param name="arg6">The sixth argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        internal LuaValue ResumeValues(
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6
        )
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            this.CheckScriptOwnership(arg6);
            return _processor.ResumeCoroutine(arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// Resumes the coroutine with seven arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <param name="arg6">The sixth argument.</param>
        /// <param name="arg7">The seventh argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead</exception>
        internal LuaValue ResumeValues(
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7
        )
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            this.CheckScriptOwnership(arg6);
            this.CheckScriptOwnership(arg7);
            return _processor.ResumeCoroutine(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// Resumes the coroutine.
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public LuaValue Resume(ScriptExecutionContext context, LuaValue[] args)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return ResumeValues(context, args.AsSpan());
        }

        /// <summary>
        /// Resumes the coroutine with caller-owned contiguous arguments.
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public LuaValue Resume(ScriptExecutionContext context, ReadOnlySpan<LuaValue> args)
        {
            return ResumeValues(context, args);
        }

        internal LuaValue ResumeValues(ScriptExecutionContext context, ReadOnlySpan<LuaValue> args)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            this.CheckScriptOwnership(context);
            this.CheckScriptOwnership(args);

            if (Type == CoroutineType.Coroutine)
            {
                return _processor.ResumeCoroutine(args);
            }
            else if (Type == CoroutineType.ClrCallback)
            {
                LuaValue ret = _clrCallback.HasArgumentViewCallback
                    ? _clrCallback.InvokeArgumentViewSpan(context, args)
                    : _clrCallback.InvokeLegacySpan(context, args);
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
        public LuaValue Resume()
        {
            return ResumeValues(Array.Empty<LuaValue>());
        }

        /// <summary>
        /// Resumes the coroutine.
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <returns></returns>
        public LuaValue Resume(ScriptExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return ResumeValues(context, Array.Empty<LuaValue>());
        }

        /// <summary>
        /// Resumes the coroutine.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue Resume(params object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return ResumeObjectArguments(args.AsSpan());
        }

        /// <summary>
        /// Resumes the coroutine with caller-owned CLR object argument storage.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue ResumeObjectArguments(object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return ResumeObjectArguments(args.AsSpan());
        }

        /// <summary>
        /// Resumes the coroutine with caller-owned contiguous CLR object arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue ResumeObjectArguments(ReadOnlySpan<object> args)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            return ResumeConvertedObjectArguments(OwnerScript, args);
        }

        private LuaValue ResumeConvertedObjectArguments(Script script, ReadOnlySpan<object> args)
        {
            switch (args.Length)
            {
                case 0:
                    return Resume();
                case 1:
                    return ResumeValues(LuaValue.FromObject(script, args[0]));
                case 2:
                    return ResumeValues(
                        LuaValue.FromObject(script, args[0]),
                        LuaValue.FromObject(script, args[1])
                    );
                case 3:
                    return ResumeValues(
                        LuaValue.FromObject(script, args[0]),
                        LuaValue.FromObject(script, args[1]),
                        LuaValue.FromObject(script, args[2])
                    );
                case 4:
                    return ResumeValues(
                        LuaValue.FromObject(script, args[0]),
                        LuaValue.FromObject(script, args[1]),
                        LuaValue.FromObject(script, args[2]),
                        LuaValue.FromObject(script, args[3])
                    );
                case 5:
                    return ResumeValues(
                        LuaValue.FromObject(script, args[0]),
                        LuaValue.FromObject(script, args[1]),
                        LuaValue.FromObject(script, args[2]),
                        LuaValue.FromObject(script, args[3]),
                        LuaValue.FromObject(script, args[4])
                    );
                case 6:
                    return ResumeValues(
                        LuaValue.FromObject(script, args[0]),
                        LuaValue.FromObject(script, args[1]),
                        LuaValue.FromObject(script, args[2]),
                        LuaValue.FromObject(script, args[3]),
                        LuaValue.FromObject(script, args[4]),
                        LuaValue.FromObject(script, args[5])
                    );
                case 7:
                    return ResumeValues(
                        LuaValue.FromObject(script, args[0]),
                        LuaValue.FromObject(script, args[1]),
                        LuaValue.FromObject(script, args[2]),
                        LuaValue.FromObject(script, args[3]),
                        LuaValue.FromObject(script, args[4]),
                        LuaValue.FromObject(script, args[5]),
                        LuaValue.FromObject(script, args[6])
                    );
            }

            using PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(
                args.Length,
                out LuaValue[] convertedArgs
            );
            for (int i = 0; i < args.Length; i++)
            {
                convertedArgs[i] = LuaValue.FromObject(script, args[i]);
            }

            return ResumeValues(convertedArgs.AsSpan(0, args.Length));
        }

        /// <summary>
        /// Resumes the coroutine with one CLR object argument.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue Resume(object arg)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            return ResumeValues(LuaValue.FromObject(OwnerScript, arg));
        }

        /// <summary>
        /// Resumes the coroutine with two CLR object arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue Resume(object arg1, object arg2)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            return ResumeValues(
                LuaValue.FromObject(OwnerScript, arg1),
                LuaValue.FromObject(OwnerScript, arg2)
            );
        }

        /// <summary>
        /// Resumes the coroutine with three CLR object arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue Resume(object arg1, object arg2, object arg3)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            return ResumeValues(
                LuaValue.FromObject(OwnerScript, arg1),
                LuaValue.FromObject(OwnerScript, arg2),
                LuaValue.FromObject(OwnerScript, arg3)
            );
        }

        /// <summary>
        /// Resumes the coroutine with four CLR object arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue Resume(object arg1, object arg2, object arg3, object arg4)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            return ResumeValues(
                LuaValue.FromObject(OwnerScript, arg1),
                LuaValue.FromObject(OwnerScript, arg2),
                LuaValue.FromObject(OwnerScript, arg3),
                LuaValue.FromObject(OwnerScript, arg4)
            );
        }

        /// <summary>
        /// Resumes the coroutine with five CLR object arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue Resume(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            return ResumeValues(
                LuaValue.FromObject(OwnerScript, arg1),
                LuaValue.FromObject(OwnerScript, arg2),
                LuaValue.FromObject(OwnerScript, arg3),
                LuaValue.FromObject(OwnerScript, arg4),
                LuaValue.FromObject(OwnerScript, arg5)
            );
        }

        /// <summary>
        /// Resumes the coroutine with six CLR object arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <param name="arg6">The sixth argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue Resume(
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6
        )
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            return ResumeValues(
                LuaValue.FromObject(OwnerScript, arg1),
                LuaValue.FromObject(OwnerScript, arg2),
                LuaValue.FromObject(OwnerScript, arg3),
                LuaValue.FromObject(OwnerScript, arg4),
                LuaValue.FromObject(OwnerScript, arg5),
                LuaValue.FromObject(OwnerScript, arg6)
            );
        }

        /// <summary>
        /// Resumes the coroutine with seven CLR object arguments.
        /// Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.
        /// </summary>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <param name="arg6">The sixth argument.</param>
        /// <param name="arg7">The seventh argument.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead.</exception>
        public LuaValue Resume(
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6,
            object arg7
        )
        {
            if (Type != CoroutineType.Coroutine)
            {
                throw new InvalidOperationException(
                    "Only non-CLR coroutines can be resumed with this overload of the Resume method. Use the overload accepting a ScriptExecutionContext instead"
                );
            }

            return ResumeValues(
                LuaValue.FromObject(OwnerScript, arg1),
                LuaValue.FromObject(OwnerScript, arg2),
                LuaValue.FromObject(OwnerScript, arg3),
                LuaValue.FromObject(OwnerScript, arg4),
                LuaValue.FromObject(OwnerScript, arg5),
                LuaValue.FromObject(OwnerScript, arg6),
                LuaValue.FromObject(OwnerScript, arg7)
            );
        }

        /// <summary>
        /// Resumes the coroutine
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public LuaValue Resume(ScriptExecutionContext context, params object[] args)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return ResumeObjectArguments(context, args.AsSpan());
        }

        /// <summary>
        /// Resumes the coroutine with caller-owned CLR object argument storage.
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public LuaValue ResumeObjectArguments(ScriptExecutionContext context, object[] args)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return ResumeObjectArguments(context, args.AsSpan());
        }

        /// <summary>
        /// Resumes the coroutine with caller-owned contiguous CLR object arguments.
        /// </summary>
        /// <param name="context">The ScriptExecutionContext.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public LuaValue ResumeObjectArguments(
            ScriptExecutionContext context,
            ReadOnlySpan<object> args
        )
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            this.CheckScriptOwnership(context);

            if (Type == CoroutineType.Coroutine)
            {
                return ResumeConvertedObjectArguments(context.Script, args);
            }

            using PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(
                args.Length,
                out LuaValue[] convertedArgs
            );
            for (int i = 0; i < args.Length; i++)
            {
                convertedArgs[i] = LuaValue.FromObject(context.Script, args[i]);
            }

            return ResumeValues(context, convertedArgs.AsSpan(0, args.Length));
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

            if (skip <= 0)
            {
                return stack.ToArray();
            }

            if (skip >= stack.Count)
            {
                return Array.Empty<WatchItem>();
            }

            int resultCount = stack.Count - skip;
            WatchItem[] result = new WatchItem[resultCount];
            for (int i = 0; i < resultCount; i++)
            {
                result[i] = stack[skip + i];
            }
            return result;
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
        public LuaValue Close()
        {
            if (Type != CoroutineType.Coroutine)
            {
                return LuaValue.True;
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
        /// Gets approximate retained bytes for this coroutine's backing VM stacks.
        /// </summary>
        internal long GetEstimatedRetainedBytesForMemoryStatistics()
        {
            if (Type != CoroutineType.Coroutine || _processor == null)
            {
                return 0L;
            }

            return BaseCoroutineOverhead
                + _processor.GetEstimatedRetainedStackBytesForMemoryStatistics();
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

        /// <summary>
        /// Records allocation with the owning script's tracker if memory tracking is enabled.
        /// Also records coroutine creation count.
        /// </summary>
        private static void TrackAllocation(Script script)
        {
            AllocationTracker tracker = script?.AllocationTracker;
            if (tracker != null)
            {
                tracker.RecordAllocation(BaseCoroutineOverhead);
                tracker.RecordCoroutineCreated();
            }
        }
    }
}
