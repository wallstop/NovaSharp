namespace WallstopStudios.NovaSharp.Interpreter
{
    using System;
    using System.Runtime.CompilerServices;
    using DataTypes;

    /// <summary>
    /// Represents a Lua/NovaSharp chunk or callable value that has already been resolved for a
    /// specific <see cref="Script"/> instance.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Script.CompileString"/>, <see cref="Script.CompileFunction"/>, or
    /// <see cref="Script.BindGlobalFunction"/> when a caller needs to execute the same callable
    /// repeatedly without keeping source text or global lookup on the hot path.
    /// </remarks>
    public readonly struct CompiledScript : IEquatable<CompiledScript>
    {
        private readonly Script _script;
        private readonly DynValue _function;

        internal CompiledScript(Script script, DynValue function)
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            _script = script;
            _function = function;
        }

        /// <summary>
        /// Gets the script instance that owns this callable handle.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this value is the default, uninitialized <see cref="CompiledScript"/>.
        /// </exception>
        public Script Script => GetScript();

        /// <summary>
        /// Gets the underlying callable value.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this value is the default, uninitialized <see cref="CompiledScript"/>.
        /// </exception>
        public DynValue Function => GetFunction();

        /// <summary>
        /// Gets a value indicating whether this handle was created by a <see cref="Script"/>
        /// compile or function binding method.
        /// </summary>
        public bool IsValid => _script != null && _function != null;

        /// <summary>
        /// Executes the compiled chunk with no arguments.
        /// </summary>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute()
        {
            return GetScript().ExecuteCompiledFunction(GetFunction());
        }

        /// <summary>
        /// Executes the compiled chunk with one argument.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(DynValue arg)
        {
            return GetScript().ExecuteCompiledFunction(GetFunction(), arg);
        }

        /// <summary>
        /// Executes the compiled chunk with two arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(DynValue arg1, DynValue arg2)
        {
            return GetScript().ExecuteCompiledFunction(GetFunction(), arg1, arg2);
        }

        /// <summary>
        /// Executes the compiled chunk with three arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(DynValue arg1, DynValue arg2, DynValue arg3)
        {
            return GetScript().ExecuteCompiledFunction(GetFunction(), arg1, arg2, arg3);
        }

        /// <summary>
        /// Executes the compiled chunk with four arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
        {
            return GetScript().ExecuteCompiledFunction(GetFunction(), arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Executes the compiled chunk with five arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            return GetScript().ExecuteCompiledFunction(GetFunction(), arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR object argument.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// For allocation-sensitive loops, prefer the <see cref="DynValue"/> overloads with cached
        /// argument values.
        /// </remarks>
        public DynValue Execute(object arg)
        {
            Script script = GetScript();
            DynValue function = GetFunction();
            return script.ExecuteCompiledFunction(function, DynValue.FromObject(script, arg));
        }

        /// <summary>
        /// Executes the compiled chunk with two CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// For allocation-sensitive loops, prefer the <see cref="DynValue"/> overloads with cached
        /// argument values.
        /// </remarks>
        public DynValue Execute(object arg1, object arg2)
        {
            Script script = GetScript();
            DynValue function = GetFunction();
            return script.ExecuteCompiledFunction(
                function,
                DynValue.FromObject(script, arg1),
                DynValue.FromObject(script, arg2)
            );
        }

        /// <summary>
        /// Executes the compiled chunk with three CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// For allocation-sensitive loops, prefer the <see cref="DynValue"/> overloads with cached
        /// argument values.
        /// </remarks>
        public DynValue Execute(object arg1, object arg2, object arg3)
        {
            Script script = GetScript();
            DynValue function = GetFunction();
            return script.ExecuteCompiledFunction(
                function,
                DynValue.FromObject(script, arg1),
                DynValue.FromObject(script, arg2),
                DynValue.FromObject(script, arg3)
            );
        }

        /// <summary>
        /// Executes the compiled chunk with four CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// For allocation-sensitive loops, prefer the <see cref="DynValue"/> overloads with cached
        /// argument values.
        /// </remarks>
        public DynValue Execute(object arg1, object arg2, object arg3, object arg4)
        {
            Script script = GetScript();
            DynValue function = GetFunction();
            return script.ExecuteCompiledFunction(
                function,
                DynValue.FromObject(script, arg1),
                DynValue.FromObject(script, arg2),
                DynValue.FromObject(script, arg3),
                DynValue.FromObject(script, arg4)
            );
        }

        /// <summary>
        /// Executes the compiled chunk with five CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// For allocation-sensitive loops, prefer the <see cref="DynValue"/> overloads with cached
        /// argument values.
        /// </remarks>
        public DynValue Execute(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            Script script = GetScript();
            DynValue function = GetFunction();
            return script.ExecuteCompiledFunction(
                function,
                DynValue.FromObject(script, arg1),
                DynValue.FromObject(script, arg2),
                DynValue.FromObject(script, arg3),
                DynValue.FromObject(script, arg4),
                DynValue.FromObject(script, arg5)
            );
        }

        /// <summary>
        /// Executes the compiled chunk with caller-owned array arguments.
        /// </summary>
        /// <param name="args">The arguments to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// The array is treated as the argument list. Use <see cref="DynValue.Nil"/> for a Lua nil
        /// argument, or cast <c>null</c> to <see cref="object"/> when using the CLR object overload.
        /// </remarks>
        public DynValue Execute(DynValue[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return Execute(args.AsSpan());
        }

        /// <summary>
        /// Executes the compiled chunk with caller-owned contiguous arguments.
        /// </summary>
        /// <param name="args">The arguments to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(ReadOnlySpan<DynValue> args)
        {
            return GetScript().ExecuteCompiledFunction(GetFunction(), args);
        }

        /// <inheritdoc />
        public bool Equals(CompiledScript other)
        {
            return ReferenceEquals(_script, other._script)
                && ReferenceEquals(_function, other._function);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is CompiledScript other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 17;
                hashCode =
                    (hashCode * 31) + (_script == null ? 0 : RuntimeHelpers.GetHashCode(_script));
                hashCode =
                    (hashCode * 31)
                    + (_function == null ? 0 : RuntimeHelpers.GetHashCode(_function));
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether two compiled handles reference the same script and function.
        /// </summary>
        public static bool operator ==(CompiledScript left, CompiledScript right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two compiled handles reference different scripts or functions.
        /// </summary>
        public static bool operator !=(CompiledScript left, CompiledScript right)
        {
            return !left.Equals(right);
        }

        private Script GetScript()
        {
            if (_script == null)
            {
                throw new InvalidOperationException(
                    "CompiledScript was not created by a Script compile or function binding method."
                );
            }

            return _script;
        }

        private DynValue GetFunction()
        {
            if (_function == null)
            {
                throw new InvalidOperationException(
                    "CompiledScript was not created by a Script compile or function binding method."
                );
            }

            return _function;
        }
    }
}
