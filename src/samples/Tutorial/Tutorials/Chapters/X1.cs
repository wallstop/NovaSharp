using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Debugging;

namespace Tutorials.Chapters
{
    [Tutorial]
    static class X1
    {
        public class MyException : Exception { }

        class BreakAfterManyInstructionsDebugger : IDebugger
        {
            int _InstructionCounter = 0;
            List<DynamicExpression> _Dynamics = new List<DynamicExpression>();

            public void SetSourceCode(SourceCode sourceCode) { }

            public void SetByteCode(string[] byteCode) { }

            public DebuggerCaps GetDebuggerCaps()
            {
                return 0;
            }

            public bool IsPauseRequested()
            {
                return true;
            }

            public bool SignalRuntimeException(ScriptRuntimeException ex)
            {
                return false;
            }

            public DebuggerAction GetAction(int ip, SourceRef sourceref)
            {
                _InstructionCounter += 1;

                if ((_InstructionCounter % 1000) == 0)
                    Console.Write(".");

                if (_InstructionCounter > 50000)
                    throw new MyException();

                return new DebuggerAction() { Action = DebuggerAction.ActionType.StepIn };
            }

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items) { }

            public IReadOnlyList<DynamicExpression> GetWatchItems()
            {
                return _Dynamics;
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }

            public void SetDebugService(DebugService debugService) { }
        }

        [Tutorial]
        static void BreakAfterManyInstructions()
        {
            Script script = new Script();
            try
            {
                script.AttachDebugger(new BreakAfterManyInstructionsDebugger());

                script.DoString(
                    @"
				x = 0;
				while true do x = x + 1; end;
				"
                );
            }
            catch (MyException)
            {
                Console.WriteLine("Done. x = {0}", script.Globals["x"]);
            }
        }
    }
}
