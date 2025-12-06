namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;

    public sealed class ScriptLoadFileTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task LoadFileThrowsWhenScriptLoaderReturnsNull()
        {
            ScriptOptions options = new() { ScriptLoader = new StubScriptLoader(null) };
            Script script = new(CoreModules.Basic, options);

            InvalidCastException exception = ExpectException<InvalidCastException>(() =>
                script.LoadFile("test.lua")
            );

            await Assert
                .That(exception.Message)
                .Contains("Unexpected null from IScriptLoader.LoadFile");
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileThrowsWhenScriptLoaderReturnsUnsupportedType()
        {
            object unexpected = new();
            ScriptOptions options = new() { ScriptLoader = new StubScriptLoader(unexpected) };
            Script script = new(CoreModules.Basic, options);

            InvalidCastException exception = ExpectException<InvalidCastException>(() =>
                script.LoadFile("test.lua")
            );

            await Assert.That(exception.Message).Contains("Unsupported return type");
            await Assert.That(exception.Message).Contains(unexpected.GetType().Name);
        }

        private static TException ExpectException<TException>(Func<DynValue> action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }

        private sealed class StubScriptLoader : IScriptLoader
        {
            private readonly object _returnValue;

            public StubScriptLoader(object returnValue)
            {
                _returnValue = returnValue;
            }

            public object LoadFile(string file, Table globalContext)
            {
                return _returnValue;
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }
    }
}
