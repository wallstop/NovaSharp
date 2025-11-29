#define EmitDebug_OPS
#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Execution.VM;

    public sealed class ByteCodeTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task EnterSourceAppliesSourceRefsUntilGuardDisposes()
        {
            Script script = new();
            ByteCode byteCode = new(script);
            SourceRef sourceRef = new(0, 1, 2, 3, 4, false);

            using (byteCode.EnterSource(sourceRef))
            {
                Instruction first = byteCode.EmitNop("first");
                await Assert.That(first.SourceCodeRef).IsSameReferenceAs(sourceRef);
            }

            Instruction second = byteCode.EmitNop("second");
            await Assert.That(second.SourceCodeRef is null).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task JumpPointHelpersExposeCodeCountAndLastInstruction()
        {
            ByteCode byteCode = new(new Script());
            byteCode.EmitNop("first");
            byteCode.EmitNop("second");

            await Assert.That(byteCode.GetJumpPointForNextInstruction()).IsEqualTo(2);
            await Assert.That(byteCode.GetJumpPointForLastInstruction()).IsEqualTo(1);
            await Assert.That(byteCode.GetLastInstruction().Name).IsEqualTo("second");
        }

#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE) && (!(NETFX_CORE))
        [global::TUnit.Core.Test]
        public async Task DumpWritesInstructionsAndDebugLines()
        {
            Script script = new();
            ByteCode byteCode = new(script);
            byteCode.EmitNop("first");
            byteCode.EmitDebug("trace");
            byteCode.EmitOperator(OpCode.Add);

            string path = Path.Combine(Path.GetTempPath(), $"bytecode-{Guid.NewGuid():N}.txt");
            try
            {
                byteCode.Dump(path);
                string contents = await File.ReadAllTextAsync(path, CancellationToken.None);

                await Assert.That(contents.Contains("00000000", StringComparison.Ordinal)).IsTrue();
                await Assert.That(contents.Contains("trace", StringComparison.Ordinal)).IsTrue();
                await Assert.That(contents.Contains("ADD", StringComparison.Ordinal)).IsTrue();
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

        [global::TUnit.Core.Test]
        public Task EmitOperatorAppendsNormalizationOpsForLessEq()
        {
            return AssertOperatorNormalization(OpCode.LessEq, OpCode.CNot);
        }

        [global::TUnit.Core.Test]
        public Task EmitOperatorAppendsNormalizationOpsForEq()
        {
            return AssertOperatorNormalization(OpCode.Eq, OpCode.ToBool);
        }

        [global::TUnit.Core.Test]
        public Task EmitOperatorAppendsNormalizationOpsForLess()
        {
            return AssertOperatorNormalization(OpCode.Less, OpCode.ToBool);
        }

        [global::TUnit.Core.Test]
        public Task EmitIndexSelectsOpcodeForNameIndices()
        {
            return AssertIndexOpcode(isNameIndex: true, isExpList: false, OpCode.IndexN);
        }

        [global::TUnit.Core.Test]
        public Task EmitIndexSelectsOpcodeForExpressionLists()
        {
            return AssertIndexOpcode(isNameIndex: false, isExpList: true, OpCode.IndexL);
        }

        [global::TUnit.Core.Test]
        public Task EmitIndexSelectsOpcodeForDefaultCase()
        {
            return AssertIndexOpcode(isNameIndex: false, isExpList: false, OpCode.Index);
        }

        [global::TUnit.Core.Test]
        public Task EmitIndexSetSelectsOpcodeForNameIndices()
        {
            return AssertIndexSetOpcode(isNameIndex: true, isExpList: false, OpCode.IndexSetN);
        }

        [global::TUnit.Core.Test]
        public Task EmitIndexSetSelectsOpcodeForExpressionLists()
        {
            return AssertIndexSetOpcode(isNameIndex: false, isExpList: true, OpCode.IndexSetL);
        }

        [global::TUnit.Core.Test]
        public Task EmitIndexSetSelectsOpcodeForDefaultCase()
        {
            return AssertIndexSetOpcode(isNameIndex: false, isExpList: false, OpCode.IndexSet);
        }

        [global::TUnit.Core.Test]
        public async Task EmitInvalidAddsInvalidInstructionWithReason()
        {
            ByteCode byteCode = new(new Script());

            Instruction instruction = byteCode.EmitInvalid("unsupported");

            await Assert.That(instruction.OpCode).IsEqualTo(OpCode.Invalid);
            await Assert.That(instruction.Name).IsEqualTo("unsupported");
        }

        [global::TUnit.Core.Test]
        public async Task EmitLoadHandlesGlobalSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef env = SymbolRef.UpValue("_ENV", 0);
            SymbolRef symbol = SymbolRef.Global("globalValue", env);

            int stackSlots = byteCode.EmitLoad(symbol);

            await Assert.That(stackSlots).IsEqualTo(2);
            await Assert.That(byteCode.Code[^2].OpCode).IsEqualTo(OpCode.UpValue);
            await Assert.That(byteCode.Code[^1].OpCode).IsEqualTo(OpCode.Index);
            await Assert.That(byteCode.Code[^1].Value.String).IsEqualTo("globalValue");
        }

        [global::TUnit.Core.Test]
        public async Task EmitLoadHandlesLocalSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef symbol = SymbolRef.Local("localValue", 1);

            int stackSlots = byteCode.EmitLoad(symbol);

            await Assert.That(stackSlots).IsEqualTo(1);
            await Assert.That(byteCode.Code[^1].OpCode).IsEqualTo(OpCode.Local);
            await Assert.That(byteCode.Code[^1].Symbol).IsSameReferenceAs(symbol);
        }

        [global::TUnit.Core.Test]
        public async Task EmitLoadHandlesUpValueSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef symbol = SymbolRef.UpValue("upvalue", 2);

            int stackSlots = byteCode.EmitLoad(symbol);

            await Assert.That(stackSlots).IsEqualTo(1);
            await Assert.That(byteCode.Code[^1].OpCode).IsEqualTo(OpCode.UpValue);
            await Assert.That(byteCode.Code[^1].Symbol).IsSameReferenceAs(symbol);
        }

        [global::TUnit.Core.Test]
        public async Task EmitStoreHandlesGlobalSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef env = SymbolRef.UpValue("_ENV", 0);
            SymbolRef symbol = SymbolRef.Global("globalValue", env);

            int stackSlots = byteCode.EmitStore(symbol, stackofs: 1, tupleidx: 2);

            await Assert.That(stackSlots).IsEqualTo(2);
            await Assert.That(byteCode.Code[^2].OpCode).IsEqualTo(OpCode.UpValue);
            Instruction setter = byteCode.Code[^1];
            await Assert.That(setter.OpCode).IsEqualTo(OpCode.IndexSet);
            await Assert.That(setter.NumVal).IsEqualTo(1);
            await Assert.That(setter.NumVal2).IsEqualTo(2);
            await Assert.That(setter.Value.String).IsEqualTo("globalValue");
        }

        [global::TUnit.Core.Test]
        public async Task EmitStoreHandlesLocalSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef symbol = SymbolRef.Local("localValue", 3);

            int stackSlots = byteCode.EmitStore(symbol, stackofs: 4, tupleidx: 1);

            await Assert.That(stackSlots).IsEqualTo(1);
            Instruction setter = byteCode.Code[^1];
            await Assert.That(setter.OpCode).IsEqualTo(OpCode.StoreLcl);
            await Assert.That(setter.NumVal).IsEqualTo(4);
            await Assert.That(setter.NumVal2).IsEqualTo(1);
            await Assert.That(setter.Symbol).IsSameReferenceAs(symbol);
        }

        [global::TUnit.Core.Test]
        public async Task EmitStoreHandlesUpValueSymbols()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef symbol = SymbolRef.UpValue("upvalue", 5);

            int stackSlots = byteCode.EmitStore(symbol, stackofs: 6, tupleidx: 0);

            await Assert.That(stackSlots).IsEqualTo(1);
            Instruction setter = byteCode.Code[^1];
            await Assert.That(setter.OpCode).IsEqualTo(OpCode.StoreUpv);
            await Assert.That(setter.NumVal).IsEqualTo(6);
            await Assert.That(setter.NumVal2).IsEqualTo(0);
            await Assert.That(setter.Symbol).IsSameReferenceAs(symbol);
        }

        [global::TUnit.Core.Test]
        public async Task EmitCleanFiltersSymbolsAboveScopeRange()
        {
            ByteCode byteCode = new(new Script());
            SymbolRef retained = SymbolRef.Local("retained", 5);
            RuntimeScopeBlock scope = new()
            {
                From = 0,
                To = 2,
                ToInclusive = 4,
                ToBeClosed = new[] { SymbolRef.Local("ignored", 1), retained },
            };

            Instruction instruction = byteCode.EmitClean(scope);

            await Assert.That(instruction.OpCode).IsEqualTo(OpCode.Clean);
            await Assert.That(instruction.NumVal).IsEqualTo(scope.To + 1);
            await Assert.That(instruction.NumVal2).IsEqualTo(scope.ToInclusive);
            await Assert.That(instruction.SymbolList.Length).IsEqualTo(1);
            await Assert.That(instruction.SymbolList[0]).IsSameReferenceAs(retained);
        }

        [global::TUnit.Core.Test]
        public async Task EmitBeginFnSerializesScopeMetadata()
        {
            ByteCode byteCode = new(new Script());
            RuntimeScopeFrame frame = new() { ToFirstBlock = 42 };
            frame.DebugSymbols.Add(SymbolRef.Local("a", 0));
            frame.DebugSymbols.Add(SymbolRef.Local("b", 1));

            Instruction instruction = byteCode.EmitBeginFn(frame);

            await Assert.That(instruction.OpCode).IsEqualTo(OpCode.BeginFn);
            await Assert.That(instruction.SymbolList.Length).IsEqualTo(frame.DebugSymbols.Count);
            await Assert.That(instruction.SymbolList[0].Name).IsEqualTo("a");
            await Assert.That(instruction.SymbolList[1].Name).IsEqualTo("b");
            await Assert.That(instruction.NumVal).IsEqualTo(frame.Count);
            await Assert.That(instruction.NumVal2).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task EmitTblInitIEncodesLastPositionFlagWhenTrue()
        {
            Instruction instruction = new ByteCode(new Script()).EmitTblInitI(lastpos: true);
            await Assert.That(instruction.OpCode).IsEqualTo(OpCode.TblInitI);
            await Assert.That(instruction.NumVal).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task EmitTblInitIEncodesLastPositionFlagWhenFalse()
        {
            Instruction instruction = new ByteCode(new Script()).EmitTblInitI(lastpos: false);
            await Assert.That(instruction.OpCode).IsEqualTo(OpCode.TblInitI);
            await Assert.That(instruction.NumVal).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task EmitDebugProducesDebugInstruction()
        {
            ByteCode byteCode = new(new Script());

            byteCode.EmitDebug("trace");

            await Assert.That(byteCode.Code[^1].OpCode).IsEqualTo(OpCode.Debug);
            await Assert.That(byteCode.Code[^1].Name).IsEqualTo("trace");
        }

        [global::TUnit.Core.Test]
        public async Task EmitLoadThrowsOnUnsupportedSymbolType()
        {
            ByteCode byteCode = new(new Script());
            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                byteCode.EmitLoad(SymbolRef.DefaultEnv)
            );
            await Assert.That(exception.Message).Contains("Unexpected symbol type");
        }

        [global::TUnit.Core.Test]
        public async Task EmitStoreThrowsOnUnsupportedSymbolType()
        {
            ByteCode byteCode = new(new Script());
            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                byteCode.EmitStore(SymbolRef.DefaultEnv, stackofs: 0, tupleidx: 0)
            );
            await Assert.That(exception.Message).Contains("Unexpected symbol type");
        }

        private static async Task AssertOperatorNormalization(OpCode input, OpCode appended)
        {
            ByteCode byteCode = new(new Script());
            byteCode.EmitOperator(input);

            await Assert.That(byteCode.Code[^2].OpCode).IsEqualTo(input);
            await Assert.That(byteCode.Code[^1].OpCode).IsEqualTo(appended);
        }

        private static async Task AssertIndexOpcode(
            bool isNameIndex,
            bool isExpList,
            OpCode expected
        )
        {
            ByteCode byteCode = new(new Script());
            Instruction instruction = byteCode.EmitIndex(
                DynValue.NewString("name"),
                isNameIndex,
                isExpList
            );

            await Assert.That(instruction.OpCode).IsEqualTo(expected);
            await Assert.That(instruction.Value.String).IsEqualTo("name");
        }

        private static async Task AssertIndexSetOpcode(
            bool isNameIndex,
            bool isExpList,
            OpCode expected
        )
        {
            ByteCode byteCode = new(new Script());
            Instruction instruction = byteCode.EmitIndexSet(
                stackofs: 1,
                tupleidx: 2,
                index: DynValue.NewString("idx"),
                isNameIndex: isNameIndex,
                isExpList: isExpList
            );

            await Assert.That(instruction.OpCode).IsEqualTo(expected);
            await Assert.That(instruction.NumVal).IsEqualTo(1);
            await Assert.That(instruction.NumVal2).IsEqualTo(2);
            await Assert.That(instruction.Value.String).IsEqualTo("idx");
        }
    }
}
#pragma warning restore CA2007
