namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;

#pragma warning disable CA2007 // ConfigureAwait noise is intentionally suppressed in test helpers.
    internal static class EndToEndDynValueAssert
    {
        public static async Task ExpectAsync(DynValue actual, params object[] expected)
        {
            ArgumentNullException.ThrowIfNull(actual);

            if (expected == null)
            {
                expected = new object[] { null };
            }
            else if (expected.Length == 0)
            {
                expected = new object[] { DataType.Void };
            }

            if (expected.Length == 1)
            {
                await AssertValueAsync(actual, expected[0]);
                return;
            }

            await AssertTupleAsync(actual, expected);
        }

        private static async Task AssertTupleAsync(DynValue value, object[] expected)
        {
            await Assert.That(value.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(value.Tuple.Length).IsEqualTo(expected.Length);

            for (int index = 0; index < expected.Length; index++)
            {
                await AssertValueAsync(value.Tuple[index], expected[index]);
            }
        }

        private static async Task AssertValueAsync(DynValue value, object expected)
        {
            if (expected is DataType dataType)
            {
                await Assert.That(value.Type).IsEqualTo(dataType);
                return;
            }
            else if (expected == null)
            {
                await Assert.That(value.Type).IsEqualTo(DataType.Nil);
            }
            else if (expected is bool boolean)
            {
                await Assert.That(value.Type).IsEqualTo(DataType.Boolean);
                await Assert.That(value.Boolean).IsEqualTo(boolean);
            }
            else if (expected is double doubleValue)
            {
                await Assert.That(value.Type).IsEqualTo(DataType.Number);
                await Assert.That(value.Number).IsEqualTo(doubleValue);
            }
            else if (expected is int intValue)
            {
                await Assert.That(value.Type).IsEqualTo(DataType.Number);
                await Assert.That(value.Number).IsEqualTo(intValue);
            }
            else if (expected is string stringValue)
            {
                await Assert.That(value.Type).IsEqualTo(DataType.String);
                await Assert.That(value.String).IsEqualTo(stringValue);
            }
            else
            {
                throw new NotSupportedException(
                    $"Unsupported expectation type '{expected.GetType().FullName}'."
                );
            }
        }
    }
#pragma warning restore CA2007
}
