namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BenchmarkDotNet.Attributes;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// High-level scenarios for string pattern matching benchmarks.
    /// </summary>
    internal enum StringPatternScenario
    {
        [Obsolete("Use a specific StringPatternScenario.", false)]
        Unknown = 0,
        MatchSimple = 1,
        MatchComplex = 2,
        GsubSimple = 3,
        GsubWithCaptures = 4,
        FormatMultiple = 5,
    }

    /// <summary>
    /// Provides ready-to-run Lua scripts that stress string pattern matching behaviors.
    /// </summary>
    internal static class StringPatternSuites
    {
        /// <summary>
        /// Number of iterations per benchmark call for stable measurements.
        /// </summary>
        public const int Iterations = 100;

        /// <summary>
        /// Returns the Lua script associated with the requested <paramref name="scenario"/>.
        /// </summary>
        /// <param name="scenario">Scenario enumerating the string pattern behavior to benchmark.</param>
        /// <returns>Lua script text suitable for <c>Script.LoadString</c>.</returns>
        public static string GetScript(StringPatternScenario scenario) =>
            scenario switch
            {
                StringPatternScenario.MatchSimple => MatchSimpleScript,
                StringPatternScenario.MatchComplex => MatchComplexScript,
                StringPatternScenario.GsubSimple => GsubSimpleScript,
                StringPatternScenario.GsubWithCaptures => GsubWithCapturesScript,
                StringPatternScenario.FormatMultiple => FormatMultipleScript,
                _ => MatchSimpleScript,
            };

        /// <summary>
        /// Simple pattern matching: find all numbers in a string.
        /// Uses a moderately sized input string with multiple numeric patterns.
        /// </summary>
        private static readonly string MatchSimpleScript =
            $@"
return function()
    local s = ""The quick brown fox has 123 legs and 456 ears. It can run 789 miles per hour. "" ..
              ""Its friend has 234 legs, 567 ears, and can run 890 miles. "" ..
              ""Together they have visited 1234 countries and 5678 cities across 9012 continents.""
    local m
    for i = 1, {Iterations} do
        m = string.match(s, ""%d+"")
    end
    return m
end";

        /// <summary>
        /// Complex pattern matching: parse key-value pairs with captures.
        /// Tests capture group handling and more sophisticated pattern syntax.
        /// </summary>
        private static readonly string MatchComplexScript =
            $@"
return function()
    local s = ""name = John, age = 42, score = 98, level = 7, health = 100, mana = 50, "" ..
              ""strength = 25, dexterity = 18, wisdom = 22, charisma = 15, luck = 12, "" ..
              ""gold = 1500, experience = 45000, kills = 127, deaths = 3, assists = 89""
    local key, value
    for i = 1, {Iterations} do
        key, value = string.match(s, ""(%a+)%s*=%s*(%d+)"")
    end
    return key .. ""="" .. value
end";

        /// <summary>
        /// Simple gsub: replace a pattern with a fixed string.
        /// Tests basic substitution without capture groups.
        /// </summary>
        private static readonly string GsubSimpleScript =
            $@"
return function()
    local template = ""hello world, hello universe, hello galaxy, hello cosmos, "" ..
                     ""hello dimension, hello multiverse, hello reality, hello existence, "" ..
                     ""hello void, hello infinity, hello eternity, hello beyond""
    local result
    for i = 1, {Iterations} do
        result = string.gsub(template, ""hello"", ""greetings"")
    end
    return result
end";

        /// <summary>
        /// Gsub with capture groups: transform key-value pairs.
        /// Tests capture group handling in replacement strings.
        /// </summary>
        private static readonly string GsubWithCapturesScript =
            $@"
return function()
    local s = ""name=John age=42 score=98 level=7 health=100 mana=50 "" ..
              ""strength=25 dexterity=18 wisdom=22 charisma=15 luck=12 "" ..
              ""gold=1500 experience=45000 kills=127 deaths=3 assists=89""
    local result
    for i = 1, {Iterations} do
        result = string.gsub(s, ""(%a+)=(%d+)"", ""[%1: %2]"")
    end
    return result
end";

        /// <summary>
        /// Format with multiple specifiers: test string.format performance.
        /// Uses various format specifiers (%s, %d, %.Nf, %x, %o, %e).
        /// </summary>
        private static readonly string FormatMultipleScript =
            $@"
return function()
    local result
    for i = 1, {Iterations} do
        result = string.format(
            ""Name: %s, Age: %d, Score: %.2f, Hex: %x, Oct: %o, Sci: %.3e, Pct: %d%%"",
            ""TestUser"", 42, 98.765, 255, 64, 12345.6789, 85
        )
    end
    return result
end";
    }

    /// <summary>
    /// BenchmarkDotNet suite that measures string pattern matching allocation patterns.
    /// This establishes baseline measurements for the KopiLua string library before optimization.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class StringPatternBenchmarks
    {
        private Script _script = null!;
        private DynValue _compiledEntry = DynValue.Nil;
        private Func<DynValue> _scenarioRunner = null!;

        /// <summary>
        /// Scenario that will be executed for the next benchmark iteration.
        /// </summary>
        [Params(
            nameof(StringPatternScenario.MatchSimple),
            nameof(StringPatternScenario.MatchComplex),
            nameof(StringPatternScenario.GsubSimple),
            nameof(StringPatternScenario.GsubWithCaptures),
            nameof(StringPatternScenario.FormatMultiple)
        )]
        public string ScenarioName { get; set; } = nameof(StringPatternScenario.MatchSimple);

        private StringPatternScenario CurrentScenario
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(ScenarioName);
                return Enum.Parse<StringPatternScenario>(ScenarioName, ignoreCase: false);
            }
        }

        /// <summary>
        /// Compiles the scenario script and prepares the helpers before the benchmark run.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            StringPatternScenario scenario = CurrentScenario;

            _script = new Script(CoreModulePresets.Complete);
            _compiledEntry = _script.LoadString(
                StringPatternSuites.GetScript(scenario),
                null,
                $"string_pattern_{scenario}"
            );

            // All scenarios return a function that we call directly
            _scenarioRunner = () => _script.Call(_script.Call(_compiledEntry));
        }

        /// <summary>
        /// Executes the selected string pattern scenario and returns its result.
        /// </summary>
        [Benchmark(Description = "String Pattern Scenario")]
        public DynValue ExecuteScenario() => _scenarioRunner();
    }
}
