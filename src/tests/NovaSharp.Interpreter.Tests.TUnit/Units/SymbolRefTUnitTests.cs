#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class SymbolRefTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GlobalSymbolSerializationRoundtrips()
        {
            SymbolRef original = SymbolRef.Global("foo", SymbolRef.DefaultEnv);
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            original.WriteBinary(writer);
            original.WriteBinaryEnv(
                writer,
                new Dictionary<SymbolRef, int> { [SymbolRef.DefaultEnv] = 0, [original] = 1 }
            );

            writer.Flush();
            stream.Position = 0;

            using BinaryReader reader = new(stream);
            SymbolRef restored = SymbolRef.ReadBinary(reader);
            restored.ReadBinaryEnv(reader, new[] { SymbolRef.DefaultEnv, restored });

            await Assert.That(restored.Type).IsEqualTo(SymbolRefType.Global);
            await Assert.That(restored.Name).IsEqualTo("foo");
            await Assert.That(restored.Environment).IsEqualTo(SymbolRef.DefaultEnv);
        }

        [global::TUnit.Core.Test]
        public async Task LocalSymbolPreservesIndexAndName()
        {
            SymbolRef original = SymbolRef.Local("bar", 3);

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            original.WriteBinary(writer);
            original.WriteBinaryEnv(writer, new Dictionary<SymbolRef, int>());
            writer.Flush();
            stream.Position = 0;

            using BinaryReader reader = new(stream);
            SymbolRef restored = SymbolRef.ReadBinary(reader);
            restored.ReadBinaryEnv(reader, new SymbolRef[] { restored });

            await Assert.That(restored.Type).IsEqualTo(SymbolRefType.Local);
            await Assert.That(restored.Index).IsEqualTo(3);
            await Assert.That(restored.Name).IsEqualTo("bar");
            await Assert.That(restored.Environment).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task LocalSymbolExposesConstAndToBeClosedFlags()
        {
            SymbolRef symbol = SymbolRef.Local(
                "constClose",
                7,
                SymbolRefAttributes.Const | SymbolRefAttributes.ToBeClosed
            );

            await Assert.That(symbol.IsConst).IsTrue();
            await Assert.That(symbol.IsToBeClosed).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ToStringReflectsSymbolType()
        {
            SymbolRef env = SymbolRef.DefaultEnv;
            SymbolRef global = SymbolRef.Global("foo", env);
            SymbolRef local = SymbolRef.Local("bar", 2);

            await Assert.That(env.ToString()).IsEqualTo("(default _ENV)");
            await Assert.That(global.ToString()).Contains("foo");
            await Assert.That(local.ToString()).Contains("bar");
        }

        [global::TUnit.Core.Test]
        public async Task UpValueSymbolSerializationRetainsIndex()
        {
            SymbolRef original = SymbolRef.UpValue("baz", 5);

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            original.WriteBinary(writer);
            original.WriteBinaryEnv(writer, new Dictionary<SymbolRef, int>());
            writer.Flush();
            stream.Position = 0;

            using BinaryReader reader = new(stream);
            SymbolRef restored = SymbolRef.ReadBinary(reader);
            restored.ReadBinaryEnv(reader, new SymbolRef[] { restored });

            await Assert.That(restored.Type).IsEqualTo(SymbolRefType.UpValue);
            await Assert.That(restored.Index).IsEqualTo(5);
            await Assert.That(restored.Name).IsEqualTo("baz");
            await Assert.That(restored.Environment).IsNull();
        }
    }
}
#pragma warning restore CA2007
