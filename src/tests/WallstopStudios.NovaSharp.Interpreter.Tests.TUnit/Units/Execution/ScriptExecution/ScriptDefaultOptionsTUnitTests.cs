namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [ScriptGlobalOptionsIsolation]
    public sealed class ScriptDefaultOptionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DefaultOptionsScriptLoaderPersistsAcrossNewScripts()
        {
            TrackingScriptLoader loader = new();
            using ScriptDefaultOptionsScope scope = ScriptDefaultOptionsScope.OverrideScriptLoader(
                loader
            );

            Script script = new();

            await Assert
                .That(ReferenceEquals(script.Options.ScriptLoader, loader))
                .IsTrue()
                .ConfigureAwait(false);
        }

        private sealed class TrackingScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                return "return";
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
