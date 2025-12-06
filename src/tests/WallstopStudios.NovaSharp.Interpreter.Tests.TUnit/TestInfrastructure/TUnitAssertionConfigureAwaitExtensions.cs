namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure
{
    using System.Runtime.CompilerServices;
    using global::TUnit.Assertions.Core;

    internal static class TUnitAssertionConfigureAwaitExtensions
    {
        internal static ConfiguredTaskAwaitable ConfigureAwait(
            this IAssertion assertion,
            bool continueOnCapturedContext
        )
        {
            return assertion.AssertAsync().ConfigureAwait(continueOnCapturedContext);
        }

        internal static ConfiguredTaskAwaitable<TValue> ConfigureAwait<TValue>(
            this Assertion<TValue> assertion,
            bool continueOnCapturedContext
        )
        {
            return assertion.AssertAsync().ConfigureAwait(continueOnCapturedContext);
        }
    }
}
