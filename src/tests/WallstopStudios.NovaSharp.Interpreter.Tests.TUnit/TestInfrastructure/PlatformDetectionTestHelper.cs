namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    internal static class PlatformDetectionTestHelper
    {
        public static PlatformDetectorOverrideScope ForceFileSystemLoader()
        {
            return PlatformDetectorOverrideScope.ForceFileSystemLoader();
        }
    }
}
