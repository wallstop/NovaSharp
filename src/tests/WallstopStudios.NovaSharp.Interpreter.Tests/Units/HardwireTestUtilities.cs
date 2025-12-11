namespace WallstopStudios.NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Hardwire;
    using WallstopStudios.NovaSharp.Hardwire.Languages;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors;

    internal static class HardwireTestUtilities
    {
        public static HardwireCodeGenerationContext CreateContext(
            ICodeGenerationLogger logger = null,
            HardwireCodeGenerationLanguage language = null,
            ITimeProvider timeProvider = null
        )
        {
            return new HardwireCodeGenerationContext(
                "NovaSharp.Tests.Generated",
                "HardwireKickstarter",
                logger ?? new CapturingCodeGenerationLogger(),
                language ?? HardwireCodeGenerationLanguage.CSharp,
                timeProvider
            );
        }

        public static Table CreateDescriptorTable(Script script, string visibility)
        {
            Table descriptor = new(script);
            descriptor.Set(
                "class",
                DynValue.NewString(typeof(StandardUserDataDescriptor).FullName)
            );
            descriptor.Set("visibility", DynValue.NewString(visibility));
            descriptor.Set("members", DynValue.NewTable(script));
            descriptor.Set("metamembers", DynValue.NewTable(script));

            Table root = new(script);
            root.Set("Sample", DynValue.NewTable(descriptor));
            return root;
        }
    }

    internal sealed class CapturingCodeGenerationLogger : ICodeGenerationLogger
    {
        public List<string> Errors { get; } = new();

        public List<string> Warnings { get; } = new();

        public List<string> MinorMessages { get; } = new();

        public void LogError(string message)
        {
            Errors.Add(message);
        }

        public void LogWarning(string message)
        {
            Warnings.Add(message);
        }

        public void LogMinor(string message)
        {
            MinorMessages.Add(message);
        }
    }
}
