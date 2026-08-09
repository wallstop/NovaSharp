namespace WallstopStudios.NovaSharp.Interpreter.Interop
{
    /// <summary>
    /// Compatibility alias for presence-aware userdata descriptors.
    /// </summary>
    /// <remarks>
    /// Presence-aware access is now part of <see cref="IUserDataDescriptor" /> itself. This alias
    /// remains so existing capability declarations continue to compile at the struct ABI boundary.
    /// </remarks>
    public interface IUserDataDescriptorTryAccess : IUserDataDescriptor { }
}
