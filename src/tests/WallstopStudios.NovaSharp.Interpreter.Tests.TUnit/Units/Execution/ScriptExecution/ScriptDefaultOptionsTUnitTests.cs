namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for <see cref="Script.DefaultOptions"/> behavior.
    /// These tests require isolation because they modify static state that could
    /// affect other parallel tests.
    /// </summary>
    [ScriptGlobalOptionsIsolation]
    [ScriptDefaultOptionsIsolation]
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

        /// <summary>
        /// Verifies that after the scope is disposed, the original loader is restored.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task DefaultOptionsScopeRestoresOriginalLoader()
        {
            IScriptLoader originalLoader = Script.DefaultOptions.ScriptLoader;

            TrackingScriptLoader customLoader = new();
            using (ScriptDefaultOptionsScope.OverrideScriptLoader(customLoader))
            {
                // Inside scope, should be custom loader
                await Assert
                    .That(Script.DefaultOptions.ScriptLoader)
                    .IsSameReferenceAs(customLoader)
                    .ConfigureAwait(false);
            }

            // After scope, should be restored
            await Assert
                .That(Script.DefaultOptions.ScriptLoader)
                .IsSameReferenceAs(originalLoader)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that scripts created inside the scope use the scoped loader,
        /// while scripts created outside use the original loader.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task ScriptsCreatedInsideScopeUsesScopedLoader()
        {
            Script scriptBefore = new();
            IScriptLoader originalLoader = scriptBefore.Options.ScriptLoader;

            TrackingScriptLoader customLoader = new();
            Script scriptDuring;

            using (ScriptDefaultOptionsScope.OverrideScriptLoader(customLoader))
            {
                scriptDuring = new Script();
            }

            Script scriptAfter = new();

            // Script created during scope should use the custom loader
            await Assert
                .That(scriptDuring.Options.ScriptLoader)
                .IsSameReferenceAs(customLoader)
                .ConfigureAwait(false);

            // Scripts created before and after should use the original loader
            // Note: These may not be the exact same reference due to how Script copies options
            await Assert
                .That(scriptBefore.Options.ScriptLoader?.GetType())
                .IsEqualTo(originalLoader?.GetType())
                .ConfigureAwait(false);

            await Assert
                .That(scriptAfter.Options.ScriptLoader?.GetType())
                .IsEqualTo(originalLoader?.GetType())
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
