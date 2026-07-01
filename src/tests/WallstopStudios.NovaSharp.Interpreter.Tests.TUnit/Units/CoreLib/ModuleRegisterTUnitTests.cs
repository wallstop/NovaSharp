namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.CoreLib
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ModuleRegisterTUnitTests
    {
        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task RegisterCoreModulesRemovesWarnWhenProfileDoesNotSupportIt(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions { CompatibilityVersion = version };
            Script script = new Script(CoreModules.Basic, options);
            Table globals = new Table(script);

            globals.RegisterCoreModules(CoreModules.Basic);

            DynValue warn = globals.RawGet("warn");
            await Assert.That(warn.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task RegisterCoreModulesRemovesTableMoveWhenProfileDoesNotSupportIt(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions { CompatibilityVersion = version };
            Script script = new Script(CoreModules.Table, options);
            Table globals = new Table(script);

            globals.RegisterCoreModules(CoreModules.Table);

            DynValue tableNamespace = globals.RawGet("table");
            await Assert.That(tableNamespace.Type).IsEqualTo(DataType.Table);
            await Assert.That(tableNamespace.Table.RawGet("move").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RegisterCoreModulesThrowsWhenTableIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                ModuleRegister.RegisterCoreModules(null, CoreModules.Basic)
            );

            await Assert.That(exception.ParamName).IsEqualTo("table");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterModuleTypeThrowsWhenArgumentsAreNull(
            LuaCompatibilityVersion version
        )
        {
            Table globals = new Table(new Script(version, CoreModules.Basic));

            ArgumentNullException tableException = Assert.Throws<ArgumentNullException>(() =>
                ModuleRegister.RegisterModuleType(null, typeof(BasicModule))
            );
            ArgumentNullException typeException = Assert.Throws<ArgumentNullException>(() =>
                globals.RegisterModuleType((Type)null)
            );

            await Assert.That(tableException.ParamName).IsEqualTo("gtable");
            await Assert.That(typeException.ParamName).IsEqualTo("t");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterModuleTypeCreatesNamespaceAndPackageEntries(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModules.Basic);
            Table globals = new Table(script);

            globals.RegisterModuleType(typeof(Bit32Module));

            DynValue namespaceValue = globals.RawGet("bit32");
            DynValue package = globals.RawGet("package");
            DynValue loaded = package.Table.RawGet("loaded");
            DynValue loadedNamespace = loaded.Table.RawGet("bit32");

            await Assert.That(namespaceValue.Type).IsEqualTo(DataType.Table);
            await Assert.That(package.Type).IsEqualTo(DataType.Table);
            await Assert.That(loaded.Type).IsEqualTo(DataType.Table);
            await Assert.That(loadedNamespace.Type).IsEqualTo(DataType.Table);
            await Assert.That(loadedNamespace.Table).IsSameReferenceAs(namespaceValue.Table);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterModuleTypeAcceptsArgumentViewCallbacks(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModules.Basic);
            Table globals = script.Globals;

            globals.RegisterModuleType(typeof(ArgumentViewModule));

            DynValue result = script.DoString("return argument_view_probe.count(1, 2, 3)");

            await Assert.That(result.Number).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterModuleTypeAcceptsNoContextArgumentViewCallbacks(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModules.Basic);
            Table globals = script.Globals;

            globals.RegisterModuleType(typeof(ArgumentViewNoContextModule));

            DynValue result = script.DoString(
                "return argument_view_no_context_probe.count(1, 2, 3, 4)"
            );

            await Assert.That(result.Number).IsEqualTo(4d);
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua51, "getfenv ~= nil", true)]
        [Arguments(LuaCompatibilityVersion.Lua52, "getfenv ~= nil", false)]
        [Arguments(LuaCompatibilityVersion.Lua52, "bit32 ~= nil", true)]
        [Arguments(LuaCompatibilityVersion.Lua53, "bit32 ~= nil", false)]
        [Arguments(LuaCompatibilityVersion.Lua52, "table.move ~= nil", false)]
        [Arguments(LuaCompatibilityVersion.Lua53, "table.move ~= nil", true)]
        [Arguments(LuaCompatibilityVersion.Lua52, "utf8 ~= nil and utf8.charpattern ~= nil", false)]
        [Arguments(LuaCompatibilityVersion.Lua53, "utf8 ~= nil and utf8.charpattern ~= nil", true)]
        [Arguments(LuaCompatibilityVersion.Lua53, "warn ~= nil", false)]
        [Arguments(LuaCompatibilityVersion.Lua54, "warn ~= nil", true)]
        [Arguments(LuaCompatibilityVersion.Lua53, "math.maxinteger ~= nil", true)]
        [Arguments(LuaCompatibilityVersion.Lua52, "math.maxinteger ~= nil", false)]
        [Arguments(LuaCompatibilityVersion.Lua53, "coroutine.close ~= nil", false)]
        [Arguments(LuaCompatibilityVersion.Lua54, "coroutine.close ~= nil", true)]
        [Arguments(LuaCompatibilityVersion.Lua54, "math.log10 ~= nil", true)]
        [Arguments(LuaCompatibilityVersion.Lua55, "math.log10 ~= nil", false)]
        public async Task RegisterCoreModulesPreservesVersionFilteredMemberMatrix(
            LuaCompatibilityVersion version,
            string expression,
            bool expected
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString("return " + expression);

            await Assert.That(result.Boolean).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        public async Task RegisterModuleTypeReusesStaticDelegatesWithoutSharingWrappers()
        {
            Script firstScript = new Script(LuaCompatibilityVersion.Lua54, CoreModules.Basic);
            Script secondScript = new Script(LuaCompatibilityVersion.Lua54, CoreModules.Basic);

            DynValue firstPrint = firstScript.Globals.RawGet("print");
            DynValue secondPrint = secondScript.Globals.RawGet("print");

            await Assert.That(firstPrint.Type).IsEqualTo(DataType.ClrFunction);
            await Assert.That(secondPrint.Type).IsEqualTo(DataType.ClrFunction);
            await Assert
                .That(firstPrint.Callback)
                .IsNotSameReferenceAs(secondPrint.Callback)
                .ConfigureAwait(false);
            await Assert
                .That(firstPrint.Callback.ClrCallback)
                .IsSameReferenceAs(secondPrint.Callback.ClrCallback)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterCoreModulesDoesNotShareMutableTablesAcrossScripts(
            LuaCompatibilityVersion version
        )
        {
            Script firstScript = new Script(version, CoreModulePresets.Complete);
            Script secondScript = new Script(version, CoreModulePresets.Complete);

            DynValue firstPackage = firstScript.Globals.RawGet("package");
            DynValue secondPackage = secondScript.Globals.RawGet("package");
            DynValue firstLoaded = firstPackage.Table.RawGet("loaded");
            DynValue secondLoaded = secondPackage.Table.RawGet("loaded");
            DynValue firstString = firstScript.Globals.RawGet("string");
            DynValue secondString = secondScript.Globals.RawGet("string");

            firstString.Table.Set("only_first_script", DynValue.True);
            DynValue secondMarker = secondString.Table.RawGet("only_first_script");

            await Assert
                .That(firstPackage.Table)
                .IsNotSameReferenceAs(secondPackage.Table)
                .ConfigureAwait(false);
            await Assert
                .That(firstLoaded.Table)
                .IsNotSameReferenceAs(secondLoaded.Table)
                .ConfigureAwait(false);
            await Assert
                .That(firstString.Table)
                .IsNotSameReferenceAs(secondString.Table)
                .ConfigureAwait(false);
            await Assert.That(IsNilOrMissing(secondMarker)).IsTrue().ConfigureAwait(false);
            await Assert
                .That(firstScript.Registry.RawGet("_LOADED").Table)
                .IsSameReferenceAs(firstLoaded.Table)
                .ConfigureAwait(false);
            await Assert
                .That(secondScript.Registry.RawGet("_LOADED").Table)
                .IsSameReferenceAs(secondLoaded.Table)
                .ConfigureAwait(false);
            await Assert
                .That(firstScript.Registry.RawGet("_LOADED").Table)
                .IsNotSameReferenceAs(secondScript.Registry.RawGet("_LOADED").Table)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterModuleTypeCompilesScriptFieldForOwningScript(
            LuaCompatibilityVersion version
        )
        {
            Script firstScript = new Script(version, CoreModulePresets.Complete);
            Script secondScript = new Script(version, CoreModulePresets.Complete);

            DynValue firstRequire = firstScript.Globals.RawGet("require");
            DynValue secondRequire = secondScript.Globals.RawGet("require");

            await Assert.That(firstRequire.Type).IsEqualTo(DataType.Function);
            await Assert.That(secondRequire.Type).IsEqualTo(DataType.Function);
            await Assert
                .That(firstRequire.Function)
                .IsNotSameReferenceAs(secondRequire.Function)
                .ConfigureAwait(false);
            await Assert
                .That(firstRequire.Function.OwnerScript)
                .IsSameReferenceAs(firstScript)
                .ConfigureAwait(false);
            await Assert
                .That(secondRequire.Function.OwnerScript)
                .IsSameReferenceAs(secondScript)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterModuleTypeScriptFieldsDoNotPolluteUserCompilationCache(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new()
            {
                CompatibilityVersion = version,
                EnableScriptCaching = true,
            };
            Script script = new(CoreModules.Basic, options);
            script.LoadString("return 1", codeFriendlyName: "user-cache-entry");

            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);

            script.Globals.RegisterModuleType(typeof(ScriptFieldCacheProbeModule));
            DynValue module = script.Globals.RawGet("script_field_cache_probe");
            DynValue answer = module.Table.RawGet("answer");
            DynValue result = script.Call(answer);

            await Assert.That(result.Number).IsEqualTo(77d).ConfigureAwait(false);
            await Assert.That(script.CompilationCacheCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RegisterModuleTypeInvokesNovaSharpInitEveryRegistration()
        {
            InitProbeModule.Reset();
            Script firstScript = new Script(CoreModules.Basic);
            Script secondScript = new Script(CoreModules.Basic);

            firstScript.Globals.RegisterModuleType(typeof(InitProbeModule));
            secondScript.Globals.RegisterModuleType(typeof(InitProbeModule));

            DynValue firstProbe = firstScript.Globals.RawGet("init_probe");
            DynValue secondProbe = secondScript.Globals.RawGet("init_probe");

            await Assert.That(InitProbeModule.InitCount).IsEqualTo(2);
            await Assert.That(firstProbe.Table.RawGet("init_count").Number).IsEqualTo(1d);
            await Assert.That(secondProbe.Table.RawGet("init_count").Number).IsEqualTo(2d);
            await Assert
                .That(firstProbe.Table)
                .IsNotSameReferenceAs(secondProbe.Table)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RegisterConstantsThrowsWhenTableIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                ModuleRegister.RegisterConstants(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("table");
        }

        private static bool IsNilOrMissing(DynValue value)
        {
            return value == null || value.IsNil();
        }

        [NovaSharpModule(Namespace = "script_field_cache_probe")]
        private static class ScriptFieldCacheProbeModule
        {
            [NovaSharpModuleMethod(Name = "answer")]
            public const string Answer = "function() return 77 end";
        }

        [NovaSharpModule(Namespace = "argument_view_probe")]
        private static class ArgumentViewModule
        {
            [NovaSharpModuleMethod(Name = "count")]
            public static DynValue Count(ScriptExecutionContext context, CallbackArgumentsView args)
            {
                return DynValue.NewNumber(args.Count);
            }
        }

        [NovaSharpModule(Namespace = "argument_view_no_context_probe")]
        private static class ArgumentViewNoContextModule
        {
            [NovaSharpModuleMethod(Name = "count")]
            public static DynValue Count(CallbackArgumentsView args)
            {
                return DynValue.NewNumber(args.Count);
            }
        }

        [NovaSharpModule(Namespace = "init_probe")]
        private static class InitProbeModule
        {
            private static int InitCountStorage;

            public static int InitCount
            {
                get { return Volatile.Read(ref InitCountStorage); }
            }

            public static void Reset()
            {
                Volatile.Write(ref InitCountStorage, 0);
            }

            public static void NovaSharpInit(Table globalTable, Table moduleTable)
            {
                int initCount = Interlocked.Increment(ref InitCountStorage);
                moduleTable.Set("init_count", DynValue.NewNumber(initCount));
            }
        }
    }
}
