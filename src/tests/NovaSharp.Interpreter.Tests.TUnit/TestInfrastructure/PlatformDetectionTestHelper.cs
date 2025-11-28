namespace NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using NovaSharp.Interpreter.Platforms;

    internal static class PlatformDetectionTestHelper
    {
        public static void ForceFileSystemLoader()
        {
            PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(false);
            PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(false);
        }
    }
}
