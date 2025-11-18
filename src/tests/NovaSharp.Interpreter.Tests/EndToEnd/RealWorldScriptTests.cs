namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RealWorldScriptTests
    {
        private static readonly string _fixtureRoot = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Fixtures",
            "RealWorld"
        );

        private static IEnumerable<TestCaseData> Corpus()
        {
            yield return new TestCaseData(
                "json.lua v0.1.2 (rxi)",
                Path.Combine("rxi-json", "json.lua"),
                (Action<Script, DynValue>)ExerciseRxiJson
            ).SetName("RealWorld_json_lua_encodes_and_decodes");

            yield return new TestCaseData(
                "inspect.lua v3.1.0 (kikito)",
                Path.Combine("kikito-inspect", "inspect.lua"),
                (Action<Script, DynValue>)ExerciseKikitoInspect
            ).SetName("RealWorld_inspect_lua_formats_tables");
        }

        [TestCaseSource(nameof(Corpus))]
        public void RealWorldScriptsExecuteWithoutRegressions(
            string name,
            string relativePath,
            Action<Script, DynValue> exercise
        )
        {
            string scriptPath = Path.Combine(_fixtureRoot, relativePath);
            Assert.That(File.Exists(scriptPath), Is.True, $"Fixture not found: {relativePath}");

            string source = File.ReadAllText(scriptPath);
            Script script = new(CoreModules.PresetComplete);

            DynValue moduleValue = script.DoString(source);
            Assert.That(moduleValue.IsNil(), Is.False, $"Fixture returned nil: {name}");

            exercise(script, moduleValue);
        }

        private static void ExerciseRxiJson(Script script, DynValue moduleValue)
        {
            Assert.That(moduleValue.Type, Is.EqualTo(DataType.Table));

            DynValue version = moduleValue.Table.Get("_version");
            Assert.That(version.Type, Is.EqualTo(DataType.String));
            Assert.That(version.String, Is.EqualTo("0.1.2"));

            DynValue encode = moduleValue.Table.Get("encode");
            DynValue decode = moduleValue.Table.Get("decode");

            Assert.That(encode.Type, Is.EqualTo(DataType.Function));
            Assert.That(decode.Type, Is.EqualTo(DataType.Function));

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
            Assert.That(encoded.Type, Is.EqualTo(DataType.String));
            Assert.That(encoded.String, Does.Contain("\"answer\":42"));
            Assert.That(encoded.String, Does.Contain("\"label\":\"NovaSharp\""));
            Assert.That(encoded.String, Does.Contain("\"nested\":[1,2,3]"));

            DynValue decoded = script.Call(decode, encoded);
            Assert.That(decoded.Type, Is.EqualTo(DataType.Table));
            Assert.That(decoded.Table.Get("answer").Number, Is.EqualTo(42));
            Assert.That(decoded.Table.Get("label").String, Is.EqualTo("NovaSharp"));
            Assert.That(decoded.Table.Get("active").Boolean, Is.True);

            DynValue nested = decoded.Table.Get("nested");
            Assert.That(nested.Type, Is.EqualTo(DataType.Table));
            Assert.That(nested.Table.Length, Is.EqualTo(3));
            Assert.That(nested.Table.Get(1).Number, Is.EqualTo(1));
            Assert.That(nested.Table.Get(2).Number, Is.EqualTo(2));
            Assert.That(nested.Table.Get(3).Number, Is.EqualTo(3));
        }

        private static void ExerciseKikitoInspect(Script script, DynValue moduleValue)
        {
            Assert.That(moduleValue.Type, Is.EqualTo(DataType.Table));

            DynValue version = moduleValue.Table.Get("_VERSION");
            Assert.That(version.Type, Is.EqualTo(DataType.String));
            Assert.That(version.String, Is.EqualTo("inspect.lua 3.1.0"));

            DynValue inspectFunc = moduleValue.Table.Get("inspect");
            Assert.That(inspectFunc.Type, Is.EqualTo(DataType.Function));

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
            Assert.That(inspected.Type, Is.EqualTo(DataType.String));

            string output = inspected.String;
            Assert.That(output, Does.Contain("player = {"));
            Assert.That(output, Does.Contain("stats = {"));
            Assert.That(output, Does.Contain("overlay = { -- ui-meta"));
            Assert.That(output, Does.Contain("<metatable> = {"));
            Assert.That(output, Does.Contain("tags = {"));
            Assert.That(output, Does.Contain("\"alpha\""));
            Assert.That(output, Does.Contain("\"beta\""));
        }
    }
}
