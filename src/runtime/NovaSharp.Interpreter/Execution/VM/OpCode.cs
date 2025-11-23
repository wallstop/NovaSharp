namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter.DataTypes;

    [SuppressMessage(
        "Design",
        "CA1069:Enums values should not be duplicated",
        Justification = "Opcode numeric values are serialized; Nop must remain 0 for backward compatibility."
    )]
    /// <summary>
    /// Defines the NovaSharp VM instruction set. Values must remain stable because they are persisted in chunk dumps.
    /// </summary>
    internal enum OpCode
    {
        [Obsolete("Use a specific OpCode.", false)]
        Unknown = 0,

        // Meta-opcodes
        Nop = 0, // Does not perform any operation.
        Debug = 1, // Does not perform any operation. Used to help debugging.

        // Stack ops and assignment
        Pop = 2, // Discards the topmost n elements from the v-stack.
        Copy = 3, // Copies the n-th value of the stack on the top
        Swap = 4, // Swaps two entries relative to the v-stack
        Literal = 5, // Pushes a literal (constant value) on the stack.
        Closure = 6, // Creates a closure on the top of the v-stack, using the symbols for upvalues and num-val for entry point of the function.
        NewTable = 7, // Creates a new empty table on the stack
        TblInitN = 8, // Initializes a table named entry
        TblInitI = 9, // Initializes a table positional entry

        StoreLcl = 10,
        Local = 11,
        StoreUpv = 12,
        Upvalue = 13,
        IndexSet = 14,
        Index = 15,
        IndexSetN = 16,
        IndexN = 17,
        IndexSetL = 18,
        IndexL = 19,

        // Stack-frame ops and calls
        Clean = 20, // Cleansup locals setting them as null

        Meta = 21, // Injects function metadata used for reflection things (dumping, debugging)
        BeginFn = 22, // Adjusts for start of function, taking in parameters and allocating locals
        Args = 23, // Takes the arguments passed to a function and sets the appropriate symbols in the local scope
        Call = 24, // Calls the function specified on the specified element from the top of the v-stack. If the function is a NovaSharp function, it pushes its numeric value on the v-stack, then pushes the current PC onto the x-stack, enters the function closure and jumps to the function first instruction. If the function is a CLR function, it pops the function value from the v-stack, then invokes the function synchronously and finally pushes the result on the v-stack.
        ThisCall = 25, // Same as call, but the call is a ':' method invocation
        Ret = 26, // Pops the top n values of the v-stack. Then pops an X value from the v-stack. Then pops X values from the v-stack. Afterwards, it pushes the top n values popped in the first step, pops the top of the x-stack and jumps to that location.

        // Jumps
        Jump = 27, // Jumps to the specified PC
        Jf = 28, // Pops the top of the v-stack and jumps to the specified location if it's false
        JNil = 29, // Jumps if the top of the stack is nil
        JFor = 30, // Peeks at the top, top-1 and top-2 values of the v-stack which it assumes to be numbers. Then if top-1 is less than zero, checks if top is <= top-2, otherwise it checks that top is >= top-2. Then if the condition is false, it jumps.
        JtOrPop = 31, // Peeks at the topmost value of the v-stack as a boolean. If true, it performs a jump, otherwise it removes the topmost value from the v-stack.
        JfOrPop = 32, // Peeks at the topmost value of the v-stack as a boolean. If false, it performs a jump, otherwise it removes the topmost value from the v-stack.

        // Operators
        Concat = 33, // Concatenation of the two topmost operands on the v-stack
        LessEq = 34, // Compare <= of the two topmost operands on the v-stack
        Less = 35, // Compare < of the two topmost operands on the v-stack
        Eq = 36, // Compare == of the two topmost operands on the v-stack
        Add = 37, // Addition of the two topmost operands on the v-stack
        Sub = 38, // Subtraction of the two topmost operands on the v-stack
        Mul = 39, // Multiplication of the two topmost operands on the v-stack
        Div = 40, // Division of the two topmost operands on the v-stack
        Mod = 41, // Modulus of the two topmost operands on the v-stack
        Not = 42, // Logical inversion of the topmost operand on the v-stack
        Len = 43, // Size operator of the topmost operand on the v-stack
        Neg = 44, // Negation (unary minus) operator of the topmost operand on the v-stack
        Power = 45, // Power of the two topmost operands on the v-stack
        CNot = 46, // Conditional NOT - takes second operand from the v-stack (must be bool), if true execs a NOT otherwise execs a TOBOOL

        // Type conversions and manipulations
        MkTuple = 47, // Creates a tuple from the topmost n values
        Scalar = 48, // Converts the topmost tuple to a scalar
        Incr = 49, // Performs an add operation, without extracting the operands from the v-stack and assuming the operands are numbers.
        ToNum = 50, // Converts the top of the stack to a number
        ToBool = 51, // Converts the top of the stack to a boolean
        ExpTuple = 52, // Expands a tuple on the stack
        Enter = 53, // Prepares a scope block (clears locals and registers to-be-closed variables)
        Leave = 54, // Leaves a scope block (normal flow, closes to-be-closed variables)
        Exit = 55, // Leaves a scope block due to break/goto (closes to-be-closed variables)

        // Iterators
        IterPrep = 56, // Prepares an iterator for execution
        IterUpd = 57, // Updates the var part of an iterator

        // Extended operators
        BitAnd = 59,
        BitOr = 60,
        BitXor = 61,
        BitNot = 62,
        ShiftLeft = 63,
        ShiftRight = 64,
        FloorDiv = 65,

        // Meta
        Invalid = 58, // Crashes the executor with an unrecoverable NotImplementedException. This MUST always be the last opcode in enum
    }
}
