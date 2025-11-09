using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace NovaSharp.Interpreter.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ExpectedExceptionAttribute : NUnitAttribute, IWrapTestMethod
    {
        public Type ExpectedException { get; }

        public ExpectedExceptionAttribute(Type expectedException)
        {
            ExpectedException =
                expectedException ?? throw new ArgumentNullException(nameof(expectedException));
        }

        public TestCommand Wrap(TestCommand command) =>
            new ExpectedExceptionCommand(command, ExpectedException);

        private sealed class ExpectedExceptionCommand : DelegatingTestCommand
        {
            private readonly Type _expectedType;

            public ExpectedExceptionCommand(TestCommand innerCommand, Type expectedType)
                : base(innerCommand)
            {
                _expectedType = expectedType;
            }

            public override NUnit.Framework.Internal.TestResult Execute(
                TestExecutionContext context
            )
            {
                try
                {
                    innerCommand.Execute(context);
                    context.CurrentResult.SetResult(
                        ResultState.Failure,
                        $"Expected exception of type {_expectedType.FullName} was not thrown."
                    );
                }
                catch (Exception ex)
                {
                    Exception actual = Unwrap(ex);

                    if (_expectedType.IsInstanceOfType(actual))
                    {
                        context.CurrentResult.SetResult(ResultState.Success);
                    }
                    else
                    {
                        throw;
                    }
                }

                return context.CurrentResult;
            }

            private static Exception Unwrap(Exception exception)
            {
                if (exception is NUnitException || exception is TargetInvocationException)
                {
                    return exception.InnerException ?? exception;
                }

                return exception;
            }
        }
    }
}
