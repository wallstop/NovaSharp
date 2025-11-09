using NovaSharp.Interpreter;

namespace NovaSharp
{
    public class ShellContext
    {
        public Script Script { get; private set; }

        public ShellContext(Script script)
        {
            Script = script;
        }
    }
}
