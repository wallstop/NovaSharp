namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BenchmarkDotNet.Attributes;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Benchmarks script startup and core module registration costs.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class ScriptConstructionBenchmarks
    {
        private Script _registrationScript;

        /// <summary>
        /// Lua compatibility version used by the construction benchmark.
        /// </summary>
        [Params(nameof(LuaCompatibilityVersion.Lua51), nameof(LuaCompatibilityVersion.Lua54))]
        public string VersionName { get; set; } = nameof(LuaCompatibilityVersion.Lua54);

        /// <summary>
        /// Core module preset used by the construction benchmark.
        /// </summary>
        [Params("Basic", "HardSandbox", "Default", "Complete")]
        public string ModulePreset { get; set; } = "Default";

        private LuaCompatibilityVersion CurrentVersion
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(VersionName);
                return Enum.Parse<LuaCompatibilityVersion>(VersionName, ignoreCase: false);
            }
        }

        private CoreModules CurrentModules
        {
            get
            {
                switch (ModulePreset)
                {
                    case "Basic":
                        return CoreModules.Basic;
                    case "HardSandbox":
                        return CoreModulePresets.HardSandbox;
                    case "Default":
                        return CoreModulePresets.Default;
                    case "Complete":
                        return CoreModulePresets.Complete;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ModulePreset));
                }
            }
        }

        /// <summary>
        /// Prepares the script used for table registration benchmarks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _registrationScript = new Script(CurrentVersion, (CoreModules)0);
        }

        /// <summary>
        /// Constructs a script with the selected module preset.
        /// </summary>
        [Benchmark(Description = "Construct Script")]
        public Script ConstructScript()
        {
            return new Script(CurrentVersion, CurrentModules);
        }

        /// <summary>
        /// Registers the selected preset on a fresh global table owned by an existing script.
        /// </summary>
        [Benchmark(Description = "Register Core Modules")]
        public Table RegisterCoreModulesOnFreshTable()
        {
            Table table = new(_registrationScript);
            return table.RegisterCoreModules(CurrentModules);
        }
    }

    /// <summary>
    /// Benchmarks representative single module registrations on an existing script.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class SingleModuleRegistrationBenchmarks
    {
        private Script _registrationScript;

        /// <summary>
        /// Lua compatibility version used by the registration benchmark.
        /// </summary>
        [Params(nameof(LuaCompatibilityVersion.Lua51), nameof(LuaCompatibilityVersion.Lua54))]
        public string VersionName { get; set; } = nameof(LuaCompatibilityVersion.Lua54);

        /// <summary>
        /// Single module type used by the module registration benchmark.
        /// </summary>
        [Params("Basic", "Math", "String", "Load", "Io", "Debug")]
        public string SingleModuleName { get; set; } = "Basic";

        private LuaCompatibilityVersion CurrentVersion
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(VersionName);
                return Enum.Parse<LuaCompatibilityVersion>(VersionName, ignoreCase: false);
            }
        }

        /// <summary>
        /// Prepares the script used for single-module registration benchmarks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _registrationScript = new Script(CurrentVersion, (CoreModules)0);
        }

        /// <summary>
        /// Registers one representative module type on a fresh global table.
        /// </summary>
        [Benchmark(Description = "Register Single Module")]
        public Table RegisterSingleModuleType()
        {
            Table table = new(_registrationScript);
            return table.RegisterModuleType(GetSingleModuleType());
        }

        private Type GetSingleModuleType()
        {
            switch (SingleModuleName)
            {
                case "Basic":
                    return typeof(BasicModule);
                case "Math":
                    return typeof(MathModule);
                case "String":
                    return typeof(StringModule);
                case "Load":
                    return typeof(LoadModule);
                case "Io":
                    return typeof(IoModule);
                case "Debug":
                    return typeof(DebugModule);
                default:
                    throw new ArgumentOutOfRangeException(nameof(SingleModuleName));
            }
        }
    }
}
