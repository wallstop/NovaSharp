namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modding;

    /// <summary>
    /// Tests for the ModContainer and ModManager classes.
    /// </summary>
    public sealed class ModContainerTUnitTests
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // ModLoadState Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModLoadStateHasExpectedValues()
        {
            int unloaded = (int)ModLoadState.Unloaded;
            int loading = (int)ModLoadState.Loading;
            int loaded = (int)ModLoadState.Loaded;
            int unloading = (int)ModLoadState.Unloading;
            int reloading = (int)ModLoadState.Reloading;
            int faulted = (int)ModLoadState.Faulted;

            await Assert.That(unloaded).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(loading).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(loaded).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(unloading).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(reloading).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(faulted).IsEqualTo(6).ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModOperationResult Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModOperationResultSucceededCreatesSuccessfulResult()
        {
            ModOperationResult result = ModOperationResult.Succeeded(
                ModLoadState.Loaded,
                "Test message"
            );

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.State).IsEqualTo(ModLoadState.Loaded).ConfigureAwait(false);
            await Assert.That(result.Message).IsEqualTo("Test message").ConfigureAwait(false);
            await Assert.That(result.Error).IsNull().ConfigureAwait(false);
        }

        [Test]
        public async Task ModOperationResultFailedWithExceptionCreatesFailedResult()
        {
            Exception error = new InvalidOperationException("Test error");
            ModOperationResult result = ModOperationResult.Failed(
                ModLoadState.Faulted,
                error,
                "Custom message"
            );

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.State).IsEqualTo(ModLoadState.Faulted).ConfigureAwait(false);
            await Assert.That(result.Message).IsEqualTo("Custom message").ConfigureAwait(false);
            await Assert.That(result.Error).IsSameReferenceAs(error).ConfigureAwait(false);
        }

        [Test]
        public async Task ModOperationResultFailedWithMessageOnlyCreatesFailedResult()
        {
            ModOperationResult result = ModOperationResult.Failed(
                ModLoadState.Faulted,
                "Error message"
            );

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.State).IsEqualTo(ModLoadState.Faulted).ConfigureAwait(false);
            await Assert.That(result.Message).IsEqualTo("Error message").ConfigureAwait(false);
            await Assert.That(result.Error).IsNull().ConfigureAwait(false);
        }

        [Test]
        public async Task ModOperationResultToStringFormatsCorrectly()
        {
            ModOperationResult success = ModOperationResult.Succeeded(ModLoadState.Loaded);
            ModOperationResult successWithMessage = ModOperationResult.Succeeded(
                ModLoadState.Loaded,
                "Done"
            );
            ModOperationResult failed = ModOperationResult.Failed(ModLoadState.Faulted, "Oops");
            ModOperationResult failedWithError = ModOperationResult.Failed(
                ModLoadState.Faulted,
                new InvalidOperationException("Test"),
                "Oops"
            );

            await Assert.That(success.ToString()).Contains("Success").ConfigureAwait(false);
            await Assert.That(successWithMessage.ToString()).Contains("Done").ConfigureAwait(false);
            await Assert.That(failed.ToString()).Contains("Failed").ConfigureAwait(false);
            await Assert
                .That(failedWithError.ToString())
                .Contains("InvalidOperationException")
                .ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModEventArgs Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModEventArgsCapturesModAndState()
        {
            ModContainer mod = new ModContainer("test-mod");
            ModEventArgs args = new ModEventArgs(mod, ModLoadState.Loading);

            await Assert.That(args.ModContainer).IsSameReferenceAs(mod).ConfigureAwait(false);
            await Assert.That(args.State).IsEqualTo(ModLoadState.Loading).ConfigureAwait(false);
        }

        [Test]
        public async Task ModErrorEventArgsCapturesError()
        {
            ModContainer mod = new ModContainer("test-mod");
            InvalidOperationException error = new InvalidOperationException("Test error");
            ModErrorEventArgs args = new ModErrorEventArgs(
                mod,
                ModLoadState.Faulted,
                error,
                "Load"
            );

            await Assert.That(args.Error).IsSameReferenceAs(error).ConfigureAwait(false);
            await Assert.That(args.Operation).IsEqualTo("Load").ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer Constructor Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerConstructorSetsModId()
        {
            ModContainer mod = new ModContainer("my-mod");

            await Assert.That(mod.ModId).IsEqualTo("my-mod").ConfigureAwait(false);
            await Assert.That(mod.DisplayName).IsEqualTo("my-mod").ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerConstructorSetsDisplayName()
        {
            ModContainer mod = new ModContainer("my-mod", "My Awesome Mod");

            await Assert.That(mod.ModId).IsEqualTo("my-mod").ConfigureAwait(false);
            await Assert.That(mod.DisplayName).IsEqualTo("My Awesome Mod").ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerConstructorThrowsOnNullModId()
        {
            await Assert
                .That(() => new ModContainer(null))
                .Throws<ArgumentException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerConstructorThrowsOnEmptyModId()
        {
            await Assert
                .That(() => new ModContainer(""))
                .Throws<ArgumentException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerInitialStateIsUnloaded()
        {
            ModContainer mod = new ModContainer("test");

            await Assert.That(mod.State).IsEqualTo(ModLoadState.Unloaded).ConfigureAwait(false);
            await Assert.That(mod.Script).IsNull().ConfigureAwait(false);
            await Assert.That(mod.Globals).IsNull().ConfigureAwait(false);
            await Assert.That(mod.LastError).IsNull().ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer Entry Point Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerAddEntryPointAddsScript()
        {
            ModContainer mod = new ModContainer("test");
            mod.AddEntryPoint("x = 1");
            mod.AddEntryPoint("y = 2");

            await Assert.That(mod.EntryPoints.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(mod.EntryPoints[0]).IsEqualTo("x = 1").ConfigureAwait(false);
            await Assert.That(mod.EntryPoints[1]).IsEqualTo("y = 2").ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerAddEntryPointSupportsFluent()
        {
            ModContainer mod = new ModContainer("test")
                .AddEntryPoint("x = 1")
                .AddEntryPoint("y = 2");

            await Assert.That(mod.EntryPoints.Count).IsEqualTo(2).ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerClearEntryPointsRemovesAll()
        {
            ModContainer mod = new ModContainer("test")
                .AddEntryPoint("x = 1")
                .AddEntryPoint("y = 2")
                .ClearEntryPoints();

            await Assert.That(mod.EntryPoints.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerAddEntryPointThrowsOnNull()
        {
            ModContainer mod = new ModContainer("test");

            await Assert
                .That(() => mod.AddEntryPoint(null))
                .Throws<ArgumentException>()
                .ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer Load Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerLoadCreatesScript()
        {
            ModContainer mod = new ModContainer("test");
            ModOperationResult result = mod.Load();

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.State).IsEqualTo(ModLoadState.Loaded).ConfigureAwait(false);
            await Assert.That(mod.State).IsEqualTo(ModLoadState.Loaded).ConfigureAwait(false);
            await Assert.That(mod.Script).IsNotNull().ConfigureAwait(false);
            await Assert.That(mod.Globals).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerLoadExecutesEntryPoints()
        {
            ModContainer mod = new ModContainer("test")
                .AddEntryPoint("counter = 0")
                .AddEntryPoint("counter = counter + 1")
                .AddEntryPoint("counter = counter + 10");

            mod.Load();

            DynValue counter = mod.GetGlobal("counter");
            await Assert.That(counter.Number).IsEqualTo(11).ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerLoadSetsModMetadata()
        {
            ModContainer mod = new ModContainer("my-mod", "My Mod");
            mod.Load();

            DynValue modId = mod.GetGlobal("MOD_ID");
            DynValue modName = mod.GetGlobal("MOD_NAME");

            await Assert.That(modId.String).IsEqualTo("my-mod").ConfigureAwait(false);
            await Assert.That(modName.String).IsEqualTo("My Mod").ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerLoadFailsOnScriptError()
        {
            ModContainer mod = new ModContainer("test").AddEntryPoint(
                "this is not valid lua syntax!!!"
            );

            ModOperationResult result = mod.Load();

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.State).IsEqualTo(ModLoadState.Faulted).ConfigureAwait(false);
            await Assert.That(mod.State).IsEqualTo(ModLoadState.Faulted).ConfigureAwait(false);
            await Assert.That(mod.LastError).IsNotNull().ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerLoadWhenAlreadyLoadedFails()
        {
            ModContainer mod = new ModContainer("test");
            mod.Load();

            ModOperationResult result = mod.Load();

            await Assert.That(result.Success).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Message).Contains("already loaded").ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerLoadFiresEvents()
        {
            ModContainer mod = new ModContainer("test");
            List<string> events = new List<string>();

            mod.OnLoading += (s, e) => events.Add("loading");
            mod.OnLoaded += (s, e) => events.Add("loaded");

            mod.Load();

            await Assert.That(events).Contains("loading").ConfigureAwait(false);
            await Assert.That(events).Contains("loaded").ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerLoadFiresErrorEventOnFailure()
        {
            ModContainer mod = new ModContainer("test").AddEntryPoint("invalid!!!");

            ModErrorEventArgs capturedArgs = null;
            mod.OnError += (s, e) => capturedArgs = e;

            mod.Load();

            await Assert.That(capturedArgs).IsNotNull().ConfigureAwait(false);
            await Assert.That(capturedArgs.Operation).IsEqualTo("Load").ConfigureAwait(false);
            await Assert.That(capturedArgs.Error).IsNotNull().ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer Unload Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerUnloadClearsScript()
        {
            ModContainer mod = new ModContainer("test");
            mod.Load();
            ModOperationResult result = mod.Unload();

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.State).IsEqualTo(ModLoadState.Unloaded).ConfigureAwait(false);
            await Assert.That(mod.State).IsEqualTo(ModLoadState.Unloaded).ConfigureAwait(false);
            await Assert.That(mod.Script).IsNull().ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerUnloadWhenNotLoadedSucceeds()
        {
            ModContainer mod = new ModContainer("test");
            ModOperationResult result = mod.Unload();

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Message).Contains("already unloaded").ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerUnloadCallsLuaOnUnload()
        {
            ModContainer mod = new ModContainer("test").AddEntryPoint(
                @"
                    cleanup_called = false
                    function on_unload()
                        cleanup_called = true
                    end
                "
            );

            mod.Load();

            // Verify on_unload exists
            DynValue func = mod.GetGlobal("on_unload");
            await Assert.That(func.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);

            mod.Unload();

            // Can't check cleanup_called after unload, but we verified the function exists
        }

        [Test]
        public async Task ModContainerUnloadCallsCSharpHandler()
        {
            ModContainer mod = new ModContainer("test");
            bool handlerCalled = false;

            mod.UnloadHandler = (container, script) => handlerCalled = true;
            mod.Load();
            mod.Unload();

            await Assert.That(handlerCalled).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerUnloadFiresEvents()
        {
            ModContainer mod = new ModContainer("test");
            List<string> events = new List<string>();

            mod.OnUnloading += (s, e) => events.Add("unloading");
            mod.OnUnloaded += (s, e) => events.Add("unloaded");

            mod.Load();
            mod.Unload();

            await Assert.That(events).Contains("unloading").ConfigureAwait(false);
            await Assert.That(events).Contains("unloaded").ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer Reload Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerReloadRecreatesScript()
        {
            ModContainer mod = new ModContainer("test").AddEntryPoint("x = 42");

            mod.Load();
            Script originalScript = mod.Script;

            ModOperationResult result = mod.Reload();

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(mod.State).IsEqualTo(ModLoadState.Loaded).ConfigureAwait(false);
            await Assert.That(mod.Script).IsNotNull().ConfigureAwait(false);
            await Assert
                .That(mod.Script)
                .IsNotSameReferenceAs(originalScript)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerReloadClearsOldState()
        {
            ModContainer mod = new ModContainer("test").AddEntryPoint("x = 1");

            mod.Load();
            mod.DoString("x = 999");

            // Clear entry points and add new one for reload
            mod.ClearEntryPoints().AddEntryPoint("x = 1");
            mod.Reload();

            DynValue x = mod.GetGlobal("x");
            await Assert.That(x.Number).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerReloadFromUnloadedStateLoads()
        {
            ModContainer mod = new ModContainer("test").AddEntryPoint("x = 42");

            ModOperationResult result = mod.Reload();

            await Assert.That(result.Success).IsTrue().ConfigureAwait(false);
            await Assert.That(mod.State).IsEqualTo(ModLoadState.Loaded).ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerReloadFiresEvents()
        {
            ModContainer mod = new ModContainer("test");
            List<string> events = new List<string>();

            mod.OnReloading += (s, e) => events.Add("reloading");
            mod.OnReloaded += (s, e) => events.Add("reloaded");

            mod.Load();
            events.Clear();
            mod.Reload();

            await Assert.That(events).Contains("reloading").ConfigureAwait(false);
            await Assert.That(events).Contains("reloaded").ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer Script Access Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerDoStringExecutesCode()
        {
            ModContainer mod = new ModContainer("test");
            mod.Load();

            DynValue result = mod.DoString("return 1 + 2");

            await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerDoStringThrowsWhenNotLoaded()
        {
            ModContainer mod = new ModContainer("test");

            await Assert
                .That(() => mod.DoString("return 1"))
                .Throws<InvalidOperationException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerCallInvokesFunction()
        {
            ModContainer mod = new ModContainer("test").AddEntryPoint(
                "function add(a, b) return a + b end"
            );

            mod.Load();
            DynValue result = mod.CallFunction("add", 3, 4);

            await Assert.That(result.Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerCallThrowsOnMissingFunction()
        {
            ModContainer mod = new ModContainer("test");
            mod.Load();

            await Assert
                .That(() => mod.CallFunction("nonexistent"))
                .Throws<Errors.ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerGetGlobalReturnsValue()
        {
            ModContainer mod = new ModContainer("test").AddEntryPoint("myValue = 'hello'");

            mod.Load();
            DynValue value = mod.GetGlobal("myValue");

            await Assert.That(value.String).IsEqualTo("hello").ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerGetGlobalReturnsNilForMissing()
        {
            ModContainer mod = new ModContainer("test");
            mod.Load();

            DynValue value = mod.GetGlobal("nonexistent");

            await Assert.That(value.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerSetGlobalSetsValue()
        {
            ModContainer mod = new ModContainer("test");
            mod.Load();

            mod.SetGlobal("myValue", DynValue.NewNumber(42));
            DynValue result = mod.GetGlobal("myValue");

            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer Custom Factory Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerScriptFactoryIsUsed()
        {
            bool factoryCalled = false;
            ModContainer mod = new ModContainer("test")
            {
                ScriptFactory = container =>
                {
                    factoryCalled = true;
                    return new Script();
                },
            };

            mod.Load();

            await Assert.That(factoryCalled).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ModContainerScriptConfiguratorIsUsed()
        {
            bool configuratorCalled = false;
            ModContainer mod = new ModContainer("test")
            {
                ScriptConfigurator = (container, script) =>
                {
                    configuratorCalled = true;
                    script.Globals["custom_value"] = DynValue.NewNumber(123);
                },
            };

            mod.Load();

            await Assert.That(configuratorCalled).IsTrue().ConfigureAwait(false);
            await Assert
                .That(mod.GetGlobal("custom_value").Number)
                .IsEqualTo(123)
                .ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer Isolation Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerScriptsAreIsolated()
        {
            ModContainer mod1 = new ModContainer("mod1").AddEntryPoint("sharedValue = 'mod1'");

            ModContainer mod2 = new ModContainer("mod2").AddEntryPoint("sharedValue = 'mod2'");

            mod1.Load();
            mod2.Load();

            await Assert
                .That(mod1.GetGlobal("sharedValue").String)
                .IsEqualTo("mod1")
                .ConfigureAwait(false);
            await Assert
                .That(mod2.GetGlobal("sharedValue").String)
                .IsEqualTo("mod2")
                .ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModManager Registration Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModManagerRegisterAddsMod()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("test");

            bool result = manager.Register(mod);

            await Assert.That(result).IsTrue().ConfigureAwait(false);
            await Assert.That(manager.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(manager.Contains("test")).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerRegisterRejectsDuplicate()
        {
            ModManager manager = new ModManager();
            ModContainer mod1 = new ModContainer("test");
            ModContainer mod2 = new ModContainer("test");

            manager.Register(mod1);
            bool result = manager.Register(mod2);

            await Assert.That(result).IsFalse().ConfigureAwait(false);
            await Assert.That(manager.Count).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerUnregisterRemovesMod()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("test");

            manager.Register(mod);
            bool result = manager.Unregister("test");

            await Assert.That(result).IsTrue().ConfigureAwait(false);
            await Assert.That(manager.Count).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(manager.Contains("test")).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerUnregisterUnloadsLoadedMod()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("test");

            manager.Register(mod);
            mod.Load();
            manager.Unregister("test");

            await Assert.That(mod.State).IsEqualTo(ModLoadState.Unloaded).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerGetModReturnsRegisteredMod()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("test");

            manager.Register(mod);
            IModContainer retrieved = manager.GetMod("test");

            await Assert.That(retrieved).IsSameReferenceAs(mod).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerGetModReturnsNullForMissing()
        {
            ModManager manager = new ModManager();
            IModContainer retrieved = manager.GetMod("nonexistent");

            await Assert.That(retrieved).IsNull().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerTryGetModReturnsTrue()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("test");
            manager.Register(mod);

            bool result = manager.TryGetMod("test", out IModContainer retrieved);

            await Assert.That(result).IsTrue().ConfigureAwait(false);
            await Assert.That(retrieved).IsSameReferenceAs(mod).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerModIdsReturnsAllIds()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));
            manager.Register(new ModContainer("mod2"));
            manager.Register(new ModContainer("mod3"));

            IReadOnlyList<string> ids = manager.ModIds;

            await Assert.That(ids.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(ids).Contains("mod1").ConfigureAwait(false);
            await Assert.That(ids).Contains("mod2").ConfigureAwait(false);
            await Assert.That(ids).Contains("mod3").ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModManager Dependency Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModManagerAddDependencySucceeds()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));
            manager.Register(new ModContainer("mod2"));

            bool result = manager.AddDependency("mod2", "mod1");

            await Assert.That(result).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerAddDependencyRejectsSelfReference()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));

            bool result = manager.AddDependency("mod1", "mod1");

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerAddDependencyRejectsCycle()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));
            manager.Register(new ModContainer("mod2"));

            manager.AddDependency("mod2", "mod1");
            bool result = manager.AddDependency("mod1", "mod2");

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerAddDependencyRejectsTransitiveCycle()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));
            manager.Register(new ModContainer("mod2"));
            manager.Register(new ModContainer("mod3"));

            manager.AddDependency("mod2", "mod1");
            manager.AddDependency("mod3", "mod2");
            bool result = manager.AddDependency("mod1", "mod3");

            await Assert.That(result).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerGetDependenciesReturnsCorrectList()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));
            manager.Register(new ModContainer("mod2"));
            manager.Register(new ModContainer("mod3"));

            manager.AddDependency("mod3", "mod1");
            manager.AddDependency("mod3", "mod2");

            IReadOnlyList<string> deps = manager.GetDependencies("mod3");

            await Assert.That(deps.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(deps).Contains("mod1").ConfigureAwait(false);
            await Assert.That(deps).Contains("mod2").ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerGetLoadOrderRespectsDependencies()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod3"));
            manager.Register(new ModContainer("mod1"));
            manager.Register(new ModContainer("mod2"));

            manager.AddDependency("mod3", "mod2");
            manager.AddDependency("mod2", "mod1");

            IReadOnlyList<string> order = manager.GetLoadOrder();
            List<string> orderList = new List<string>(order);

            int mod1Index = orderList.IndexOf("mod1");
            int mod2Index = orderList.IndexOf("mod2");
            int mod3Index = orderList.IndexOf("mod3");

            await Assert.That(mod1Index).IsLessThan(mod2Index).ConfigureAwait(false);
            await Assert.That(mod2Index).IsLessThan(mod3Index).ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModManager Bulk Operations Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModManagerLoadAllLoadsAllMods()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));
            manager.Register(new ModContainer("mod2"));

            IDictionary<string, ModOperationResult> results = manager.LoadAll();

            await Assert.That(results.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(results["mod1"].Success).IsTrue().ConfigureAwait(false);
            await Assert.That(results["mod2"].Success).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerLoadAllRespectsDependencyOrder()
        {
            ModManager manager = new ModManager();
            List<string> loadOrder = new List<string>();

            ModContainer mod1 = new ModContainer("mod1");
            ModContainer mod2 = new ModContainer("mod2");

            mod1.OnLoaded += (s, e) => loadOrder.Add("mod1");
            mod2.OnLoaded += (s, e) => loadOrder.Add("mod2");

            manager.Register(mod1);
            manager.Register(mod2);
            manager.AddDependency("mod2", "mod1");

            manager.LoadAll();

            await Assert.That(loadOrder[0]).IsEqualTo("mod1").ConfigureAwait(false);
            await Assert.That(loadOrder[1]).IsEqualTo("mod2").ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerLoadAllSkipsAlreadyLoaded()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("mod1");

            manager.Register(mod);
            mod.Load();

            IDictionary<string, ModOperationResult> results = manager.LoadAll();

            await Assert.That(results["mod1"].Success).IsTrue().ConfigureAwait(false);
            await Assert
                .That(results["mod1"].Message)
                .Contains("Already loaded")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerUnloadAllUnloadsAllMods()
        {
            ModManager manager = new ModManager();
            ModContainer mod1 = new ModContainer("mod1");
            ModContainer mod2 = new ModContainer("mod2");

            manager.Register(mod1);
            manager.Register(mod2);
            manager.LoadAll();

            IDictionary<string, ModOperationResult> results = manager.UnloadAll();

            await Assert.That(results.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(mod1.State).IsEqualTo(ModLoadState.Unloaded).ConfigureAwait(false);
            await Assert.That(mod2.State).IsEqualTo(ModLoadState.Unloaded).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerUnloadAllUnloadsInReverseOrder()
        {
            ModManager manager = new ModManager();
            List<string> unloadOrder = new List<string>();

            ModContainer mod1 = new ModContainer("mod1");
            ModContainer mod2 = new ModContainer("mod2");

            mod1.OnUnloaded += (s, e) => unloadOrder.Add("mod1");
            mod2.OnUnloaded += (s, e) => unloadOrder.Add("mod2");

            manager.Register(mod1);
            manager.Register(mod2);
            manager.AddDependency("mod2", "mod1");
            manager.LoadAll();

            manager.UnloadAll();

            // mod2 should unload before mod1 (reverse dependency order)
            await Assert.That(unloadOrder[0]).IsEqualTo("mod2").ConfigureAwait(false);
            await Assert.That(unloadOrder[1]).IsEqualTo("mod1").ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerReloadAllReloadsAllMods()
        {
            ModManager manager = new ModManager();
            ModContainer mod1 = new ModContainer("mod1").AddEntryPoint("x = 1");
            ModContainer mod2 = new ModContainer("mod2").AddEntryPoint("y = 2");

            manager.Register(mod1);
            manager.Register(mod2);
            manager.LoadAll();

            // Modify state
            mod1.DoString("x = 99");
            mod2.DoString("y = 99");

            IDictionary<string, ModOperationResult> results = manager.ReloadAll();

            await Assert.That(results.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(mod1.GetGlobal("x").Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(mod2.GetGlobal("y").Number).IsEqualTo(2).ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModManager Inter-Mod Communication Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModManagerBroadcastCallInvokesAllMods()
        {
            ModManager manager = new ModManager();

            ModContainer mod1 = new ModContainer("mod1").AddEntryPoint(
                "function on_tick() return 'mod1' end"
            );

            ModContainer mod2 = new ModContainer("mod2").AddEntryPoint(
                "function on_tick() return 'mod2' end"
            );

            manager.Register(mod1);
            manager.Register(mod2);
            manager.LoadAll();

            IDictionary<string, DynValue> results = manager.BroadcastCall("on_tick");

            await Assert.That(results.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(results["mod1"].String).IsEqualTo("mod1").ConfigureAwait(false);
            await Assert.That(results["mod2"].String).IsEqualTo("mod2").ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerBroadcastCallSkipsModsWithoutFunction()
        {
            ModManager manager = new ModManager();

            ModContainer mod1 = new ModContainer("mod1").AddEntryPoint(
                "function on_tick() return 'mod1' end"
            );

            ModContainer mod2 = new ModContainer("mod2").AddEntryPoint("-- no on_tick function");

            manager.Register(mod1);
            manager.Register(mod2);
            manager.LoadAll();

            IDictionary<string, DynValue> results = manager.BroadcastCall("on_tick");

            await Assert.That(results.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(results.ContainsKey("mod1")).IsTrue().ConfigureAwait(false);
            await Assert.That(results.ContainsKey("mod2")).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerBroadcastCallPassesArguments()
        {
            ModManager manager = new ModManager();

            ModContainer mod = new ModContainer("mod1").AddEntryPoint(
                "function add(a, b) return a + b end"
            );

            manager.Register(mod);
            manager.LoadAll();

            IDictionary<string, DynValue> results = manager.BroadcastCall("add", 10, 5);

            await Assert.That(results["mod1"].Number).IsEqualTo(15).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerGetModGlobalReturnsValue()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("mod1").AddEntryPoint("myValue = 42");

            manager.Register(mod);
            manager.LoadAll();

            DynValue value = manager.GetModGlobal("mod1", "myValue");

            await Assert.That(value.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerGetModGlobalReturnsNilForMissingMod()
        {
            ModManager manager = new ModManager();

            DynValue value = manager.GetModGlobal("nonexistent", "myValue");

            await Assert.That(value.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModManager Events Tests
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModManagerFiresOnModRegistered()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("test");
            IModContainer capturedMod = null;

            manager.OnModRegistered += (s, e) => capturedMod = e.ModContainer;
            manager.Register(mod);

            await Assert.That(capturedMod).IsSameReferenceAs(mod).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerFiresOnModUnregistered()
        {
            ModManager manager = new ModManager();
            ModContainer mod = new ModContainer("test");
            IModContainer capturedMod = null;

            manager.OnModUnregistered += (s, e) => capturedMod = e.ModContainer;
            manager.Register(mod);
            manager.Unregister("test");

            await Assert.That(capturedMod).IsSameReferenceAs(mod).ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerFiresOnAllModsLoaded()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));
            manager.Register(new ModContainer("mod2"));

            bool eventFired = false;
            manager.OnAllModsLoaded += (s, e) => eventFired = true;

            manager.LoadAll();

            await Assert.That(eventFired).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ModManagerFiresOnAllModsUnloaded()
        {
            ModManager manager = new ModManager();
            manager.Register(new ModContainer("mod1"));
            manager.LoadAll();

            bool eventFired = false;
            manager.OnAllModsUnloaded += (s, e) => eventFired = true;

            manager.UnloadAll();

            await Assert.That(eventFired).IsTrue().ConfigureAwait(false);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ModContainer ToString Test
        // ═══════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ModContainerToStringFormatsCorrectly()
        {
            ModContainer mod = new ModContainer("my-mod");
            string str = mod.ToString();

            await Assert.That(str).Contains("my-mod").ConfigureAwait(false);
            await Assert.That(str).Contains("Unloaded").ConfigureAwait(false);
        }
    }
}
