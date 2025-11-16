namespace NovaSharp.Hardwire
{
    using System.Reflection;
    using Generators;

    public static class HardwireGeneratorRegistry
    {
        private static readonly Dictionary<string, IHardwireGenerator> Generators = new();

        public static void Register(IHardwireGenerator g)
        {
            Generators[g.ManagedType] = g;
        }

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

        public static void RegisterPredefined()
        {
            DiscoverFromAssembly(Assembly.GetExecutingAssembly());
        }

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
