namespace NovaSharp.Interpreter.REPL
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;

    /// <summary>
    /// This class provides a simple REPL intepreter ready to be reused in a simple way.
    /// </summary>
    public class ReplInterpreter
    {
        private Script _script;
        private string _currentCommand = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplInterpreter"/> class.
        /// </summary>
        /// <param name="script">The script.</param>
        public ReplInterpreter(Script script)
        {
            _script = script;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instances handle inputs starting with a "?" as a
        /// dynamic expression to evaluate instead of script code (likely invalid)
        /// </summary>
        public bool HandleDynamicExprs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instances handle inputs starting with a "=" as a
        /// non-dynamic expression to evaluate (just like the Lua interpreter does by default).
        /// </summary>
        public bool HandleClassicExprsSyntax { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has a pending command
        /// </summary>
        public virtual bool HasPendingCommand
        {
            get { return _currentCommand.Length > 0; }
        }

        /// <summary>
        /// Gets the current pending command.
        /// </summary>
        public virtual string CurrentPendingCommand
        {
            get { return _currentCommand; }
        }

        /// <summary>
        /// Gets the classic prompt (">" or ">>") given the current state of the interpreter
        /// </summary>
        public virtual string ClassicPrompt
        {
            get { return HasPendingCommand ? ">>" : ">"; }
        }

        /// <summary>
        /// Evaluate a REPL command.
        /// This method returns the result of the computation, or null if more input is needed for having valid code.
        /// In case of errors, exceptions are propagated to the caller.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>This method returns the result of the computation, or null if more input is needed for a computation.</returns>
        public virtual DynValue Evaluate(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            bool isFirstLine = !HasPendingCommand;

            bool forced = input.Length == 0;

            _currentCommand += input;

            if (_currentCommand.Length == 0)
            {
                return DynValue.Void;
            }

            _currentCommand += "\n";

            try
            {
                DynValue result = null;

                if (isFirstLine && HandleClassicExprsSyntax && _currentCommand[0] == '=')
                {
                    _currentCommand = "return " + _currentCommand.Substring(1);
                }

                if (isFirstLine && HandleDynamicExprs && _currentCommand[0] == '?')
                {
                    string code = _currentCommand.Substring(1).Trim();
                    if (code.Length == 0)
                    {
                        _currentCommand = string.Empty;
                        return DynValue.Void;
                    }

                    DynamicExpression exp = _script.CreateDynamicExpression(code);
                    result = exp.Evaluate();
                }
                else
                {
                    DynValue v = _script.LoadString(_currentCommand, null, "stdin");
                    result = _script.Call(v);
                }

                _currentCommand = string.Empty;
                return result;
            }
            catch (SyntaxErrorException ex)
            {
                if (forced || !ex.IsPrematureStreamTermination)
                {
                    _currentCommand = string.Empty;
                    ex.Rethrow();
                    throw;
                }
                else
                {
                    return null;
                }
            }
            catch (ScriptRuntimeException sre)
            {
                _currentCommand = string.Empty;
                sre.Rethrow();
                throw;
            }
            catch (Exception)
            {
                _currentCommand = string.Empty;
                throw;
            }
        }

        internal static class TestHooks
        {
            public static void SetScript(ReplInterpreter interpreter, Script script)
            {
                interpreter._script = script;
            }
        }
    }
}
