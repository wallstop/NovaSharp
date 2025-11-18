namespace NovaSharp.Interpreter.Compatibility
{
    using Frameworks;
    using NovaSharp.Interpreter.Compatibility.Frameworks.Base;

    public static class Framework
    {
        private static readonly FrameworkCurrent CurrentFramework = new();

        public static FrameworkBase Do
        {
            get { return CurrentFramework; }
        }
    }
}
