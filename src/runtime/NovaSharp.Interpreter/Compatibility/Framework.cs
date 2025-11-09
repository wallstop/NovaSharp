using NovaSharp.Interpreter.Compatibility.Frameworks;

namespace NovaSharp.Interpreter.Compatibility
{
    public static class Framework
    {
        static FrameworkCurrent s_FrameworkCurrent = new();

        public static FrameworkBase Do
        {
            get { return s_FrameworkCurrent; }
        }
    }
}
