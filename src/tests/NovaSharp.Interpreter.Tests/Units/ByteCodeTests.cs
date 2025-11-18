#define EMIT_DEBUG_OPS

namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Execution.VM;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ByteCodeTests
    {
        [Test]
        public void EnterSourceAppliesSourceRefsUntilGuardDisposes()
        {
            Script script = new();
            ByteCode byteCode = new(script);
            SourceRef sourceRef = new(0, 1, 2, 3, 4, false);

            using (byteCode.EnterSource(sourceRef))
            {
                Instruction first = byteCode.Emit_Nop("first");
                Assert.That(first.SourceCodeRef, Is.SameAs(sourceRef));
            }

            Instruction second = byteCode.Emit_Nop("second");
            Assert.That(second.SourceCodeRef, Is.Null, "PopSourceRef should clear the current ref");
        }

#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE) && (!(NETFX_CORE))
        [Test]
        public void DumpWritesInstructionsAndDebugLines()
        {
            Script script = new();
            ByteCode byteCode = new(script);
            byteCode.Emit_Nop("first");
            byteCode.Emit_Debug("trace");
            byteCode.Emit_Operator(OpCode.Add);

            string path = Path.Combine(Path.GetTempPath(), $"bytecode-{Guid.NewGuid():N}.txt");
            try
            {
                byteCode.Dump(path);
                string contents = File.ReadAllText(path);
                Assert.That(contents, Does.Contain("00000000"));
                Assert.That(contents, Does.Contain("trace"));
                Assert.That(contents, Does.Contain("ADD"));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
#endif

        [TestCase((int)OpCode.LessEq, (int)OpCode.CNot)]
        [TestCase((int)OpCode.Eq, (int)OpCode.ToBool)]
        [TestCase((int)OpCode.Less, (int)OpCode.ToBool)]
        public void EmitOperatorAppendsNormalizationOps(int inputValue, int appendedValue)
        {
            OpCode input = (OpCode)inputValue;
            OpCode appended = (OpCode)appendedValue;
            ByteCode byteCode = new(new Script());

            byteCode.Emit_Operator(input);

            Assert.That(byteCode.code[^2].OpCode, Is.EqualTo(input));
            Assert.That(byteCode.code[^1].OpCode, Is.EqualTo(appended));
        }

        [TestCase(true, false, (int)OpCode.IndexN)]
        [TestCase(false, true, (int)OpCode.IndexL)]
        [TestCase(false, false, (int)OpCode.Index)]
        public void EmitIndexSelectsOpcodeBasedOnFlags(
            bool isNameIndex,
            bool isExpList,
            int expectedValue
        )
        {
            OpCode expected = (OpCode)expectedValue;
            ByteCode byteCode = new(new Script());
            Instruction instruction = byteCode.Emit_Index(
                DynValue.NewString("name"),
                isNameIndex,
                isExpList
            );

            Assert.That(instruction.OpCode, Is.EqualTo(expected));
            Assert.That(instruction.Value.String, Is.EqualTo("name"));
        }

        [TestCase(true, false, (int)OpCode.IndexSetN)]
        [TestCase(false, true, (int)OpCode.IndexSetL)]
        [TestCase(false, false, (int)OpCode.IndexSet)]
        public void EmitIndexSetSelectsOpcodeBasedOnFlags(
            bool isNameIndex,
            bool isExpList,
            int expectedValue
        )
        {
            OpCode expected = (OpCode)expectedValue;
            ByteCode byteCode = new(new Script());
            Instruction instruction = byteCode.Emit_IndexSet(
                stackofs: 1,
                tupleidx: 2,
                index: DynValue.NewString("idx"),
                isNameIndex: isNameIndex,
                isExpList: isExpList
            );

            Assert.That(instruction.OpCode, Is.EqualTo(expected));
            Assert.That(instruction.NumVal, Is.EqualTo(1));
            Assert.That(instruction.NumVal2, Is.EqualTo(2));
            Assert.That(instruction.Value.String, Is.EqualTo("idx"));
        }

        [Test]
        public void EmitLoadHandlesGlobalSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef env = SymbolRef.Upvalue("_ENV", 0);
            SymbolRef symbol = SymbolRef.Global("globalValue", env);

            int stackSlots = byteCode.Emit_Load(symbol);

            Assert.That(stackSlots, Is.EqualTo(2));
            Assert.That(byteCode.code[^2].OpCode, Is.EqualTo(OpCode.Upvalue));
            Assert.That(byteCode.code[^1].OpCode, Is.EqualTo(OpCode.Index));
            Assert.That(byteCode.code[^1].Value.String, Is.EqualTo("globalValue"));
        }

        [Test]
        public void EmitLoadHandlesLocalSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef symbol = SymbolRef.Local("localValue", 1);

            int stackSlots = byteCode.Emit_Load(symbol);

            Assert.That(stackSlots, Is.EqualTo(1));
            Assert.That(byteCode.code[^1].OpCode, Is.EqualTo(OpCode.Local));
            Assert.That(byteCode.code[^1].Symbol, Is.EqualTo(symbol));
        }

        [Test]
        public void EmitLoadHandlesUpvalueSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef symbol = SymbolRef.Upvalue("upvalue", 2);

            int stackSlots = byteCode.Emit_Load(symbol);

            Assert.That(stackSlots, Is.EqualTo(1));
            Assert.That(byteCode.code[^1].OpCode, Is.EqualTo(OpCode.Upvalue));
            Assert.That(byteCode.code[^1].Symbol, Is.EqualTo(symbol));
        }

        [Test]
        public void EmitStoreHandlesGlobalSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef env = SymbolRef.Upvalue("_ENV", 0);
            SymbolRef symbol = SymbolRef.Global("globalValue", env);

            int stackSlots = byteCode.Emit_Store(symbol, stackofs: 1, tupleidx: 2);

            Assert.That(stackSlots, Is.EqualTo(2));
            Assert.That(byteCode.code[^2].OpCode, Is.EqualTo(OpCode.Upvalue));
            Instruction setter = byteCode.code[^1];
            Assert.That(setter.OpCode, Is.EqualTo(OpCode.IndexSet));
            Assert.That(setter.NumVal, Is.EqualTo(1));
            Assert.That(setter.NumVal2, Is.EqualTo(2));
            Assert.That(setter.Value.String, Is.EqualTo("globalValue"));
        }

        [Test]
        public void EmitStoreHandlesLocalSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef symbol = SymbolRef.Local("localValue", 3);

            int stackSlots = byteCode.Emit_Store(symbol, stackofs: 4, tupleidx: 1);

            Assert.That(stackSlots, Is.EqualTo(1));
            Instruction setter = byteCode.code[^1];
            Assert.That(setter.OpCode, Is.EqualTo(OpCode.StoreLcl));
            Assert.That(setter.NumVal, Is.EqualTo(4));
            Assert.That(setter.NumVal2, Is.EqualTo(1));
            Assert.That(setter.Symbol, Is.EqualTo(symbol));
        }

        [Test]
        public void EmitStoreHandlesUpvalueSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef symbol = SymbolRef.Upvalue("upvalue", 5);

            int stackSlots = byteCode.Emit_Store(symbol, stackofs: 6, tupleidx: 0);

            Assert.That(stackSlots, Is.EqualTo(1));
            Instruction setter = byteCode.code[^1];
            Assert.That(setter.OpCode, Is.EqualTo(OpCode.StoreUpv));
            Assert.That(setter.NumVal, Is.EqualTo(6));
            Assert.That(setter.NumVal2, Is.EqualTo(0));
            Assert.That(setter.Symbol, Is.EqualTo(symbol));
        }

        [Test]
        public void EmitBeginFnSerializesScopeMetadata()
        {
            ByteCode byteCode = new(new Script());
            RuntimeScopeFrame frame = new() { ToFirstBlock = 42 };
            frame.DebugSymbols.Add(SymbolRef.Local("a", 0));
            frame.DebugSymbols.Add(SymbolRef.Local("b", 1));

            Instruction instruction = byteCode.Emit_BeginFn(frame);

            Assert.That(instruction.OpCode, Is.EqualTo(OpCode.BeginFn));
            Assert.That(instruction.SymbolList, Is.EquivalentTo(frame.DebugSymbols));
            Assert.That(instruction.NumVal, Is.EqualTo(frame.Count));
            Assert.That(instruction.NumVal2, Is.EqualTo(42));
        }

        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void EmitTblInitIEncodesLastPositionFlag(bool lastPos, int expectedFlag)
        {
            ByteCode byteCode = new(new Script());

            Instruction instruction = byteCode.Emit_TblInitI(lastPos);

            Assert.That(instruction.OpCode, Is.EqualTo(OpCode.TblInitI));
            Assert.That(instruction.NumVal, Is.EqualTo(expectedFlag));
        }

        [Test]
        public void EmitDebugProducesDebugInstruction()
        {
            ByteCode byteCode = new(new Script());

            byteCode.Emit_Debug("trace");

            Assert.That(byteCode.code[^1].OpCode, Is.EqualTo(OpCode.Debug));
            Assert.That(byteCode.code[^1].Name, Is.EqualTo("trace"));
        }
    }
}
