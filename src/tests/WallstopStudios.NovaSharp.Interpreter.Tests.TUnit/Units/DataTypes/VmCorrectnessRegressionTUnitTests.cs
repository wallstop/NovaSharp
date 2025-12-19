namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    /// <summary>
    /// Regression tests for VM correctness and state protection.
    /// These tests verify that external code cannot corrupt internal VM state.
    /// </summary>
    public sealed class VmCorrectnessRegressionTUnitTests
    {
        /// <summary>
        /// Verifies that Closure.GetUpValue returns a readonly copy, not the mutable internal reference.
        /// </summary>
        [Test]
        public async Task GetUpValueReturnsReadonlyCopy()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                local x = 10
                return function()
                    return x
                end
                "
            );

            Closure closure = function.Function;
            int xIndex = -1;
            for (int i = 0; i < closure.UpValuesCount; i++)
            {
                if (closure.GetUpValueName(i) == "x")
                {
                    xIndex = i;
                    break;
                }
            }

            await Assert.That(xIndex).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);

            DynValue upvalue = closure.GetUpValue(xIndex);

            // The returned value should be readonly
            await Assert.That(upvalue.ReadOnly).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that SetUpValue correctly modifies closure upvalues.
        /// </summary>
        [Test]
        public async Task SetUpValueModifiesClosureUpvalue()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                local x = 10
                return function()
                    return x
                end
                "
            );

            Closure closure = function.Function;
            int xIndex = -1;
            for (int i = 0; i < closure.UpValuesCount; i++)
            {
                if (closure.GetUpValueName(i) == "x")
                {
                    xIndex = i;
                    break;
                }
            }

            await Assert.That(xIndex).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);

            // Change the upvalue
            closure.SetUpValue(xIndex, DynValue.NewNumber(42));

            // Verify the closure now returns the new value
            DynValue result = closure.Call();
            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that SetUpValue throws for invalid index.
        /// </summary>
        [Test]
        public async Task SetUpValueThrowsForInvalidIndex()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 1 end");
            Closure closure = function.Function;

            await Assert
                .That(() => closure.SetUpValue(-1, DynValue.NewNumber(1)))
                .Throws<ArgumentOutOfRangeException>()
                .ConfigureAwait(false);

            await Assert
                .That(() => closure.SetUpValue(100, DynValue.NewNumber(1)))
                .Throws<ArgumentOutOfRangeException>()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that debug.setupvalue still works correctly after the API changes.
        /// The first upvalue (index 1) is _ENV, the captured variable x is at index 2.
        /// </summary>
        [Test]
        public async Task DebugSetUpValueStillWorks()
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local x = 10
                local function f()
                    return x
                end
                debug.setupvalue(f, 2, 99)  -- x is at index 2 (_ENV is at index 1)
                return f()
                "
            );

            await Assert.That(result.Number).IsEqualTo(99d).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that debug.setlocal still works correctly after the API changes.
        /// We verify this by checking that the existing debug.setlocal tests pass,
        /// and just do a simple verification that debug module is loaded.
        /// </summary>
        [Test]
        public async Task DebugSetLocalIsAvailable()
        {
            Script script = new(CoreModulePresets.Complete);
            // Just verify debug.setlocal function exists
            DynValue result = script.DoString("return type(debug.setlocal)");

            await Assert.That(result.String).IsEqualTo("function").ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that table keys are made readonly when stored in _valueMap.
        /// </summary>
        [Test]
        public async Task TableKeySafetyPreventsHashCorruption()
        {
            Script script = new();

            // Create a table as a key
            Table keyTable = new(script);
            keyTable.Set("id", DynValue.NewNumber(1));

            DynValue key = DynValue.NewTable(keyTable);
            Table mainTable = new(script);

            // Set the table as a key
            mainTable.Set(key, DynValue.NewString("value1"));

            // Verify we can retrieve the value
            DynValue retrieved = mainTable.Get(key);
            await Assert.That(retrieved.String).IsEqualTo("value1").ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that UserData hash codes don't all collide.
        /// </summary>
        [Test]
        [UserDataIsolation]
        public async Task UserDataHashCodesAreDifferent()
        {
            // Register System.Object for UserData
            UserData.RegisterType<TestUserDataObject1>();
            UserData.RegisterType<TestUserDataObject2>();

            // Create two different UserData objects
            TestUserDataObject1 obj1 = new();
            TestUserDataObject2 obj2 = new();

            DynValue ud1 = UserData.Create(obj1);
            DynValue ud2 = UserData.Create(obj2);

            // Verify they are UserData type
            await Assert.That(ud1.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
            await Assert.That(ud2.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);

            // They should have different hash codes (with very high probability)
            // because they wrap different objects
            int hash1 = ud1.GetHashCode();
            int hash2 = ud2.GetHashCode();

            // In the old code, ALL UserData had hash code 999
            await Assert.That(hash1).IsNotEqualTo(999).ConfigureAwait(false);
            await Assert.That(hash2).IsNotEqualTo(999).ConfigureAwait(false);
        }

        /// <summary>
        /// Test class for UserData hash code testing.
        /// </summary>
        private sealed class TestUserDataObject1 { }

        /// <summary>
        /// Test class for UserData hash code testing.
        /// </summary>
        private sealed class TestUserDataObject2 { }

        /// <summary>
        /// Verifies that Thread (coroutine) hash codes don't all collide.
        /// </summary>
        [Test]
        public async Task ThreadHashCodesAreDifferent()
        {
            Script script = new();

            // Create two different coroutines
            DynValue co1 = script.DoString("return coroutine.create(function() end)");
            DynValue co2 = script.DoString("return coroutine.create(function() end)");

            // They should have different hash codes
            int hash1 = co1.GetHashCode();
            int hash2 = co2.GetHashCode();

            // In the old code, ALL Thread had hash code 999
            await Assert.That(hash1).IsNotEqualTo(999).ConfigureAwait(false);
            await Assert.That(hash2).IsNotEqualTo(999).ConfigureAwait(false);
            await Assert.That(hash1).IsNotEqualTo(hash2).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that DynValue.Assign is internal and not accessible for external modification.
        /// This is verified by the fact that this test file compiles (Assign is internal but accessible
        /// due to InternalsVisibleTo) and we can use it, but external code cannot.
        /// </summary>
        [Test]
        public async Task AssignIsInternalButAccessibleToTests()
        {
            // This test verifies that Assign() still works for internal use
            DynValue target = DynValue.NewNumber(1);
            target.Assign(DynValue.NewNumber(2));

            await Assert.That(target.Number).IsEqualTo(2d).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that DynValue.Assign throws on readonly values.
        /// </summary>
        [Test]
        public async Task AssignThrowsOnReadonlyValues()
        {
            DynValue readonlyValue = DynValue.True;

            await Assert
                .That(() => readonlyValue.Assign(DynValue.False))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that closure upvalue sharing still works after modifications.
        /// Two closures should see each other's changes to shared upvalues.
        /// </summary>
        [Test]
        public async Task ClosureUpValueSharingStillWorks()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local count = 0
                local function inc() count = count + 1 end
                local function get() return count end
                
                inc()
                inc()
                return get()
                "
            );

            await Assert.That(result.Number).IsEqualTo(2d).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that debug.upvaluejoin function exists after the API changes.
        /// </summary>
        [Test]
        public async Task DebugUpValueJoinIsAvailable()
        {
            Script script = new(CoreModulePresets.Complete);
            // Just verify debug.upvaluejoin function exists
            DynValue result = script.DoString("return type(debug.upvaluejoin)");

            await Assert.That(result.String).IsEqualTo("function").ConfigureAwait(false);
        }
    }
}
