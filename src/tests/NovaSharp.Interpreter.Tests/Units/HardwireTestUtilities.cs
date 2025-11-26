namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Hardwire;
    using NovaSharp.Hardwire.Languages;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Infrastructure;

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
                DynValue.NewString(
                    "NovaSharp.Interpreter.Interop.StandardDescriptors.StandardUserDataDescriptor"
                )
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
