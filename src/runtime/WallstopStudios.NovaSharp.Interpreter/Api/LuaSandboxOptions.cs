namespace NovaSharp
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Facade sandbox limits and deny lists.
    /// </summary>
    public sealed class LuaSandboxOptions
    {
        private readonly HashSet<string> _restrictedModules;
        private readonly HashSet<string> _restrictedFunctions;
        private long _maxInstructions;
        private int _maxCallStackDepth;
        private long _maxMemoryBytes;
        private int _maxCoroutines;

        /// <summary>
        /// Initializes unrestricted sandbox options.
        /// </summary>
        public LuaSandboxOptions()
        {
            _restrictedModules = new HashSet<string>(StringComparer.Ordinal);
            _restrictedFunctions = new HashSet<string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Initializes sandbox options by copying an existing instance.
        /// </summary>
        public LuaSandboxOptions(LuaSandboxOptions defaults)
            : this()
        {
            if (defaults == null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            MaxInstructions = defaults.MaxInstructions;
            MaxCallStackDepth = defaults.MaxCallStackDepth;
            MaxMemoryBytes = defaults.MaxMemoryBytes;
            MaxCoroutines = defaults.MaxCoroutines;
            foreach (string moduleName in defaults._restrictedModules)
            {
                _restrictedModules.Add(moduleName);
            }

            foreach (string functionName in defaults._restrictedFunctions)
            {
                _restrictedFunctions.Add(functionName);
            }
        }

        /// <summary>
        /// Gets a new unrestricted sandbox options instance.
        /// </summary>
        public static LuaSandboxOptions Unrestricted => new LuaSandboxOptions();

        /// <summary>
        /// Gets or sets the maximum VM instructions before sandbox violation. Zero means unlimited.
        /// </summary>
        public long MaxInstructions
        {
            get { return _maxInstructions; }
            set { _maxInstructions = value < 0 ? 0 : value; }
        }

        /// <summary>
        /// Gets or sets the maximum call stack depth before sandbox violation. Zero means unlimited.
        /// </summary>
        public int MaxCallStackDepth
        {
            get { return _maxCallStackDepth; }
            set { _maxCallStackDepth = value < 0 ? 0 : value; }
        }

        /// <summary>
        /// Gets or sets the maximum tracked script memory before sandbox violation. Zero means unlimited.
        /// </summary>
        public long MaxMemoryBytes
        {
            get { return _maxMemoryBytes; }
            set { _maxMemoryBytes = value < 0 ? 0 : value; }
        }

        /// <summary>
        /// Gets or sets the maximum concurrent coroutine count before sandbox violation. Zero means unlimited.
        /// </summary>
        public int MaxCoroutines
        {
            get { return _maxCoroutines; }
            set { _maxCoroutines = value < 0 ? 0 : value; }
        }

        /// <summary>
        /// Gets whether instruction limiting is enabled.
        /// </summary>
        public bool HasInstructionLimit => MaxInstructions > 0;

        /// <summary>
        /// Gets whether call stack depth limiting is enabled.
        /// </summary>
        public bool HasCallStackDepthLimit => MaxCallStackDepth > 0;

        /// <summary>
        /// Gets whether memory limiting is enabled.
        /// </summary>
        public bool HasMemoryLimit => MaxMemoryBytes > 0;

        /// <summary>
        /// Gets whether coroutine limiting is enabled.
        /// </summary>
        public bool HasCoroutineLimit => MaxCoroutines > 0;

        /// <summary>
        /// Gets whether any module deny-list entries are configured.
        /// </summary>
        public bool HasModuleRestrictions => _restrictedModules.Count > 0;

        /// <summary>
        /// Gets whether any function deny-list entries are configured.
        /// </summary>
        public bool HasFunctionRestrictions => _restrictedFunctions.Count > 0;

        /// <summary>
        /// Gets the denied module names.
        /// </summary>
        public IReadOnlyCollection<string> RestrictedModules => Snapshot(_restrictedModules);

        /// <summary>
        /// Gets the denied function names.
        /// </summary>
        public IReadOnlyCollection<string> RestrictedFunctions => Snapshot(_restrictedFunctions);

        /// <summary>
        /// Creates restrictive sandbox options for untrusted scripts.
        /// </summary>
        public static LuaSandboxOptions CreateRestrictive(
            long maxInstructions = 1_000_000,
            int maxCallStackDepth = 256
        )
        {
            return new LuaSandboxOptions
            {
                MaxInstructions = maxInstructions,
                MaxCallStackDepth = maxCallStackDepth,
            }
                .RestrictModules("io", "os", "debug")
                .RestrictFunctions("loadfile", "dofile", "load", "loadstring");
        }

        /// <summary>
        /// Creates moderate sandbox options with instruction and stack limits.
        /// </summary>
        public static LuaSandboxOptions CreateModerate(
            long maxInstructions = 10_000_000,
            int maxCallStackDepth = 512
        )
        {
            return new LuaSandboxOptions
            {
                MaxInstructions = maxInstructions,
                MaxCallStackDepth = maxCallStackDepth,
            };
        }

        /// <summary>
        /// Adds a module name to the deny list.
        /// </summary>
        public LuaSandboxOptions RestrictModule(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                throw new ArgumentException(
                    "Module name cannot be null or empty.",
                    nameof(moduleName)
                );
            }

            _restrictedModules.Add(moduleName);
            return this;
        }

        /// <summary>
        /// Adds module names to the deny list.
        /// </summary>
        public LuaSandboxOptions RestrictModules(params string[] moduleNames)
        {
            if (moduleNames == null)
            {
                throw new ArgumentNullException(nameof(moduleNames));
            }

            foreach (string moduleName in moduleNames)
            {
                RestrictModule(moduleName);
            }

            return this;
        }

        /// <summary>
        /// Removes a module name from the deny list.
        /// </summary>
        public LuaSandboxOptions AllowModule(string moduleName)
        {
            if (!string.IsNullOrWhiteSpace(moduleName))
            {
                _restrictedModules.Remove(moduleName);
            }

            return this;
        }

        /// <summary>
        /// Checks whether a module name is denied.
        /// </summary>
        public bool IsModuleRestricted(string moduleName)
        {
            return !string.IsNullOrWhiteSpace(moduleName)
                && _restrictedModules.Contains(moduleName);
        }

        /// <summary>
        /// Adds a function name to the deny list.
        /// </summary>
        public LuaSandboxOptions RestrictFunction(string functionName)
        {
            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new ArgumentException(
                    "Function name cannot be null or empty.",
                    nameof(functionName)
                );
            }

            _restrictedFunctions.Add(functionName);
            return this;
        }

        /// <summary>
        /// Adds function names to the deny list.
        /// </summary>
        public LuaSandboxOptions RestrictFunctions(params string[] functionNames)
        {
            if (functionNames == null)
            {
                throw new ArgumentNullException(nameof(functionNames));
            }

            foreach (string functionName in functionNames)
            {
                RestrictFunction(functionName);
            }

            return this;
        }

        /// <summary>
        /// Removes a function name from the deny list.
        /// </summary>
        public LuaSandboxOptions AllowFunction(string functionName)
        {
            if (!string.IsNullOrWhiteSpace(functionName))
            {
                _restrictedFunctions.Remove(functionName);
            }

            return this;
        }

        /// <summary>
        /// Checks whether a function name is denied.
        /// </summary>
        public bool IsFunctionRestricted(string functionName)
        {
            return !string.IsNullOrWhiteSpace(functionName)
                && _restrictedFunctions.Contains(functionName);
        }

        /// <summary>
        /// Converts facade sandbox options to the current VM sandbox options.
        /// </summary>
        internal SandboxOptions ToSandboxOptions()
        {
            SandboxOptions options = new SandboxOptions
            {
                MaxInstructions = MaxInstructions,
                MaxCallStackDepth = MaxCallStackDepth,
                MaxMemoryBytes = MaxMemoryBytes,
                MaxCoroutines = MaxCoroutines,
            };

            foreach (string moduleName in _restrictedModules)
            {
                options.RestrictModule(moduleName);
            }

            foreach (string functionName in _restrictedFunctions)
            {
                options.RestrictFunction(functionName);
            }

            return options;
        }

        private static string[] Snapshot(HashSet<string> values)
        {
            if (values.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] snapshot = new string[values.Count];
            values.CopyTo(snapshot);
            return snapshot;
        }
    }
}
