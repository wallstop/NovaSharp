namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;

    /// <summary>
    /// Demonstrates the file-based test authoring pattern where tests load Lua fixtures
    /// from <c>LuaFixtures/&lt;TestClass&gt;/&lt;TestMethod&gt;.lua</c> files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This pattern provides several benefits:
    /// </para>
    /// <list type="bullet">
    ///   <item>Lua code is syntax-highlighted in editors</item>
    ///   <item>Fixtures can be run against reference Lua interpreters for semantic validation</item>
    ///   <item>Tests are more readable with separated concerns</item>
    ///   <item>Fixtures can include version compatibility metadata</item>
    /// </list>
    /// <para>
    /// To add a new test using this pattern:
    /// </para>
    /// <list type="number">
    ///   <item>Create a <c>.lua</c> file at <c>LuaFixtures/StringModuleFixtureBasedTUnitTests/YourTestMethod.lua</c></item>
    ///   <item>Add metadata headers (optional): <c>-- @lua-versions: 5.1+</c>, <c>-- @expects-error: false</c></item>
    ///   <item>Write the test method using <see cref="LuaFixtureHelper"/></item>
    /// </list>
    /// </remarks>
    public sealed class StringModuleFixtureBasedTUnitTests
    {
        /// <summary>
        /// Tests string.byte using a fixture file.
        /// </summary>
        /// <remarks>
        /// Fixture: <c>LuaFixtures/StringModuleTUnitTests/ByteAcceptsIntegralFloatIndices.lua</c>
        /// (Loaded from existing extracted fixture to demonstrate the pattern.)
        /// </remarks>
        [global::TUnit.Core.Test]
        public async Task ByteAcceptsIntegralFloatIndicesFromFixture()
        {
            // Use the helper to load a fixture from the existing StringModuleTUnitTests fixtures
            LuaFixtureHelper helper = new(
                "StringModuleTUnitTests",
                "ByteAcceptsIntegralFloatIndices"
            );

            // Verify the fixture exists and load metadata
            await Assert.That(helper.FixtureExists()).IsTrue().ConfigureAwait(false);

            LuaFixtureMetadata metadata = helper.LoadMetadata();
            await Assert.That(metadata.NovaSharpOnly).IsFalse().ConfigureAwait(false);
            await Assert.That(metadata.ExpectsError).IsFalse().ConfigureAwait(false);

            // Run the fixture
            DynValue result = helper.RunFixture();

            // string.byte('Lua', 1.0) should return 76 (ASCII code of 'L')
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(76d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests string.len using a fixture file.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task LenReturnsStringLengthFromFixture()
        {
            LuaFixtureHelper helper = new("StringModuleTUnitTests", "LenReturnsStringLength");

            await Assert.That(helper.FixtureExists()).IsTrue().ConfigureAwait(false);

            DynValue result = helper.RunFixture();

            // string.len('Nova') should return 4
            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests string.upper using a fixture file.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task UpperReturnsUppercaseStringFromFixture()
        {
            LuaFixtureHelper helper = new("StringModuleTUnitTests", "UpperReturnsUppercaseString");

            await Assert.That(helper.FixtureExists()).IsTrue().ConfigureAwait(false);

            DynValue result = helper.RunFixture();

            // Fixture: return string.upper('NovaSharp') -> "NOVASHARP"
            await Assert.That(result.String).IsEqualTo("NOVASHARP").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests string.lower using a fixture file.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task LowerReturnsLowercaseStringFromFixture()
        {
            LuaFixtureHelper helper = new("StringModuleTUnitTests", "LowerReturnsLowercaseString");

            await Assert.That(helper.FixtureExists()).IsTrue().ConfigureAwait(false);

            DynValue result = helper.RunFixture();

            // Fixture: return string.lower('NovaSharp') -> "novasharp"
            await Assert.That(result.String).IsEqualTo("novasharp").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests string.reverse with empty string using a fixture file.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task ReverseReturnsEmptyStringFromFixture()
        {
            LuaFixtureHelper helper = new(
                "StringModuleTUnitTests",
                "ReverseReturnsEmptyStringForEmptyInput"
            );

            await Assert.That(helper.FixtureExists()).IsTrue().ConfigureAwait(false);

            DynValue result = helper.RunFixture();

            await Assert.That(result.String).IsEmpty().ConfigureAwait(false);
        }

        /// <summary>
        /// Demonstrates using the ForCallingTest helper for automatic test name detection.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task FormatInterpolatesValuesFromFixture()
        {
            // Use the type-based helper for automatic class name inference
            // Note: This looks for a fixture named FormatInterpolatesValuesFromFixture.lua
            // which doesn't exist, so we manually specify the fixture name
            LuaFixtureHelper helper = new("StringModuleTUnitTests", "FormatInterpolatesValues");

            await Assert.That(helper.FixtureExists()).IsTrue().ConfigureAwait(false);

            DynValue result = helper.RunFixture();

            // Fixture: return string.format('Value: %0.2f', 3.14159) -> "Value: 3.14"
            await Assert.That(result.String).IsEqualTo("Value: 3.14").ConfigureAwait(false);
        }

        /// <summary>
        /// Demonstrates loading fixture metadata for version compatibility checks.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task MetadataContainsVersionInformation()
        {
            LuaFixtureHelper helper = new(
                "StringModuleTUnitTests",
                "ByteAcceptsIntegralFloatIndices"
            );

            LuaFixtureMetadata metadata = helper.LoadMetadata();

            // Verify metadata was parsed correctly
            await Assert.That(metadata.LuaVersions.Count).IsGreaterThan(0).ConfigureAwait(false);
            await Assert.That(metadata.SourcePath).IsNotNullOrEmpty().ConfigureAwait(false);
            await Assert
                .That(metadata.TestName)
                .IsEqualTo("StringModuleTUnitTests.ByteAcceptsIntegralFloatIndices")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Demonstrates running a fixture with a custom script configuration.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task CanRunFixtureWithCustomScript()
        {
            LuaFixtureHelper helper = new("StringModuleTUnitTests", "LenReturnsStringLength");

            // Create a script with specific modules enabled
            Script script = new(CoreModules.StringLib);

            DynValue result = helper.RunFixture(script);

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
        }
    }
}
