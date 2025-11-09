namespace NovaSharp.Interpreter.REPL
{
    /// <summary>
    /// An implementation of <see cref="ReplInterpreter"/> which supports a very basic history of recent input lines.
    /// </summary>
    public class ReplHistoryInterpreter : ReplInterpreter
    {
        private readonly string[] _history;
        private int _last = -1;
        private int _navi = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplHistoryInterpreter"/> class.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="historySize">Size of the history.</param>
        public ReplHistoryInterpreter(Script script, int historySize)
            : base(script)
        {
            _history = new string[historySize];
        }

        /// <summary>
        /// Evaluate a REPL command.
        /// This method returns the result of the computation, or null if more input is needed for having valid code.
        /// In case of errors, exceptions are propagated to the caller.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>
        /// This method returns the result of the computation, or null if more input is needed for a computation.
        /// </returns>
        public override DynValue Evaluate(string input)
        {
            _navi = -1;
            _last = (_last + 1) % _history.Length;
            _history[_last] = input;
            return base.Evaluate(input);
        }

        /// <summary>
        /// Gets the previous item in history, or null
        /// </summary>
        public string HistoryPrev()
        {
            if (_navi == -1)
            {
                _navi = _last;
            }
            else
            {
                _navi = ((_navi - 1) + _history.Length) % _history.Length;
            }

            if (_navi >= 0)
            {
                return _history[_navi];
            }

            return null;
        }

        /// <summary>
        /// Gets the next item in history, or null
        /// </summary>
        public string HistoryNext()
        {
            if (_navi == -1)
            {
                return null;
            }
            else
            {
                _navi = (_navi + 1) % _history.Length;
            }

            if (_navi >= 0)
            {
                return _history[_navi];
            }

            return null;
        }
    }
}
