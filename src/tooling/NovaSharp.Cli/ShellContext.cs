namespace NovaSharp
{
    using Interpreter;

    public class ShellContext
    {
        public Script Script { get; private set; }

        public ShellContext(Script script)
        {
            Script = script;
        }
    }
}
