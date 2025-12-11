namespace WallstopStudios.NovaSharp.Cli
{
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.REPL;

    /// <summary>
    /// Internal helpers exposed for unit tests so they can drive the REPL without reflection.
    /// </summary>
    internal sealed partial class Program
    {
        /// <summary>
        /// Invokes the private interpreter loop so tests can step through REPL behavior.
        /// </summary>
        internal static void RunInterpreterLoopForTests(
            ReplInterpreter interpreter,
            ShellContext shellContext
        )
        {
            InterpreterLoop(interpreter, shellContext);
        }

        /// <summary>
        /// Invokes the banner writer so tests can capture its output.
        /// </summary>
        internal static void ShowBannerForTests(Script script)
        {
            Banner(script);
        }
    }
}
