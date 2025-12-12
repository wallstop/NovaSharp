namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    internal static class PlatformDetectionTestHelper
    {
        /// <summary>
        /// Forces the platform detector to use the file system loader.
        /// </summary>
        /// <remarks>
        /// Tests using this method should have both <c>[PlatformDetectorIsolation]</c> and
        /// <c>[ScriptDefaultOptionsIsolation]</c> attributes applied to ensure proper isolation.
        /// </remarks>
        /// <returns>A scope that restores the original settings when disposed.</returns>
        public static PlatformDetectorOverrideScope ForceFileSystemLoader()
        {
            return PlatformDetectorOverrideScope.ForceFileSystemLoader();
        }
    }
}
