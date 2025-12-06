namespace WallstopStudios.NovaSharp.Hardwire
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Generators;

    /// <summary>
    /// Maintains the registry of available hardwire generators and provides discovery helpers.
    /// </summary>
    public static class HardwireGeneratorRegistry
    {
        private static readonly Dictionary<string, IHardwireGenerator> Generators = new();
        private static readonly object SyncRoot = new();

        /// <summary>
        /// Registers a generator instance for its managed type.
        /// </summary>
        public static void Register(IHardwireGenerator generator)
        {
            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (string.IsNullOrWhiteSpace(generator.ManagedType))
            {
                throw new ArgumentException(
                    "Generators must expose a non-empty managed type.",
                    nameof(generator)
                );
            }

            lock (SyncRoot)
            {
                Generators[generator.ManagedType] = generator;
            }
        }

        /// <summary>
        /// Retrieves a generator for the requested managed type (returns a placeholder when missing).
        /// </summary>
        public static IHardwireGenerator GetGenerator(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Type cannot be null or whitespace.", nameof(type));
            }

            lock (SyncRoot)
            {
                if (Generators.TryGetValue(type, out IHardwireGenerator generator))
                {
                    return generator;
                }
            }

            return new NullGenerator(type);
        }

        /// <summary>
        /// Registers all generators defined in the current assembly.
        /// </summary>
        public static void RegisterPredefined()
        {
            DiscoverFromAssembly(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Discovers and registers generators from <paramref name="asm"/> (or the calling assembly when null).
        /// </summary>
        public static void DiscoverFromAssembly(Assembly asm = null)
        {
            Assembly assemblyToScan = asm ?? Assembly.GetCallingAssembly();

            foreach (
                Type type in assemblyToScan
                    .GetTypes()
                    .Where(t => !(t.IsAbstract || t.IsGenericTypeDefinition || t.IsGenericType))
                    .Where(t => typeof(IHardwireGenerator).IsAssignableFrom(t))
            )
            {
                if (Activator.CreateInstance(type) is not IHardwireGenerator generator)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(generator.ManagedType))
                {
                    continue;
                }

                Register(generator);
            }
        }

        /// <summary>
        /// Clears all registered generators. Intended for test isolation.
        /// </summary>
        internal static void Reset()
        {
            lock (SyncRoot)
            {
                Generators.Clear();
            }
        }
    }
}
