namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Manages a collection of mod containers, providing coordinated loading, unloading,
    /// and inter-mod communication capabilities.
    /// </summary>
    public sealed class ModManager
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, IModContainer> _mods;
        private readonly List<string> _loadOrder;
        private readonly Dictionary<string, HashSet<string>> _dependencies;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModManager"/> class.
        /// </summary>
        public ModManager()
        {
            _mods = new Dictionary<string, IModContainer>(StringComparer.Ordinal);
            _loadOrder = new List<string>();
            _dependencies = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets the number of registered mods.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _mods.Count;
                }
            }
        }

        /// <summary>
        /// Gets all registered mod IDs.
        /// </summary>
        public IReadOnlyList<string> ModIds
        {
            get
            {
                lock (_lock)
                {
                    return new List<string>(_loadOrder);
                }
            }
        }

        /// <summary>
        /// Occurs when a mod is registered.
        /// </summary>
        public event EventHandler<ModEventArgs> OnModRegistered;

        /// <summary>
        /// Occurs when a mod is unregistered.
        /// </summary>
        public event EventHandler<ModEventArgs> OnModUnregistered;

        /// <summary>
        /// Occurs when all mods have finished loading.
        /// </summary>
        public event EventHandler OnAllModsLoaded;

        /// <summary>
        /// Occurs when all mods have finished unloading.
        /// </summary>
        public event EventHandler OnAllModsUnloaded;

        /// <summary>
        /// Registers a mod container with the manager.
        /// </summary>
        /// <param name="mod">The mod container to register.</param>
        /// <returns><c>true</c> if the mod was registered; <c>false</c> if a mod with the same ID already exists.</returns>
        public bool Register(IModContainer mod)
        {
            if (mod == null)
            {
                throw new ArgumentNullException(nameof(mod));
            }

            lock (_lock)
            {
                if (_mods.ContainsKey(mod.ModId))
                {
                    return false;
                }

                _mods[mod.ModId] = mod;
                _loadOrder.Add(mod.ModId);
                _dependencies[mod.ModId] = new HashSet<string>(StringComparer.Ordinal);
            }

            RaiseEvent(OnModRegistered, new ModEventArgs(mod, mod.State));
            return true;
        }

        /// <summary>
        /// Unregisters a mod container from the manager.
        /// If the mod is loaded, it will be unloaded first.
        /// </summary>
        /// <param name="modId">The ID of the mod to unregister.</param>
        /// <returns><c>true</c> if the mod was unregistered; <c>false</c> if no mod with the ID exists.</returns>
        public bool Unregister(string modId)
        {
            if (string.IsNullOrEmpty(modId))
            {
                throw new ArgumentException("Mod ID cannot be null or empty.", nameof(modId));
            }

            IModContainer mod;
            lock (_lock)
            {
                if (!_mods.TryGetValue(modId, out mod))
                {
                    return false;
                }
            }

            // Unload if needed (outside lock)
            if (mod.State == ModLoadState.Loaded)
            {
                mod.Unload();
            }

            lock (_lock)
            {
                _mods.Remove(modId);
                _loadOrder.Remove(modId);
                _dependencies.Remove(modId);

                // Remove this mod from other mods' dependencies
                foreach (HashSet<string> deps in _dependencies.Values)
                {
                    deps.Remove(modId);
                }
            }

            RaiseEvent(OnModUnregistered, new ModEventArgs(mod, mod.State));
            return true;
        }

        /// <summary>
        /// Gets a registered mod container by ID.
        /// </summary>
        /// <param name="modId">The ID of the mod.</param>
        /// <returns>The mod container, or <c>null</c> if not found.</returns>
        public IModContainer GetMod(string modId)
        {
            if (string.IsNullOrEmpty(modId))
            {
                return null;
            }

            lock (_lock)
            {
                _mods.TryGetValue(modId, out IModContainer mod);
                return mod;
            }
        }

        /// <summary>
        /// Tries to get a registered mod container by ID.
        /// </summary>
        /// <param name="modId">The ID of the mod.</param>
        /// <param name="mod">The mod container if found.</param>
        /// <returns><c>true</c> if the mod was found; otherwise <c>false</c>.</returns>
        public bool TryGetMod(string modId, out IModContainer mod)
        {
            if (string.IsNullOrEmpty(modId))
            {
                mod = null;
                return false;
            }

            lock (_lock)
            {
                return _mods.TryGetValue(modId, out mod);
            }
        }

        /// <summary>
        /// Checks if a mod is registered.
        /// </summary>
        /// <param name="modId">The ID of the mod.</param>
        /// <returns><c>true</c> if the mod is registered; otherwise <c>false</c>.</returns>
        public bool Contains(string modId)
        {
            if (string.IsNullOrEmpty(modId))
            {
                return false;
            }

            lock (_lock)
            {
                return _mods.ContainsKey(modId);
            }
        }

        /// <summary>
        /// Declares that one mod depends on another.
        /// Dependencies affect load order: dependencies are loaded first.
        /// </summary>
        /// <param name="modId">The ID of the dependent mod.</param>
        /// <param name="dependsOnModId">The ID of the mod that <paramref name="modId"/> depends on.</param>
        /// <returns><c>true</c> if the dependency was added; <c>false</c> if either mod doesn't exist or would create a cycle.</returns>
        public bool AddDependency(string modId, string dependsOnModId)
        {
            if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(dependsOnModId))
            {
                return false;
            }

            if (string.Equals(modId, dependsOnModId, StringComparison.Ordinal))
            {
                return false; // Cannot depend on self
            }

            lock (_lock)
            {
                if (!_mods.ContainsKey(modId) || !_mods.ContainsKey(dependsOnModId))
                {
                    return false;
                }

                // Check for cycle
                if (WouldCreateCycle(modId, dependsOnModId))
                {
                    return false;
                }

                _dependencies[modId].Add(dependsOnModId);
                return true;
            }
        }

        /// <summary>
        /// Gets the IDs of mods that a given mod depends on.
        /// </summary>
        /// <param name="modId">The ID of the mod.</param>
        /// <returns>The dependency IDs, or an empty list if the mod doesn't exist.</returns>
        public IReadOnlyList<string> GetDependencies(string modId)
        {
            if (string.IsNullOrEmpty(modId))
            {
                return Array.Empty<string>();
            }

            lock (_lock)
            {
                if (_dependencies.TryGetValue(modId, out HashSet<string> deps))
                {
                    return new List<string>(deps);
                }

                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Loads all registered mods in dependency order.
        /// </summary>
        /// <returns>A dictionary mapping mod IDs to their load results.</returns>
        public IDictionary<string, ModOperationResult> LoadAll()
        {
            IReadOnlyList<string> sortedOrder = GetLoadOrder();
            Dictionary<string, ModOperationResult> results = new Dictionary<
                string,
                ModOperationResult
            >(StringComparer.Ordinal);

            foreach (string modId in sortedOrder)
            {
                IModContainer mod = GetMod(modId);
                if (mod == null)
                {
                    continue;
                }

                // Skip if already loaded
                if (mod.State == ModLoadState.Loaded)
                {
                    results[modId] = ModOperationResult.Succeeded(
                        ModLoadState.Loaded,
                        "Already loaded."
                    );
                    continue;
                }

                // Check if dependencies are loaded
                if (!AreDependenciesLoaded(modId))
                {
                    results[modId] = ModOperationResult.Failed(
                        mod.State,
                        "One or more dependencies failed to load."
                    );
                    continue;
                }

                results[modId] = mod.Load();
            }

            OnAllModsLoaded?.Invoke(this, EventArgs.Empty);
            return results;
        }

        /// <summary>
        /// Unloads all registered mods in reverse dependency order.
        /// </summary>
        /// <returns>A dictionary mapping mod IDs to their unload results.</returns>
        public IDictionary<string, ModOperationResult> UnloadAll()
        {
            IReadOnlyList<string> sortedOrder = GetLoadOrder();
            Dictionary<string, ModOperationResult> results = new Dictionary<
                string,
                ModOperationResult
            >(StringComparer.Ordinal);

            // Iterate in reverse for unload (process dependents before their dependencies)
            for (int i = sortedOrder.Count - 1; i >= 0; i--)
            {
                string modId = sortedOrder[i];
                IModContainer mod = GetMod(modId);
                if (mod == null)
                {
                    continue;
                }

                // Skip if already unloaded
                if (mod.State == ModLoadState.Unloaded)
                {
                    results[modId] = ModOperationResult.Succeeded(
                        ModLoadState.Unloaded,
                        "Already unloaded."
                    );
                    continue;
                }

                results[modId] = mod.Unload();
            }

            OnAllModsUnloaded?.Invoke(this, EventArgs.Empty);
            return results;
        }

        /// <summary>
        /// Reloads all loaded mods in dependency order.
        /// </summary>
        /// <returns>A dictionary mapping mod IDs to their reload results.</returns>
        public IDictionary<string, ModOperationResult> ReloadAll()
        {
            IReadOnlyList<string> sortedOrder = GetLoadOrder();
            Dictionary<string, ModOperationResult> results = new Dictionary<
                string,
                ModOperationResult
            >(StringComparer.Ordinal);

            // First unload in reverse order
            for (int i = sortedOrder.Count - 1; i >= 0; i--)
            {
                string modId = sortedOrder[i];
                IModContainer mod = GetMod(modId);
                if (mod != null && mod.State == ModLoadState.Loaded)
                {
                    mod.Unload();
                }
            }

            // Then load in forward order
            foreach (string modId in sortedOrder)
            {
                IModContainer mod = GetMod(modId);
                if (mod == null)
                {
                    continue;
                }

                // Check if dependencies are loaded
                if (!AreDependenciesLoaded(modId))
                {
                    results[modId] = ModOperationResult.Failed(
                        mod.State,
                        "One or more dependencies failed to load."
                    );
                    continue;
                }

                results[modId] = mod.Load();
            }

            return results;
        }

        /// <summary>
        /// Calls a function on all loaded mods that have it defined.
        /// </summary>
        /// <param name="functionName">The name of the function to call.</param>
        /// <param name="args">Arguments to pass to the function.</param>
        /// <returns>A dictionary mapping mod IDs to their return values (or exceptions).</returns>
        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "BroadcastCall must capture all errors to report them per-mod"
        )]
        public IDictionary<string, DynValue> BroadcastCall(
            string functionName,
            params object[] args
        )
        {
            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException(
                    "Function name cannot be null or empty.",
                    nameof(functionName)
                );
            }

            IReadOnlyList<string> modIds = GetLoadOrder();
            Dictionary<string, DynValue> results = new Dictionary<string, DynValue>(
                StringComparer.Ordinal
            );

            foreach (string modId in modIds)
            {
                IModContainer mod = GetMod(modId);
                if (mod == null || mod.State != ModLoadState.Loaded)
                {
                    continue;
                }

                DynValue func = mod.GetGlobal(functionName);
                if (func.Type != DataType.Function)
                {
                    continue;
                }

                try
                {
                    results[modId] = mod.CallFunction(functionName, args);
                }
                catch (Exception ex)
                {
                    // Store error as string for debugging
                    results[modId] = DynValue.NewString($"Error: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// Gets a value from a specific mod's globals.
        /// </summary>
        /// <param name="modId">The ID of the mod.</param>
        /// <param name="globalName">The name of the global.</param>
        /// <returns>The value, or <see cref="DynValue.Nil"/> if not found or mod not loaded.</returns>
        public DynValue GetModGlobal(string modId, string globalName)
        {
            if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(globalName))
            {
                return DynValue.Nil;
            }

            IModContainer mod = GetMod(modId);
            if (mod == null || mod.State != ModLoadState.Loaded)
            {
                return DynValue.Nil;
            }

            return mod.GetGlobal(globalName);
        }

        /// <summary>
        /// Gets the load order respecting dependencies (topological sort).
        /// </summary>
        /// <returns>A collection of mod IDs in dependency order.</returns>
        public IReadOnlyList<string> GetLoadOrder()
        {
            lock (_lock)
            {
                // Kahn's algorithm for topological sort
                Dictionary<string, int> inDegree = new Dictionary<string, int>(
                    StringComparer.Ordinal
                );
                Dictionary<string, List<string>> graph = new Dictionary<string, List<string>>(
                    StringComparer.Ordinal
                );

                // Initialize
                foreach (string modId in _loadOrder)
                {
                    inDegree[modId] = 0;
                    graph[modId] = new List<string>();
                }

                // Build reverse graph (dependents)
                foreach (KeyValuePair<string, HashSet<string>> kvp in _dependencies)
                {
                    string dependent = kvp.Key;
                    foreach (string dependency in kvp.Value)
                    {
                        if (graph.TryGetValue(dependency, out List<string> dependentList))
                        {
                            dependentList.Add(dependent);
                            inDegree[dependent]++;
                        }
                    }
                }

                // Find nodes with no dependencies
                Queue<string> queue = new Queue<string>();
                foreach (string modId in _loadOrder)
                {
                    if (inDegree[modId] == 0)
                    {
                        queue.Enqueue(modId);
                    }
                }

                List<string> result = new List<string>();
                while (queue.Count > 0)
                {
                    string current = queue.Dequeue();
                    result.Add(current);

                    foreach (string dependent in graph[current])
                    {
                        inDegree[dependent]--;
                        if (inDegree[dependent] == 0)
                        {
                            queue.Enqueue(dependent);
                        }
                    }
                }

                // If not all mods are in result, there's a cycle (shouldn't happen due to cycle check)
                // Fall back to registration order for any remaining
                foreach (string modId in _loadOrder)
                {
                    if (!result.Contains(modId))
                    {
                        result.Add(modId);
                    }
                }

                return result;
            }
        }

        private bool AreDependenciesLoaded(string modId)
        {
            lock (_lock)
            {
                if (!_dependencies.TryGetValue(modId, out HashSet<string> deps))
                {
                    return true;
                }

                foreach (string depId in deps)
                {
                    if (_mods.TryGetValue(depId, out IModContainer dep))
                    {
                        if (dep.State != ModLoadState.Loaded)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // Missing dependency
                    }
                }

                return true;
            }
        }

        private bool WouldCreateCycle(string from, string to)
        {
            // Check if 'to' already depends on 'from' (directly or transitively)
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(to);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (string.Equals(current, from, StringComparison.Ordinal))
                {
                    return true; // Would create cycle
                }

                if (!visited.Add(current))
                {
                    continue;
                }

                if (_dependencies.TryGetValue(current, out HashSet<string> deps))
                {
                    foreach (string dep in deps)
                    {
                        queue.Enqueue(dep);
                    }
                }
            }

            return false;
        }

        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Event handlers must not throw; errors are intentionally swallowed"
        )]
        private void RaiseEvent(EventHandler<ModEventArgs> handler, ModEventArgs args)
        {
            try
            {
                handler?.Invoke(this, args);
            }
            catch
            {
                // Ignore errors in event handlers
            }
        }
    }
}
