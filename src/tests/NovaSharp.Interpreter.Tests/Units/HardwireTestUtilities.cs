namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Hardwire;
    using NovaSharp.Hardwire.Languages;

    internal static class HardwireTestUtilities
    {
        public static HardwireCodeGenerationContext CreateContext(
            ICodeGenerationLogger logger = null,
            HardwireCodeGenerationLanguage language = null
        )
        {
            return new HardwireCodeGenerationContext(
                "NovaSharp.Tests.Generated",
                "HardwireKickstarter",
                logger ?? new CapturingCodeGenerationLogger(),
                language ?? HardwireCodeGenerationLanguage.CSharp
            );
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
