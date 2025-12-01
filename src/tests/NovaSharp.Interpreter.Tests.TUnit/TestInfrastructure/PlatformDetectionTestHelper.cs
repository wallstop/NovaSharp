namespace NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    internal static class PlatformDetectionTestHelper
    {
        public static PlatformDetectorOverrideScope ForceFileSystemLoader()
        {
            return PlatformDetectorOverrideScope.ForceFileSystemLoader();
        }
    }
}
