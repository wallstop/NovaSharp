namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;

    /// <content>
    /// Exposes internal processor state for unit tests.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Gets the parent processor instance (test helper).
        /// </summary>
        internal Processor ParentProcessorForTests
        {
            get { return _parent; }
        }

        /// <summary>
        /// Exposes the value stack for assertions.
        /// </summary>
        internal FastStack<DynValue> GetValueStackForTests()
        {
            return _valueStack;
        }

        /// <summary>
        /// Exposes the execution stack for assertions.
        /// </summary>
        internal FastStack<CallStackItem> GetExecutionStackForTests()
        {
            return _executionStack;
        }

        /// <summary>
        /// Exposes the coroutine stack for assertions.
        /// </summary>
        internal List<Processor> GetCoroutineStackForTests()
        {
            return _coroutinesStack;
        }

        /// <summary>
        /// Replaces the coroutine stack with a test-provided instance.
        /// </summary>
        internal void ReplaceCoroutineStackForTests(List<Processor> stack)
        {
            if (stack == null)
            {
                throw new ArgumentNullException(nameof(stack));
            }

            _coroutinesStack = stack;
        }

        /// <summary>
        /// Overrides thread ownership metadata to simulate nested entries.
        /// </summary>
        internal void SetThreadOwnershipStateForTests(int owningThreadId, int executionNesting)
        {
            _owningThreadId = owningThreadId;
            _executionNesting = executionNesting;
        }

        /// <summary>
        /// Calls <see cref="EnterProcessor"/> without changing production state (test-only).
        /// </summary>
        internal void EnterProcessorForTests()
        {
            EnterProcessor();
        }

        /// <summary>
        /// Calls <see cref="LeaveProcessor"/> without changing production state (test-only).
        /// </summary>
        internal void LeaveProcessorForTests()
        {
            LeaveProcessor();
        }

        /// <summary>
        /// Creates a child processor via the private constructor to facilitate unit tests.
        /// </summary>
        internal static Processor CreateChildProcessorForTests(Processor parentProcessor)
        {
            if (parentProcessor == null)
            {
                throw new ArgumentNullException(nameof(parentProcessor));
            }

            return new Processor(parentProcessor);
        }

        /// <summary>
        /// Creates a recycled processor via the private constructor for testing pooling.
        /// </summary>
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

        /// <summary>
        /// Sets the <see cref="CanYield"/> flag and returns the previous value.
        /// </summary>
        internal bool SwapCanYieldForTests(bool canYield)
        {
            bool original = _canYield;
            _canYield = canYield;
            return original;
        }

        /// <summary>
        /// Wraps <see cref="StackTopToArray"/> so tests can inspect the stack.
        /// </summary>
        internal DynValue[] StackTopToArrayForTests(int items, bool pop)
        {
            return StackTopToArray(items, pop);
        }

        /// <summary>
        /// Wraps <see cref="StackTopToArrayReverse"/> so tests can inspect the stack.
        /// </summary>
        internal DynValue[] StackTopToArrayReverseForTests(int items, bool pop)
        {
            return StackTopToArrayReverse(items, pop);
        }

        /// <summary>
        /// Invokes debugger-thread refresh logic and returns the resulting watch list.
        /// </summary>
        internal List<WatchItem> RefreshDebuggerThreadsForTests()
        {
            return RefreshDebuggerThreads(null);
        }

        /// <summary>
        /// Invokes <see cref="PopExecStackAndCheckVStack"/> for assertions.
        /// </summary>
        internal int PopExecStackAndCheckVStackForTests(int expectedGuard)
        {
            return PopExecStackAndCheckVStack(expectedGuard);
        }

        /// <summary>
        /// Executes the INCR opcode implementation for tests.
        /// </summary>
        internal void ExecIncrForTests(Instruction instruction)
        {
            ExecIncr(instruction);
        }

        /// <summary>
        /// Executes the CNOT opcode implementation for tests.
        /// </summary>
        internal void ExecCNotForTests(Instruction instruction)
        {
            ExecCNot(instruction);
        }

        /// <summary>
        /// Executes the BEGINFN opcode implementation for tests.
        /// </summary>
        internal void ExecBeginFnForTests(Instruction instruction)
        {
            ExecBeginFn(instruction);
        }

        /// <summary>
        /// Runs the to-be-closed cleanup logic against a custom frame (test helper).
        /// </summary>
        internal void CloseSymbolsSubsetForTests(
            CallStackItem frame,
            SymbolRef[] symbols,
            DynValue errorValue
        )
        {
            CloseSymbolsSubset(frame, symbols, errorValue);
        }

        /// <summary>
        /// Invokes <see cref="ClearBlockData"/> to help tests verify block unwinding.
        /// </summary>
        internal void ClearBlockDataForTests(Instruction instruction)
        {
            ClearBlockData(instruction);
        }

        /// <summary>
        /// Wraps <see cref="ProcessorScope.GetGlobalSymbol"/> for tests.
        /// </summary>
        internal static DynValue GetGlobalSymbolForTests(DynValue env, string name)
        {
            return GetGlobalSymbol(env, name);
        }

        /// <summary>
        /// Wraps <see cref="ProcessorScope.SetGlobalSymbol"/> for tests.
        /// </summary>
        internal static void SetGlobalSymbolForTests(DynValue env, string name, DynValue value)
        {
            SetGlobalSymbol(env, name, value);
        }

        /// <summary>
        /// Wraps the tuple-adjustment helper for tests.
        /// </summary>
        internal static DynValue[] InternalAdjustTupleForTests(IList<DynValue> values)
        {
            return InternalAdjustTuple(values);
        }

        /// <summary>
        /// Executes the ITERPREP opcode implementation for tests.
        /// </summary>
        internal void ExecIterPrepForTests(Instruction instruction)
        {
            ExecIterPrep(instruction);
        }

        /// <summary>
        /// Executes the RET opcode implementation for tests.
        /// </summary>
        internal void ExecRetForTests(Instruction instruction)
        {
            ExecRet(instruction);
        }

        /// <summary>
        /// Executes the BITNOT opcode implementation for tests.
        /// </summary>
        internal void ExecBitNotForTests(Instruction instruction, int instructionPtr)
        {
            ExecBitNot(instruction, instructionPtr);
        }

        /// <summary>
        /// Executes the BITAND opcode implementation for tests.
        /// </summary>
        internal void ExecBitAndForTests(Instruction instruction, int instructionPtr)
        {
            ExecBitAnd(instruction, instructionPtr);
        }

        /// <summary>
        /// Executes the TBLINITI opcode implementation for tests.
        /// </summary>
        internal void ExecTblInitIForTests(Instruction instruction)
        {
            ExecTblInitI(instruction);
        }

        /// <summary>
        /// Executes the TBLINITN opcode implementation for tests.
        /// </summary>
        internal void ExecTblInitNForTests(Instruction instruction)
        {
            ExecTblInitN(instruction);
        }

        /// <summary>
        /// Executes the INDEXSET opcode implementation for tests.
        /// </summary>
        internal void ExecIndexSetForTests(Instruction instruction, int instructionPtr)
        {
            ExecIndexSet(instruction, instructionPtr);
        }

        /// <summary>
        /// Executes the INDEX opcode implementation for tests.
        /// </summary>
        internal void ExecIndexForTests(Instruction instruction, int instructionPtr)
        {
            ExecIndex(instruction, instructionPtr);
        }
    }
}
