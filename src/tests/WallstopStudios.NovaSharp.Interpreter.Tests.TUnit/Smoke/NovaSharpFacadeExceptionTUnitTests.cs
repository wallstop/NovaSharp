namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Smoke
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    [SuppressMessage(
        "Performance",
        "CA1849:Call async methods when in an async method",
        Justification = "These smoke tests intentionally exercise sync facade exception contracts."
    )]
    public sealed class NovaSharpFacadeExceptionTUnitTests
    {
        [Test]
        public async Task RunTranslatesSyntaxErrors()
        {
            using LuaEngine lua = LuaEngine.Create();

            LuaSyntaxException exception = Assert.Throws<LuaSyntaxException>(() =>
                lua.Run("if true then", "syntax_case")
            );

            await Assert
                .That(exception.InnerException)
                .IsTypeOf<SyntaxErrorException>()
                .ConfigureAwait(false);
            await Assert.That(exception.IsIncompleteInput).IsTrue().ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("syntax_case").ConfigureAwait(false);
            await Assert
                .That(exception.DecoratedMessage)
                .Contains("syntax_case")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task CompileTranslatesSyntaxErrors()
        {
            using LuaEngine lua = LuaEngine.Create();

            LuaSyntaxException exception = Assert.Throws<LuaSyntaxException>(() =>
                lua.Compile("return function(", "compile_case")
            );

            await Assert
                .That(exception.InnerException)
                .IsTypeOf<SyntaxErrorException>()
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("compile_case").ConfigureAwait(false);
        }

        [Test]
        public async Task RunTranslatesRuntimeErrors()
        {
            using LuaEngine lua = LuaEngine.Create();

            LuaRuntimeException exception = Assert.Throws<LuaRuntimeException>(() =>
                lua.Run("error('runtime failure')", "runtime_case")
            );

            await Assert
                .That(exception.InnerException)
                .IsTypeOf<ScriptRuntimeException>()
                .ConfigureAwait(false);
            await Assert
                .That(exception.InnerException is LuaException)
                .IsFalse()
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("runtime failure").ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("runtime_case").ConfigureAwait(false);
        }

        [Test]
        public async Task LuaFunctionCallTranslatesRuntimeErrorsWithoutDoubleWrapping()
        {
            using LuaEngine lua = LuaEngine.Create();
            LuaFunction function = lua.Run("return function() error('call failure') end")
                .AsFunction();

            LuaRuntimeException exception = Assert.Throws<LuaRuntimeException>(() =>
                function.Call()
            );

            await Assert
                .That(exception.InnerException)
                .IsTypeOf<ScriptRuntimeException>()
                .ConfigureAwait(false);
            await Assert
                .That(exception.InnerException is LuaException)
                .IsFalse()
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("call failure").ConfigureAwait(false);
        }

        [Test]
        public async Task LuaChunkRunTranslatesRuntimeErrors()
        {
            using LuaEngine lua = LuaEngine.Create();
            LuaChunk chunk = lua.Compile("return missing_global + 1", "chunk_case");

            LuaRuntimeException exception = Assert.Throws<LuaRuntimeException>(() => chunk.Run());

            await Assert
                .That(exception.InnerException)
                .IsTypeOf<ScriptRuntimeException>()
                .ConfigureAwait(false);
            await Assert.That(exception.Message).Contains("chunk_case").ConfigureAwait(false);
        }

        [Test]
        public async Task LuaCoroutineResumeTranslatesRuntimeErrors()
        {
            using LuaEngine lua = LuaEngine.Create();
            LuaFunction function = lua.Run("return function() error('coroutine failure') end")
                .AsFunction();
            LuaCoroutine coroutine = lua.CreateCoroutine(function);

            LuaRuntimeException exception = Assert.Throws<LuaRuntimeException>(() =>
                coroutine.Resume()
            );

            await Assert
                .That(exception.InnerException)
                .IsTypeOf<ScriptRuntimeException>()
                .ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("coroutine failure")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task RestrictedFunctionTranslatesSandboxViolationDetails()
        {
            LuaEngineOptions options = LuaEngineOptions.Default;
            options.Sandbox = LuaSandboxOptions.Unrestricted.RestrictFunction("load");
            using LuaEngine lua = LuaEngine.Create(options);

            LuaSandboxException exception = Assert.Throws<LuaSandboxException>(() =>
                lua.Run("return load('return 42')", "sandbox_function_case")
            );

            await Assert
                .That(exception.InnerException)
                .IsTypeOf<SandboxViolationException>()
                .ConfigureAwait(false);
            await Assert
                .That(exception.ViolationKind)
                .IsEqualTo(LuaSandboxViolationKind.FunctionAccessDenied)
                .ConfigureAwait(false);
            await Assert.That(exception.IsAccessDenied).IsTrue().ConfigureAwait(false);
            await Assert.That(exception.IsLimitViolation).IsFalse().ConfigureAwait(false);
            await Assert.That(exception.DeniedAccessName).IsEqualTo("load").ConfigureAwait(false);
        }

        [Test]
        public async Task InstructionLimitTranslatesSandboxViolationDetails()
        {
            LuaEngineOptions options = LuaEngineOptions.Default;
            options.Sandbox = new LuaSandboxOptions { MaxInstructions = 10 };
            using LuaEngine lua = LuaEngine.Create(options);

            LuaSandboxException exception = Assert.Throws<LuaSandboxException>(() =>
                lua.Run("while true do end", "instruction_limit_case")
            );

            await Assert
                .That(exception.ViolationKind)
                .IsEqualTo(LuaSandboxViolationKind.InstructionLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(exception.IsLimitViolation).IsTrue().ConfigureAwait(false);
            await Assert.That(exception.IsAccessDenied).IsFalse().ConfigureAwait(false);
            await Assert.That(exception.ConfiguredLimit).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(exception.ActualValue).IsGreaterThan(10).ConfigureAwait(false);
        }

        [Test]
        public async Task CoroutineLimitTranslatesSandboxViolationDetails()
        {
            LuaEngineOptions options = LuaEngineOptions.Default;
            options.Sandbox = new LuaSandboxOptions { MaxCoroutines = 1 };
            using LuaEngine lua = LuaEngine.Create(options);

            lua.Run("first = coroutine.create(function() end)");
            LuaSandboxException exception = Assert.Throws<LuaSandboxException>(() =>
                lua.Run("second = coroutine.create(function() end)")
            );

            await Assert
                .That(exception.ViolationKind)
                .IsEqualTo(LuaSandboxViolationKind.CoroutineLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(exception.ConfiguredLimit).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(exception.ActualValue).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task RunAsyncCancellationIsNotWrapped()
        {
            using LuaEngine lua = LuaEngine.Create();
            CancellationToken cancellationToken = new CancellationToken(canceled: true);

            OperationCanceledException exception = await Assert
                .ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await lua.RunAsync("return 42", cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        public async Task FacadeMisuseIsNotWrapped()
        {
            using LuaEngine lua = LuaEngine.Create();

            await Assert
                .That(() => lua.Call(null))
                .Throws<ArgumentNullException>()
                .ConfigureAwait(false);
        }
    }
}
