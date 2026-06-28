namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;

    /// <summary>
    /// Provides cached string representations for <see cref="OpCode"/> values
    /// to avoid allocating ToString() calls.
    /// </summary>
    internal static class OpCodeStrings
    {
        // Offset to handle Unknown = -1; array index = (int)opCode + 1
        private const int Offset = 1;

        private static readonly string[] Names =
        {
            // Index 0: Unknown (-1)
            "Unknown",
            // Index 1: Nop (0)
            "Nop",
            // Index 2: Debug (1)
            "Debug",
            // Index 3: Pop (2)
            "Pop",
            // Index 4: Copy (3)
            "Copy",
            // Index 5: Swap (4)
            "Swap",
            // Index 6: Literal (5)
            "Literal",
            // Index 7: Closure (6)
            "Closure",
            // Index 8: NewTable (7)
            "NewTable",
            // Index 9: TblInitN (8)
            "TblInitN",
            // Index 10: TblInitI (9)
            "TblInitI",
            // Index 11: StoreLcl (10)
            "StoreLcl",
            // Index 12: Local (11)
            "Local",
            // Index 13: StoreUpv (12)
            "StoreUpv",
            // Index 14: UpValue (13)
            "UpValue",
            // Index 15: IndexSet (14)
            "IndexSet",
            // Index 16: Index (15)
            "Index",
            // Index 17: IndexSetN (16)
            "IndexSetN",
            // Index 18: IndexN (17)
            "IndexN",
            // Index 19: IndexSetL (18)
            "IndexSetL",
            // Index 20: IndexL (19)
            "IndexL",
            // Index 21: Clean (20)
            "Clean",
            // Index 22: Meta (21)
            "Meta",
            // Index 23: BeginFn (22)
            "BeginFn",
            // Index 24: Args (23)
            "Args",
            // Index 25: Call (24)
            "Call",
            // Index 26: ThisCall (25)
            "ThisCall",
            // Index 27: Ret (26)
            "Ret",
            // Index 28: Jump (27)
            "Jump",
            // Index 29: Jf (28)
            "Jf",
            // Index 30: JNil (29)
            "JNil",
            // Index 31: JFor (30)
            "JFor",
            // Index 32: JtOrPop (31)
            "JtOrPop",
            // Index 33: JfOrPop (32)
            "JfOrPop",
            // Index 34: Concat (33)
            "Concat",
            // Index 35: LessEq (34)
            "LessEq",
            // Index 36: Less (35)
            "Less",
            // Index 37: Eq (36)
            "Eq",
            // Index 38: Add (37)
            "Add",
            // Index 39: Sub (38)
            "Sub",
            // Index 40: Mul (39)
            "Mul",
            // Index 41: Div (40)
            "Div",
            // Index 42: Mod (41)
            "Mod",
            // Index 43: Not (42)
            "Not",
            // Index 44: Len (43)
            "Len",
            // Index 45: Neg (44)
            "Neg",
            // Index 46: Power (45)
            "Power",
            // Index 47: CNot (46)
            "CNot",
            // Index 48: MkTuple (47)
            "MkTuple",
            // Index 49: Scalar (48)
            "Scalar",
            // Index 50: Incr (49)
            "Incr",
            // Index 51: ToNum (50)
            "ToNum",
            // Index 52: ToBool (51)
            "ToBool",
            // Index 53: ExpTuple (52)
            "ExpTuple",
            // Index 54: Enter (53)
            "Enter",
            // Index 55: Leave (54)
            "Leave",
            // Index 56: Exit (55)
            "Exit",
            // Index 57: IterPrep (56)
            "IterPrep",
            // Index 58: IterUpd (57)
            "IterUpd",
            // Index 59: Invalid (58)
            "Invalid",
            // Index 60: BitAnd (59)
            "BitAnd",
            // Index 61: BitOr (60)
            "BitOr",
            // Index 62: BitXor (61)
            "BitXor",
            // Index 63: BitNot (62)
            "BitNot",
            // Index 64: ShiftLeft (63)
            "ShiftLeft",
            // Index 65: ShiftRight (64)
            "ShiftRight",
            // Index 66: FloorDiv (65)
            "FloorDiv",
        };

        private static readonly string[] UpperNames =
        {
            // Index 0: Unknown (-1)
            "UNKNOWN",
            // Index 1: Nop (0)
            "NOP",
            // Index 2: Debug (1)
            "DEBUG",
            // Index 3: Pop (2)
            "POP",
            // Index 4: Copy (3)
            "COPY",
            // Index 5: Swap (4)
            "SWAP",
            // Index 6: Literal (5)
            "LITERAL",
            // Index 7: Closure (6)
            "CLOSURE",
            // Index 8: NewTable (7)
            "NEWTABLE",
            // Index 9: TblInitN (8)
            "TBLINITN",
            // Index 10: TblInitI (9)
            "TBLINITI",
            // Index 11: StoreLcl (10)
            "STORELCL",
            // Index 12: Local (11)
            "LOCAL",
            // Index 13: StoreUpv (12)
            "STOREUPV",
            // Index 14: UpValue (13)
            "UPVALUE",
            // Index 15: IndexSet (14)
            "INDEXSET",
            // Index 16: Index (15)
            "INDEX",
            // Index 17: IndexSetN (16)
            "INDEXSETN",
            // Index 18: IndexN (17)
            "INDEXN",
            // Index 19: IndexSetL (18)
            "INDEXSETL",
            // Index 20: IndexL (19)
            "INDEXL",
            // Index 21: Clean (20)
            "CLEAN",
            // Index 22: Meta (21)
            "META",
            // Index 23: BeginFn (22)
            "BEGINFN",
            // Index 24: Args (23)
            "ARGS",
            // Index 25: Call (24)
            "CALL",
            // Index 26: ThisCall (25)
            "THISCALL",
            // Index 27: Ret (26)
            "RET",
            // Index 28: Jump (27)
            "JUMP",
            // Index 29: Jf (28)
            "JF",
            // Index 30: JNil (29)
            "JNIL",
            // Index 31: JFor (30)
            "JFOR",
            // Index 32: JtOrPop (31)
            "JTORPOP",
            // Index 33: JfOrPop (32)
            "JFORPOP",
            // Index 34: Concat (33)
            "CONCAT",
            // Index 35: LessEq (34)
            "LESSEQ",
            // Index 36: Less (35)
            "LESS",
            // Index 37: Eq (36)
            "EQ",
            // Index 38: Add (37)
            "ADD",
            // Index 39: Sub (38)
            "SUB",
            // Index 40: Mul (39)
            "MUL",
            // Index 41: Div (40)
            "DIV",
            // Index 42: Mod (41)
            "MOD",
            // Index 43: Not (42)
            "NOT",
            // Index 44: Len (43)
            "LEN",
            // Index 45: Neg (44)
            "NEG",
            // Index 46: Power (45)
            "POWER",
            // Index 47: CNot (46)
            "CNOT",
            // Index 48: MkTuple (47)
            "MKTUPLE",
            // Index 49: Scalar (48)
            "SCALAR",
            // Index 50: Incr (49)
            "INCR",
            // Index 51: ToNum (50)
            "TONUM",
            // Index 52: ToBool (51)
            "TOBOOL",
            // Index 53: ExpTuple (52)
            "EXPTUPLE",
            // Index 54: Enter (53)
            "ENTER",
            // Index 55: Leave (54)
            "LEAVE",
            // Index 56: Exit (55)
            "EXIT",
            // Index 57: IterPrep (56)
            "ITERPREP",
            // Index 58: IterUpd (57)
            "ITERUPD",
            // Index 59: Invalid (58)
            "INVALID",
            // Index 60: BitAnd (59)
            "BITAND",
            // Index 61: BitOr (60)
            "BITOR",
            // Index 62: BitXor (61)
            "BITXOR",
            // Index 63: BitNot (62)
            "BITNOT",
            // Index 64: ShiftLeft (63)
            "SHIFTLEFT",
            // Index 65: ShiftRight (64)
            "SHIFTRIGHT",
            // Index 66: FloorDiv (65)
            "FLOORDIV",
        };

        /// <summary>
        /// Gets the cached string name for the specified <see cref="OpCode"/>.
        /// </summary>
        /// <param name="opCode">The opcode to get the name for.</param>
        /// <returns>The string representation of the opcode.</returns>
        public static string GetName(OpCode opCode)
        {
            int index = (int)opCode + Offset;
            if (index >= 0 && index < Names.Length)
            {
                return Names[index];
            }
            return opCode.ToString();
        }

        /// <summary>
        /// Gets the cached uppercase string name for the specified <see cref="OpCode"/>.
        /// </summary>
        /// <param name="opCode">The opcode to get the uppercase name for.</param>
        /// <returns>The uppercase string representation of the opcode.</returns>
        public static string GetUpperName(OpCode opCode)
        {
            int index = (int)opCode + Offset;
            if (index >= 0 && index < UpperNames.Length)
            {
                return UpperNames[index];
            }
            return opCode.ToString().ToUpperInvariant();
        }
    }
}
