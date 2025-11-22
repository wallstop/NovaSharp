namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;

    internal sealed partial class Processor
    {
        internal Processor ParentProcessorForTests
        {
            get { return _parent; }
        }

        internal FastStack<DynValue> GetValueStackForTests()
        {
            return _valueStack;
        }

        internal FastStack<CallStackItem> GetExecutionStackForTests()
        {
            return _executionStack;
        }

        internal List<Processor> GetCoroutineStackForTests()
        {
            return _coroutinesStack;
        }

        internal void ReplaceCoroutineStackForTests(List<Processor> stack)
        {
            if (stack == null)
            {
                throw new ArgumentNullException(nameof(stack));
            }

            _coroutinesStack = stack;
        }

        internal void SetThreadOwnershipStateForTests(int owningThreadId, int executionNesting)
        {
            _owningThreadId = owningThreadId;
            _executionNesting = executionNesting;
        }

        internal void EnterProcessorForTests()
        {
            EnterProcessor();
        }

        internal void LeaveProcessorForTests()
        {
            LeaveProcessor();
        }

        internal static Processor CreateChildProcessorForTests(Processor parentProcessor)
        {
            if (parentProcessor == null)
            {
                throw new ArgumentNullException(nameof(parentProcessor));
            }

            return new Processor(parentProcessor);
        }

        internal static Processor CreateRecycledProcessorForTests(
            Processor parentProcessor,
            Processor recycleProcessor
        )
        {
            if (parentProcessor == null)
            {
                throw new ArgumentNullException(nameof(parentProcessor));
            }

            if (recycleProcessor == null)
            {
                throw new ArgumentNullException(nameof(recycleProcessor));
            }

            return new Processor(parentProcessor, recycleProcessor);
        }

        internal bool SwapCanYieldForTests(bool canYield)
        {
            bool original = _canYield;
            _canYield = canYield;
            return original;
        }

        internal DynValue[] StackTopToArrayForTests(int items, bool pop)
        {
            return StackTopToArray(items, pop);
        }

        internal DynValue[] StackTopToArrayReverseForTests(int items, bool pop)
        {
            return StackTopToArrayReverse(items, pop);
        }

        internal List<WatchItem> RefreshDebuggerThreadsForTests()
        {
            return RefreshDebuggerThreads(null);
        }

        internal int PopExecStackAndCheckVStackForTests(int expectedGuard)
        {
            return PopExecStackAndCheckVStack(expectedGuard);
        }

        internal void ExecIncrForTests(Instruction instruction)
        {
            ExecIncr(instruction);
        }

        internal void ExecCNotForTests(Instruction instruction)
        {
            ExecCNot(instruction);
        }

        internal void ExecBeginFnForTests(Instruction instruction)
        {
            ExecBeginFn(instruction);
        }

        internal void CloseSymbolsSubsetForTests(
            CallStackItem frame,
            SymbolRef[] symbols,
            DynValue errorValue
        )
        {
            CloseSymbolsSubset(frame, symbols, errorValue);
        }

        internal void ClearBlockDataForTests(Instruction instruction)
        {
            ClearBlockData(instruction);
        }

        internal DynValue GetGlobalSymbolForTests(DynValue env, string name)
        {
            return GetGlobalSymbol(env, name);
        }

        internal void SetGlobalSymbolForTests(DynValue env, string name, DynValue value)
        {
            SetGlobalSymbol(env, name, value);
        }

        internal DynValue[] InternalAdjustTupleForTests(IList<DynValue> values)
        {
            return InternalAdjustTuple(values);
        }

        internal void ExecIterPrepForTests(Instruction instruction)
        {
            ExecIterPrep(instruction);
        }

        internal void ExecRetForTests(Instruction instruction)
        {
            ExecRet(instruction);
        }

        internal void ExecBitNotForTests(Instruction instruction, int instructionPtr)
        {
            ExecBitNot(instruction, instructionPtr);
        }

        internal void ExecBitAndForTests(Instruction instruction, int instructionPtr)
        {
            ExecBitAnd(instruction, instructionPtr);
        }

        internal void ExecTblInitIForTests(Instruction instruction)
        {
            ExecTblInitI(instruction);
        }

        internal void ExecTblInitNForTests(Instruction instruction)
        {
            ExecTblInitN(instruction);
        }

        internal void ExecIndexSetForTests(Instruction instruction, int instructionPtr)
        {
            ExecIndexSet(instruction, instructionPtr);
        }

        internal void ExecIndexForTests(Instruction instruction, int instructionPtr)
        {
            ExecIndex(instruction, instructionPtr);
        }
    }
}
