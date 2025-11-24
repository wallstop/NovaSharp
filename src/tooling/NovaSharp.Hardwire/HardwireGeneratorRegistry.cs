namespace NovaSharp.Hardwire
{
    using System.Reflection;
    using Generators;

    /// <summary>
    /// Maintains the registry of available hardwire generators and provides discovery helpers.
    /// </summary>
    public static class HardwireGeneratorRegistry
    {
        private static readonly Dictionary<string, IHardwireGenerator> Generators = new();

        /// <summary>
        /// Registers a generator instance for its managed type.
        /// </summary>
        public static void Register(IHardwireGenerator g)
        {
            Generators[g.ManagedType] = g;
        }

        /// <summary>
        /// Retrieves a generator for the requested managed type (returns a placeholder when missing).
        /// </summary>
        public static IHardwireGenerator GetGenerator(string type)
        {
            if (Generators.ContainsKey(type))
            {
                return Generators[type];
            }
            else
            {
                return new NullGenerator(type);
            }
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
            if (asm == null)
            {
                asm = Assembly.GetCallingAssembly();
            }

            foreach (
                Type type in asm.GetTypes()
                    .Where(t => !(t.IsAbstract || t.IsGenericTypeDefinition || t.IsGenericType))
                    .Where(t => (typeof(IHardwireGenerator)).IsAssignableFrom(t))
            )
            {
                IHardwireGenerator g = (IHardwireGenerator)Activator.CreateInstance(type);
                Register(g);
            }
        }
    }
}
