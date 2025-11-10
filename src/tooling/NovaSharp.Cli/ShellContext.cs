namespace NovaSharp
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public class ShellContext
    {
        public Script Script { get; private set; }

        public ShellContext(Script script)
        {
            Script = script;
        }
    }
}
