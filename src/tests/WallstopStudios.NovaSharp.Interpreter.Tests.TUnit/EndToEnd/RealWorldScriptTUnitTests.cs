namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class RealWorldScriptTUnitTests
    {
        private static readonly string FixtureRoot = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "RealWorld"
        );

        [global::TUnit.Core.Test]
        public Task JsonLuaFixtureEncodesAndDecodes()
        {
            return ExecuteFixtureAsync(Path.Combine("rxi-json", "json.lua"), ExerciseRxiJsonAsync);
        }

        [global::TUnit.Core.Test]
        public Task InspectLuaFixtureFormatsTables()
        {
            return ExecuteFixtureAsync(
                Path.Combine("kikito-inspect", "inspect.lua"),
                ExerciseKikitoInspectAsync
            );
        }

        private static async Task ExecuteFixtureAsync(
            string relativePath,
            Func<Script, DynValue, Task> exercise
        )
        {
            ArgumentNullException.ThrowIfNull(exercise);

            string scriptPath = Path.Combine(FixtureRoot, relativePath);
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Fixture not found: {relativePath}", scriptPath);
            }

            string source = await File.ReadAllTextAsync(scriptPath).ConfigureAwait(false);
            Script script = new(CoreModulePresets.Complete);
            DynValue moduleValue = script.DoString(source);

            await Assert.That(moduleValue.IsNil()).IsFalse().ConfigureAwait(false);
            await exercise(script, moduleValue).ConfigureAwait(false);
        }

        private static async Task ExerciseRxiJsonAsync(Script script, DynValue moduleValue)
        {
            await Assert.That(moduleValue.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);

            DynValue version = moduleValue.Table.Get("_version");
            await Assert.That(version.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(version.String).IsEqualTo("0.1.2").ConfigureAwait(false);

            DynValue encode = moduleValue.Table.Get("encode");
            DynValue decode = moduleValue.Table.Get("decode");

            await Assert.That(encode.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);
            await Assert.That(decode.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);

            DynValue sampleTable = script.DoString(
                @"
                    return {
                        answer = 42,
                        nested = { 1, 2, 3 },
                        label = 'NovaSharp',
                        active = true
                    }
                "
            );

            DynValue encoded = script.Call(encode, sampleTable);
            await Assert.That(encoded.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(encoded.String).Contains("\"answer\":42").ConfigureAwait(false);
            await Assert
                .That(encoded.String)
                .Contains("\"label\":\"NovaSharp\"")
                .ConfigureAwait(false);
            await Assert.That(encoded.String).Contains("\"nested\":[1,2,3]").ConfigureAwait(false);

            DynValue decoded = script.Call(decode, encoded);
            await Assert.That(decoded.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert
                .That(decoded.Table.Get("answer").Number)
                .IsEqualTo(42)
                .ConfigureAwait(false);
            await Assert
                .That(decoded.Table.Get("label").String)
                .IsEqualTo("NovaSharp")
                .ConfigureAwait(false);
            await Assert.That(decoded.Table.Get("active").Boolean).IsTrue().ConfigureAwait(false);

            DynValue nested = decoded.Table.Get("nested");
            await Assert.That(nested.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert.That(nested.Table.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(nested.Table.Get(1).Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(nested.Table.Get(2).Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(nested.Table.Get(3).Number).IsEqualTo(3).ConfigureAwait(false);
        }

        private static async Task ExerciseKikitoInspectAsync(Script script, DynValue moduleValue)
        {
            await Assert.That(moduleValue.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);

            DynValue version = moduleValue.Table.Get("_VERSION");
            await Assert.That(version.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(version.String).IsEqualTo("inspect.lua 3.1.0").ConfigureAwait(false);

            DynValue inspectFunc = moduleValue.Table.Get("inspect");
            await Assert.That(inspectFunc.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);

            DynValue sample = script.DoString(
                @"
                    local ui = setmetatable(
                        { isVisible = true },
                        { __tostring = function() return 'ui-meta' end }
                    )

                    return {
                        player = { name = 'Nova', stats = { hp = 100, mana = 45 } },
                        tags = { 'alpha', 'beta' },
                        overlay = ui
                    }
                "
            );

            DynValue inspected = script.Call(inspectFunc, sample);
            await Assert.That(inspected.Type).IsEqualTo(DataType.String).ConfigureAwait(false);

            string output = inspected.String;
            await Assert.That(output).Contains("player = {").ConfigureAwait(false);
            await Assert.That(output).Contains("stats = {").ConfigureAwait(false);
            await Assert.That(output).Contains("overlay = { -- ui-meta").ConfigureAwait(false);
            await Assert.That(output).Contains("<metatable> = {").ConfigureAwait(false);
            await Assert.That(output).Contains("tags = {").ConfigureAwait(false);
            await Assert.That(output).Contains("\"alpha\"").ConfigureAwait(false);
            await Assert.That(output).Contains("\"beta\"").ConfigureAwait(false);
        }
    }
}
