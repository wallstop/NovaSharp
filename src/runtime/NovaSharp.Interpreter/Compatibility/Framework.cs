namespace NovaSharp.Interpreter.Compatibility
{
    using Frameworks;
    using NovaSharp.Interpreter.Compatibility.Frameworks.Base;

    /// <summary>
    /// Provides access to the platform-specific <see cref="FrameworkBase"/> implementation that
    /// NovaSharp should use for reflection and type-system queries at runtime.
    /// </summary>
    public static class Framework
    {
        private static readonly FrameworkCurrent CurrentFramework = new();

        /// <summary>
        /// Gets the active <see cref="FrameworkBase"/> adapter selected for the current runtime.
        /// </summary>
        public static FrameworkBase Do
        {
            get { return CurrentFramework; }
        }
    }
}
