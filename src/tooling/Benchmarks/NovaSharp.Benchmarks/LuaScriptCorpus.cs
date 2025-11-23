namespace NovaSharp.Benchmarks
{
    using System;
    using System.Text;

    /// <summary>
    /// Describes the relative size/complexity of scripts used when benchmarking compilation performance.
    /// </summary>
    public enum ScriptComplexity
    {
        [Obsolete("Use a specific ScriptComplexity.", false)]
        Unknown = 0,
        Tiny = 1,
        Small = 2,
        Medium = 3,
        Large = 4,
    }

    /// <summary>
    /// Supplies canned Lua scripts covering multiple complexity levels for compile-time benchmarks.
    /// </summary>
    internal static class LuaScriptCorpus
    {
        private const string TinyScript = "return 1 + 1";

        private static readonly string SmallScript = string.Join(
            '\n',
            "local sum = 0",
            "for i = 1, 64 do",
            "    sum = sum + (i % 7) * 1.5",
            "end",
            "return sum"
        );

        private static readonly string MediumScript = BuildScript(6, 128);

        private static readonly string LargeScript = BuildScript(20, 320);

        /// <summary>
        /// Returns a Lua script tuned for the requested <paramref name="complexity"/>.
        /// </summary>
        /// <param name="complexity">Target complexity level.</param>
        /// <returns>Lua script text that exercises the parser and compiler.</returns>
        public static string GetCompilationScript(ScriptComplexity complexity) =>
            complexity switch
            {
                ScriptComplexity.Tiny => TinyScript,
                ScriptComplexity.Small => SmallScript,
                ScriptComplexity.Medium => MediumScript,
                ScriptComplexity.Large => LargeScript,
                _ => TinyScript,
            };

        /// <summary>
        /// Builds a synthetic Lua script with the requested number of helper functions/loop iterations.
        /// </summary>
        private static string BuildScript(int functionCount, int loopIterations)
        {
            StringBuilder builder = new();

            builder.AppendLine("local total = 0");
            builder.AppendLine("local mt = { __index = function(tbl, key) return key * 2 end }");
            builder.AppendLine("local container = setmetatable({}, mt)");

            for (int f = 1; f <= functionCount; f++)
            {
                builder.AppendLine(FormattableString.Invariant($"local function func_{f}(seed)"));
                builder.AppendLine("    local value = seed");
                builder.AppendLine(
                    FormattableString.Invariant($"    for i = 1, {loopIterations} do")
                );
                builder.AppendLine("        value = value + math.sin(i + seed) * 0.125");
                builder.AppendLine("        value = value + container[i % 32]");
                builder.AppendLine("    end");
                builder.AppendLine("    return value");
                builder.AppendLine("end");
            }

            builder.AppendLine(FormattableString.Invariant($"for i = 1, {loopIterations} do"));
            for (int f = 1; f <= functionCount; f++)
            {
                builder.AppendLine(FormattableString.Invariant($"    total = total + func_{f}(i)"));
            }

            builder.AppendLine("    total = total % 1024");
            builder.AppendLine("end");
            builder.AppendLine("return total");

            return builder.ToString();
        }
    }
}
