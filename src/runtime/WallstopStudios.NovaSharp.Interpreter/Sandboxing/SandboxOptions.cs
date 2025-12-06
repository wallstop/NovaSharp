namespace WallstopStudios.NovaSharp.Interpreter.Sandboxing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configures sandbox limits for script execution, including instruction ceilings,
    /// recursion depth, and module/function access restrictions.
    /// </summary>
    public sealed class SandboxOptions
    {
        /// <summary>
        /// A shared instance with no limits applied (all execution paths permitted).
        /// </summary>
        public static readonly SandboxOptions Unrestricted = new SandboxOptions();

        private long _maxInstructions;
        private int _maxCallStackDepth;
        private HashSet<string> _restrictedModules;
        private HashSet<string> _restrictedFunctions;

        /// <summary>
        /// Initializes a new <see cref="SandboxOptions"/> with no restrictions.
        /// </summary>
        public SandboxOptions()
        {
            _maxInstructions = 0; // 0 means unlimited
            _maxCallStackDepth = 0; // 0 means unlimited
        }

        /// <summary>
        /// Initializes a new <see cref="SandboxOptions"/> by copying from an existing instance.
        /// </summary>
        /// <param name="defaults">The instance to copy settings from.</param>
        public SandboxOptions(SandboxOptions defaults)
        {
            if (defaults == null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            _maxInstructions = defaults._maxInstructions;
            _maxCallStackDepth = defaults._maxCallStackDepth;
            OnInstructionLimitExceeded = defaults.OnInstructionLimitExceeded;
            OnRecursionLimitExceeded = defaults.OnRecursionLimitExceeded;
            OnModuleAccessDenied = defaults.OnModuleAccessDenied;
            OnFunctionAccessDenied = defaults.OnFunctionAccessDenied;

            if (defaults._restrictedModules != null)
            {
                _restrictedModules = new HashSet<string>(
                    defaults._restrictedModules,
                    StringComparer.Ordinal
                );
            }

            if (defaults._restrictedFunctions != null)
            {
                _restrictedFunctions = new HashSet<string>(
                    defaults._restrictedFunctions,
                    StringComparer.Ordinal
                );
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of VM instructions that may execute before
        /// raising <see cref="SandboxViolationException"/>. Set to 0 for unlimited.
        /// </summary>
        public long MaxInstructions
        {
            get => _maxInstructions;
            set => _maxInstructions = value < 0 ? 0 : value;
        }

        /// <summary>
        /// Gets or sets the maximum call stack depth before raising
        /// <see cref="SandboxViolationException"/>. Set to 0 for unlimited.
        /// </summary>
        public int MaxCallStackDepth
        {
            get => _maxCallStackDepth;
            set => _maxCallStackDepth = value < 0 ? 0 : value;
        }

        /// <summary>
        /// Gets a value indicating whether instruction limiting is enabled.
        /// </summary>
        public bool HasInstructionLimit => _maxInstructions > 0;

        /// <summary>
        /// Gets a value indicating whether call stack depth limiting is enabled.
        /// </summary>
        public bool HasCallStackDepthLimit => _maxCallStackDepth > 0;

        /// <summary>
        /// Gets a value indicating whether any module restrictions are configured.
        /// </summary>
        public bool HasModuleRestrictions =>
            _restrictedModules != null && _restrictedModules.Count > 0;

        /// <summary>
        /// Gets a value indicating whether any function restrictions are configured.
        /// </summary>
        public bool HasFunctionRestrictions =>
            _restrictedFunctions != null && _restrictedFunctions.Count > 0;

        /// <summary>
        /// Gets or sets a callback invoked when the instruction limit is about to be exceeded.
        /// Return <c>true</c> to allow continued execution (e.g., after resetting counters),
        /// or <c>false</c> to throw <see cref="SandboxViolationException"/>.
        /// </summary>
        public Func<Script, long, bool> OnInstructionLimitExceeded { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the recursion limit is about to be exceeded.
        /// Return <c>true</c> to allow the call, or <c>false</c> to throw <see cref="SandboxViolationException"/>.
        /// </summary>
        public Func<Script, int, bool> OnRecursionLimitExceeded { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when a restricted module is accessed.
        /// Return <c>true</c> to allow access, or <c>false</c> to throw <see cref="SandboxViolationException"/>.
        /// </summary>
        public Func<Script, string, bool> OnModuleAccessDenied { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when a restricted function is accessed.
        /// Return <c>true</c> to allow access, or <c>false</c> to throw <see cref="SandboxViolationException"/>.
        /// </summary>
        public Func<Script, string, bool> OnFunctionAccessDenied { get; set; }

        /// <summary>
        /// Adds a module name to the restricted list. Scripts will not be able to
        /// <c>require</c> this module unless <see cref="OnModuleAccessDenied"/> allows it.
        /// </summary>
        /// <param name="moduleName">The module name to restrict (e.g., "io", "os").</param>
        /// <returns>This instance for method chaining.</returns>
        public SandboxOptions RestrictModule(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                throw new ArgumentException(
                    "Module name cannot be null or empty.",
                    nameof(moduleName)
                );
            }

            if (_restrictedModules == null)
            {
                _restrictedModules = new HashSet<string>(StringComparer.Ordinal);
            }

            _restrictedModules.Add(moduleName);
            return this;
        }

        /// <summary>
        /// Adds multiple module names to the restricted list.
        /// </summary>
        /// <param name="moduleNames">The module names to restrict.</param>
        /// <returns>This instance for method chaining.</returns>
        public SandboxOptions RestrictModules(params string[] moduleNames)
        {
            if (moduleNames == null)
            {
                throw new ArgumentNullException(nameof(moduleNames));
            }

            foreach (string name in moduleNames)
            {
                RestrictModule(name);
            }

            return this;
        }

        /// <summary>
        /// Removes a module from the restricted list.
        /// </summary>
        /// <param name="moduleName">The module name to allow.</param>
        /// <returns>This instance for method chaining.</returns>
        public SandboxOptions AllowModule(string moduleName)
        {
            if (_restrictedModules != null && !string.IsNullOrWhiteSpace(moduleName))
            {
                _restrictedModules.Remove(moduleName);
            }

            return this;
        }

        /// <summary>
        /// Checks whether a module is restricted.
        /// </summary>
        /// <param name="moduleName">The module name to check.</param>
        /// <returns><c>true</c> if the module is restricted; otherwise <c>false</c>.</returns>
        public bool IsModuleRestricted(string moduleName)
        {
            return _restrictedModules != null
                && !string.IsNullOrWhiteSpace(moduleName)
                && _restrictedModules.Contains(moduleName);
        }

        /// <summary>
        /// Adds a function name to the restricted list. Scripts will not be able to
        /// call this function unless <see cref="OnFunctionAccessDenied"/> allows it.
        /// </summary>
        /// <param name="functionName">The function name to restrict (e.g., "loadfile", "dofile", "load").</param>
        /// <returns>This instance for method chaining.</returns>
        public SandboxOptions RestrictFunction(string functionName)
        {
            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new ArgumentException(
                    "Function name cannot be null or empty.",
                    nameof(functionName)
                );
            }

            if (_restrictedFunctions == null)
            {
                _restrictedFunctions = new HashSet<string>(StringComparer.Ordinal);
            }

            _restrictedFunctions.Add(functionName);
            return this;
        }

        /// <summary>
        /// Adds multiple function names to the restricted list.
        /// </summary>
        /// <param name="functionNames">The function names to restrict.</param>
        /// <returns>This instance for method chaining.</returns>
        public SandboxOptions RestrictFunctions(params string[] functionNames)
        {
            if (functionNames == null)
            {
                throw new ArgumentNullException(nameof(functionNames));
            }

            foreach (string name in functionNames)
            {
                RestrictFunction(name);
            }

            return this;
        }

        /// <summary>
        /// Removes a function from the restricted list.
        /// </summary>
        /// <param name="functionName">The function name to allow.</param>
        /// <returns>This instance for method chaining.</returns>
        public SandboxOptions AllowFunction(string functionName)
        {
            if (_restrictedFunctions != null && !string.IsNullOrWhiteSpace(functionName))
            {
                _restrictedFunctions.Remove(functionName);
            }

            return this;
        }

        /// <summary>
        /// Checks whether a function is restricted.
        /// </summary>
        /// <param name="functionName">The function name to check.</param>
        /// <returns><c>true</c> if the function is restricted; otherwise <c>false</c>.</returns>
        public bool IsFunctionRestricted(string functionName)
        {
            return _restrictedFunctions != null
                && !string.IsNullOrWhiteSpace(functionName)
                && _restrictedFunctions.Contains(functionName);
        }

        /// <summary>
        /// Gets a read-only view of the restricted module names.
        /// </summary>
        /// <returns>A read-only collection of restricted module names, or an empty collection if none.</returns>
        public IReadOnlyCollection<string> GetRestrictedModules()
        {
            if (_restrictedModules == null)
            {
                return Array.Empty<string>();
            }

            return _restrictedModules;
        }

        /// <summary>
        /// Gets a read-only view of the restricted function names.
        /// </summary>
        /// <returns>A read-only collection of restricted function names, or an empty collection if none.</returns>
        public IReadOnlyCollection<string> GetRestrictedFunctions()
        {
            if (_restrictedFunctions == null)
            {
                return Array.Empty<string>();
            }

            return _restrictedFunctions;
        }

        /// <summary>
        /// Creates a preset for highly restrictive sandboxing: no file I/O, no OS access,
        /// no dynamic code loading.
        /// </summary>
        /// <param name="maxInstructions">Maximum instructions (default: 1,000,000).</param>
        /// <param name="maxCallStackDepth">Maximum call depth (default: 256).</param>
        /// <returns>A new <see cref="SandboxOptions"/> with restrictive settings.</returns>
        public static SandboxOptions CreateRestrictive(
            long maxInstructions = 1_000_000,
            int maxCallStackDepth = 256
        )
        {
            return new SandboxOptions
            {
                MaxInstructions = maxInstructions,
                MaxCallStackDepth = maxCallStackDepth,
            }
                .RestrictModules("io", "os", "debug")
                .RestrictFunctions("loadfile", "dofile", "load", "loadstring");
        }

        /// <summary>
        /// Creates a preset for moderate sandboxing: allows most operations but limits
        /// instruction count and call depth.
        /// </summary>
        /// <param name="maxInstructions">Maximum instructions (default: 10,000,000).</param>
        /// <param name="maxCallStackDepth">Maximum call depth (default: 512).</param>
        /// <returns>A new <see cref="SandboxOptions"/> with moderate settings.</returns>
        public static SandboxOptions CreateModerate(
            long maxInstructions = 10_000_000,
            int maxCallStackDepth = 512
        )
        {
            return new SandboxOptions
            {
                MaxInstructions = maxInstructions,
                MaxCallStackDepth = maxCallStackDepth,
            };
        }
    }
}
