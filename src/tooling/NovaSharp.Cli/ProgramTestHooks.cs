namespace NovaSharp.Cli
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.REPL;

    /// <summary>
    /// Internal helpers exposed for unit tests so they can drive the REPL without reflection.
    /// </summary>
    internal sealed partial class Program
    {
        internal static void RunInterpreterLoopForTests(
            ReplInterpreter interpreter,
            ShellContext shellContext
        )
        {
            InterpreterLoop(interpreter, shellContext);
        }

        internal static void ShowBannerForTests(Script script)
        {
            Banner(script);
        }
    }
}
