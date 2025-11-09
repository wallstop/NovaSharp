using System.Reflection;
using NovaSharp.Hardwire.Generators;

namespace NovaSharp.Hardwire
{
    public static class HardwireGeneratorRegistry
    {
        static readonly Dictionary<string, IHardwireGenerator> m_Generators = new();

        public static void Register(IHardwireGenerator g)
        {
            m_Generators[g.ManagedType] = g;
        }

        public static IHardwireGenerator GetGenerator(string type)
        {
            if (m_Generators.ContainsKey(type))
            {
                return m_Generators[type];
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
