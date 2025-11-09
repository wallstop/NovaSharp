namespace NovaSharp.Interpreter.Compatibility
{
    using Frameworks;

    public static class Framework
    {
        private static readonly FrameworkCurrent SFrameworkCurrent = new();

        public static FrameworkBase Do
        {
            get { return SFrameworkCurrent; }
        }
    }
}
