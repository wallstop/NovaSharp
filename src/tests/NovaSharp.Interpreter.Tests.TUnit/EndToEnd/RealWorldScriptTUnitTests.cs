#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

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

            string source = await File.ReadAllTextAsync(scriptPath);
            Script script = new(CoreModules.PresetComplete);
            DynValue moduleValue = script.DoString(source);

            await Assert.That(moduleValue.IsNil()).IsFalse();
            await exercise(script, moduleValue);
        }

        private static async Task ExerciseRxiJsonAsync(Script script, DynValue moduleValue)
        {
            await Assert.That(moduleValue.Type).IsEqualTo(DataType.Table);

            DynValue version = moduleValue.Table.Get("_version");
            await Assert.That(version.Type).IsEqualTo(DataType.String);
            await Assert.That(version.String).IsEqualTo("0.1.2");

            DynValue encode = moduleValue.Table.Get("encode");
            DynValue decode = moduleValue.Table.Get("decode");

            await Assert.That(encode.Type).IsEqualTo(DataType.Function);
            await Assert.That(decode.Type).IsEqualTo(DataType.Function);

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
            await Assert.That(encoded.Type).IsEqualTo(DataType.String);
            await Assert.That(encoded.String).Contains("\"answer\":42");
            await Assert.That(encoded.String).Contains("\"label\":\"NovaSharp\"");
            await Assert.That(encoded.String).Contains("\"nested\":[1,2,3]");

            DynValue decoded = script.Call(decode, encoded);
            await Assert.That(decoded.Type).IsEqualTo(DataType.Table);
            await Assert.That(decoded.Table.Get("answer").Number).IsEqualTo(42);
            await Assert.That(decoded.Table.Get("label").String).IsEqualTo("NovaSharp");
            await Assert.That(decoded.Table.Get("active").Boolean).IsTrue();

            DynValue nested = decoded.Table.Get("nested");
            await Assert.That(nested.Type).IsEqualTo(DataType.Table);
            await Assert.That(nested.Table.Length).IsEqualTo(3);
            await Assert.That(nested.Table.Get(1).Number).IsEqualTo(1);
            await Assert.That(nested.Table.Get(2).Number).IsEqualTo(2);
            await Assert.That(nested.Table.Get(3).Number).IsEqualTo(3);
        }

        private static async Task ExerciseKikitoInspectAsync(Script script, DynValue moduleValue)
        {
            await Assert.That(moduleValue.Type).IsEqualTo(DataType.Table);

            DynValue version = moduleValue.Table.Get("_VERSION");
            await Assert.That(version.Type).IsEqualTo(DataType.String);
            await Assert.That(version.String).IsEqualTo("inspect.lua 3.1.0");

            DynValue inspectFunc = moduleValue.Table.Get("inspect");
            await Assert.That(inspectFunc.Type).IsEqualTo(DataType.Function);

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
            await Assert.That(inspected.Type).IsEqualTo(DataType.String);

            string output = inspected.String;
            await Assert.That(output).Contains("player = {");
            await Assert.That(output).Contains("stats = {");
            await Assert.That(output).Contains("overlay = { -- ui-meta");
            await Assert.That(output).Contains("<metatable> = {");
            await Assert.That(output).Contains("tags = {");
            await Assert.That(output).Contains("\"alpha\"");
            await Assert.That(output).Contains("\"beta\"");
        }
    }
}
#pragma warning restore CA2007
