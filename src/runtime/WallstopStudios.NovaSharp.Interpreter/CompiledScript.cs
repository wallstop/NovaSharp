namespace WallstopStudios.NovaSharp.Interpreter
{
    using System;
    using System.Runtime.CompilerServices;
    using DataTypes;
    using Errors;

    /// <summary>
    /// Represents a Lua/NovaSharp chunk or callable value that has already been resolved for a
    /// specific <see cref="Script"/> instance.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Script.PrepareString"/>, <see cref="Script.PrepareStream"/>,
    /// <see cref="Script.PrepareFile"/>, <see cref="Script.PrepareFunction"/>,
    /// <see cref="Script.PrepareCallable"/>, or <see cref="Script.PrepareGlobalFunction"/>
    /// when a caller needs to execute the same callable repeatedly without keeping source text,
    /// streams, files, or global lookup on the hot path.
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

            script.ValidateCompiledScriptTarget(function);

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
            return GetScript().ExecuteTrustedCompiledFunction(_function);
        }

        /// <summary>
        /// Executes the compiled chunk with one argument.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(DynValue arg)
        {
            return GetScript().ExecuteTrustedCompiledFunction(_function, arg);
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR double argument without routing through the
        /// boxed <see cref="object"/> conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(double arg)
        {
            return Execute(DynValue.FromNumber(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR float argument without routing through the
        /// boxed <see cref="object"/> conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(float arg)
        {
            return Execute(DynValue.FromNumber(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR integer argument without routing through the
        /// boxed <see cref="object"/> conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(int arg)
        {
            return Execute(DynValue.FromInteger(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR integer argument without routing through the
        /// boxed <see cref="object"/> conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(long arg)
        {
            return Execute(DynValue.FromInteger(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR Boolean argument without routing through the
        /// boxed <see cref="object"/> conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(bool arg)
        {
            return Execute(DynValue.FromBoolean(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with two arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(DynValue arg1, DynValue arg2)
        {
            return GetScript().ExecuteTrustedCompiledFunction(_function, arg1, arg2);
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
            return GetScript().ExecuteTrustedCompiledFunction(_function, arg1, arg2, arg3);
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
            return GetScript().ExecuteTrustedCompiledFunction(_function, arg1, arg2, arg3, arg4);
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
            return GetScript()
                .ExecuteTrustedCompiledFunction(_function, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// Executes the compiled chunk with six arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            return GetScript()
                .ExecuteTrustedCompiledFunction(_function, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// Executes the compiled chunk with seven arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <param name="arg7">The seventh argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            return GetScript()
                .ExecuteTrustedCompiledFunction(
                    _function,
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6,
                    arg7
                );
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
            return script.ExecuteTrustedCompiledFunction(
                _function,
                DynValue.FromObject(script, arg)
            );
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
            return script.ExecuteTrustedCompiledFunction(
                _function,
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
            return script.ExecuteTrustedCompiledFunction(
                _function,
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
            return script.ExecuteTrustedCompiledFunction(
                _function,
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
            return script.ExecuteTrustedCompiledFunction(
                _function,
                DynValue.FromObject(script, arg1),
                DynValue.FromObject(script, arg2),
                DynValue.FromObject(script, arg3),
                DynValue.FromObject(script, arg4),
                DynValue.FromObject(script, arg5)
            );
        }

        /// <summary>
        /// Executes the compiled chunk with six CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// For allocation-sensitive loops, prefer the <see cref="DynValue"/> overloads with cached
        /// argument values.
        /// </remarks>
        public DynValue Execute(
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6
        )
        {
            Script script = GetScript();
            return script.ExecuteTrustedCompiledFunction(
                _function,
                DynValue.FromObject(script, arg1),
                DynValue.FromObject(script, arg2),
                DynValue.FromObject(script, arg3),
                DynValue.FromObject(script, arg4),
                DynValue.FromObject(script, arg5),
                DynValue.FromObject(script, arg6)
            );
        }

        /// <summary>
        /// Executes the compiled chunk with seven CLR object arguments.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <param name="arg7">The seventh argument to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// For allocation-sensitive loops, prefer the <see cref="DynValue"/> overloads with cached
        /// argument values.
        /// </remarks>
        public DynValue Execute(
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6,
            object arg7
        )
        {
            Script script = GetScript();
            return script.ExecuteTrustedCompiledFunction(
                _function,
                DynValue.FromObject(script, arg1),
                DynValue.FromObject(script, arg2),
                DynValue.FromObject(script, arg3),
                DynValue.FromObject(script, arg4),
                DynValue.FromObject(script, arg5),
                DynValue.FromObject(script, arg6),
                DynValue.FromObject(script, arg7)
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
        /// Executes the compiled chunk with caller-owned CLR object argument storage.
        /// </summary>
        /// <param name="args">The arguments to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// The array is treated as the argument list. To pass an <see cref="object"/> array as
        /// one Lua argument, use the fixed <see cref="Execute(object)"/> overload and cast the
        /// array to <see cref="object"/>.
        /// </remarks>
        public DynValue ExecuteObjectArguments(object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return ExecuteObjectArguments(args.AsSpan());
        }

        /// <summary>
        /// Executes the compiled chunk with caller-owned contiguous CLR object arguments.
        /// </summary>
        /// <param name="args">The arguments to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        /// <remarks>
        /// The span is treated as the argument list. To pass an <see cref="object"/> array as
        /// one Lua argument, use the fixed <see cref="Execute(object)"/> overload and cast the
        /// array to <see cref="object"/>.
        /// </remarks>
        public DynValue ExecuteObjectArguments(ReadOnlySpan<object> args)
        {
            return GetScript().ExecuteTrustedCompiledFunction(_function, args);
        }

        /// <summary>
        /// Executes the compiled chunk with caller-owned contiguous arguments.
        /// </summary>
        /// <param name="args">The arguments to pass to the chunk.</param>
        /// <returns>The return value(s) of the chunk.</returns>
        public DynValue Execute(ReadOnlySpan<DynValue> args)
        {
            return GetScript().ExecuteTrustedCompiledFunction(_function, args);
        }

        /// <summary>
        /// Executes the compiled chunk and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <returns>The converted first scalar result.</returns>
        /// <remarks>
        /// Use <see cref="Execute()"/> when tuple-preserving multi-result behavior is required.
        /// </remarks>
        public T ExecuteAs<T>()
        {
            return ConvertScalarResult<T>(Execute());
        }

        /// <summary>
        /// Executes the compiled chunk with one argument and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The converted first scalar result.</returns>
        public T ExecuteAs<T>(DynValue arg)
        {
            return ConvertScalarResult<T>(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with two arguments and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <returns>The converted first scalar result.</returns>
        public T ExecuteAs<T>(DynValue arg1, DynValue arg2)
        {
            return ConvertScalarResult<T>(Execute(arg1, arg2));
        }

        /// <summary>
        /// Executes the compiled chunk with three arguments and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <returns>The converted first scalar result.</returns>
        public T ExecuteAs<T>(DynValue arg1, DynValue arg2, DynValue arg3)
        {
            return ConvertScalarResult<T>(Execute(arg1, arg2, arg3));
        }

        /// <summary>
        /// Executes the compiled chunk with four arguments and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <returns>The converted first scalar result.</returns>
        public T ExecuteAs<T>(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
        {
            return ConvertScalarResult<T>(Execute(arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Executes the compiled chunk with five arguments and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <returns>The converted first scalar result.</returns>
        public T ExecuteAs<T>(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            return ConvertScalarResult<T>(Execute(arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Executes the compiled chunk with six arguments and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <returns>The converted first scalar result.</returns>
        public T ExecuteAs<T>(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            return ConvertScalarResult<T>(Execute(arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Executes the compiled chunk with seven arguments and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <param name="arg7">The seventh argument to pass to the chunk.</param>
        /// <returns>The converted first scalar result.</returns>
        public T ExecuteAs<T>(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            return ConvertScalarResult<T>(Execute(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// Executes the compiled chunk with caller-owned contiguous arguments and converts the first scalar result to the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The CLR result type.</typeparam>
        /// <param name="args">The arguments to pass to the chunk.</param>
        /// <returns>The converted first scalar result.</returns>
        public T ExecuteAs<T>(ReadOnlySpan<DynValue> args)
        {
            return ConvertScalarResult<T>(Execute(args));
        }

        /// <summary>
        /// Executes the compiled chunk and returns a strict numeric scalar result.
        /// </summary>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber()
        {
            return ConvertNumberResult(Execute());
        }

        /// <summary>
        /// Executes the compiled chunk with one argument and returns a strict numeric scalar result.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(DynValue arg)
        {
            return ConvertNumberResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR double argument and returns a strict numeric
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(double arg)
        {
            return ConvertNumberResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR float argument and returns a strict numeric
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(float arg)
        {
            return ConvertNumberResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR integer argument and returns a strict numeric
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(int arg)
        {
            return ConvertNumberResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR integer argument and returns a strict numeric
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(long arg)
        {
            return ConvertNumberResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR Boolean argument and returns a strict numeric
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(bool arg)
        {
            return ConvertNumberResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with two arguments and returns a strict numeric scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(DynValue arg1, DynValue arg2)
        {
            return ConvertNumberResult(Execute(arg1, arg2));
        }

        /// <summary>
        /// Executes the compiled chunk with three arguments and returns a strict numeric scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(DynValue arg1, DynValue arg2, DynValue arg3)
        {
            return ConvertNumberResult(Execute(arg1, arg2, arg3));
        }

        /// <summary>
        /// Executes the compiled chunk with four arguments and returns a strict numeric scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
        {
            return ConvertNumberResult(Execute(arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Executes the compiled chunk with five arguments and returns a strict numeric scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            return ConvertNumberResult(Execute(arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Executes the compiled chunk with six arguments and returns a strict numeric scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            return ConvertNumberResult(Execute(arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Executes the compiled chunk with seven arguments and returns a strict numeric scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <param name="arg7">The seventh argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            return ConvertNumberResult(Execute(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// Executes the compiled chunk with caller-owned contiguous arguments and returns a strict numeric scalar result.
        /// </summary>
        /// <param name="args">The arguments to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR double.</returns>
        public double ExecuteNumber(ReadOnlySpan<DynValue> args)
        {
            return ConvertNumberResult(Execute(args));
        }

        /// <summary>
        /// Executes the compiled chunk and returns a strict Boolean scalar result.
        /// </summary>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean()
        {
            return ConvertBooleanResult(Execute());
        }

        /// <summary>
        /// Executes the compiled chunk with one argument and returns a strict Boolean scalar result.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(DynValue arg)
        {
            return ConvertBooleanResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR double argument and returns a strict Boolean
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(double arg)
        {
            return ConvertBooleanResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR float argument and returns a strict Boolean
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(float arg)
        {
            return ConvertBooleanResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR integer argument and returns a strict Boolean
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(int arg)
        {
            return ConvertBooleanResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR integer argument and returns a strict Boolean
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(long arg)
        {
            return ConvertBooleanResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with one CLR Boolean argument and returns a strict Boolean
        /// scalar result without routing the argument through the boxed <see cref="object"/>
        /// conversion overload.
        /// </summary>
        /// <param name="arg">The argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(bool arg)
        {
            return ConvertBooleanResult(Execute(arg));
        }

        /// <summary>
        /// Executes the compiled chunk with two arguments and returns a strict Boolean scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(DynValue arg1, DynValue arg2)
        {
            return ConvertBooleanResult(Execute(arg1, arg2));
        }

        /// <summary>
        /// Executes the compiled chunk with three arguments and returns a strict Boolean scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(DynValue arg1, DynValue arg2, DynValue arg3)
        {
            return ConvertBooleanResult(Execute(arg1, arg2, arg3));
        }

        /// <summary>
        /// Executes the compiled chunk with four arguments and returns a strict Boolean scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
        {
            return ConvertBooleanResult(Execute(arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Executes the compiled chunk with five arguments and returns a strict Boolean scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            return ConvertBooleanResult(Execute(arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Executes the compiled chunk with six arguments and returns a strict Boolean scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            return ConvertBooleanResult(Execute(arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Executes the compiled chunk with seven arguments and returns a strict Boolean scalar result.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the chunk.</param>
        /// <param name="arg2">The second argument to pass to the chunk.</param>
        /// <param name="arg3">The third argument to pass to the chunk.</param>
        /// <param name="arg4">The fourth argument to pass to the chunk.</param>
        /// <param name="arg5">The fifth argument to pass to the chunk.</param>
        /// <param name="arg6">The sixth argument to pass to the chunk.</param>
        /// <param name="arg7">The seventh argument to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            return ConvertBooleanResult(Execute(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// Executes the compiled chunk with caller-owned contiguous arguments and returns a strict Boolean scalar result.
        /// </summary>
        /// <param name="args">The arguments to pass to the chunk.</param>
        /// <returns>The first scalar result as a CLR Boolean.</returns>
        public bool ExecuteBoolean(ReadOnlySpan<DynValue> args)
        {
            return ConvertBooleanResult(Execute(args));
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

        private static T ConvertScalarResult<T>(DynValue result)
        {
            return result.ToScalar().ToObject<T>();
        }

        private static double ConvertNumberResult(DynValue result)
        {
            DynValue scalar = result.ToScalar();
            if (scalar.Type != DataType.Number)
            {
                throw ScriptRuntimeException.ConvertObjectFailed(scalar.Type, typeof(double));
            }

            return scalar.Number;
        }

        private static bool ConvertBooleanResult(DynValue result)
        {
            DynValue scalar = result.ToScalar();
            if (scalar.Type != DataType.Boolean)
            {
                throw ScriptRuntimeException.ConvertObjectFailed(scalar.Type, typeof(bool));
            }

            return scalar.Boolean;
        }
    }
}
