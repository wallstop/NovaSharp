namespace NovaSharp.Interpreter.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Compatibility;
    using NovaSharp.Interpreter;
    using NUnit.Framework;

    public enum TestResultType
    {
        Message,
        Ok,
        Fail,
        Skipped,
    }

    public class TestResult
    {
        public string TestName { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public TestResultType Type { get; set; }
    }

    internal sealed class SkipThisTestException : Exception
    {
        public SkipThisTestException() { }

        public SkipThisTestException(string message)
            : base(message) { }

        public SkipThisTestException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    public class TestRunner
    {
        private readonly Action<TestResult> _loggerAction;

        public int OkCount { get; private set; }
        public int FailCount { get; private set; }
        public int TotalCount { get; private set; }
        public int SkippedCount { get; private set; }

        public static bool IsRunning { get; private set; }

        public TestRunner(Action<TestResult> loggerAction)
        {
            IsRunning = true;

            _loggerAction = loggerAction;

            ConsoleWriteLine(
                "NovaSharp Test Suite Runner - {0} [{1}]",
                Script.VERSION,
                Script.GlobalOptions.Platform.GetPlatformName()
            );
            ConsoleWriteLine("http://www.NovaSharp.org");
            ConsoleWriteLine("");
        }

        public void Test(string whichTest = null, string[] testsToSkip = null)
        {
            foreach (TestResult tr in IterateOnTests(whichTest, testsToSkip))
            {
                _loggerAction(tr);
            }
        }

        public IEnumerable<TestResult> IterateOnTests(
            string whichTest = null,
            string[] testsToSkip = null,
            Type[] types = null
        )
        {
            HashSet<string> skipList = new();

            if (testsToSkip != null)
            {
                skipList.UnionWith(testsToSkip);
            }

            Assembly asm = Framework.Do.GetAssembly(typeof(TestRunner));

            types =
                types
                ?? Framework
                    .Do.GetAssemblyTypes(asm)
                    .Where(t =>
                    {
                        object[] fixtures = Framework.Do.GetCustomAttributes(
                            t,
                            typeof(TestFixtureAttribute),
                            true
                        );
                        return fixtures.Length != 0;
                    })
                    .ToArray();

#if UNITY_EDITOR_OSX
            System.IO.File.WriteAllLines("/temp/types.cs", types.Select(t => t.FullName).ToArray());
#endif

            ConsoleWriteLine("Found {0} test types.", types.Length);

            foreach (Type t in types)
            {
                MethodInfo[] tests = Framework
                    .Do.GetMethods(t)
                    .Where(m => m.GetCustomAttributes(typeof(TestAttribute), true).Length != 0)
                    .ToArray();
                //Console_WriteLine("Testing {0} - {1} tests found.", t.Name, tests.Length);

                foreach (MethodInfo mi in tests)
                {
                    if (whichTest != null && mi.Name != whichTest)
                    {
                        continue;
                    }

                    if (skipList.Contains(mi.Name))
                    {
                        SkippedCount++;
                        TestResult trs = new()
                        {
                            TestName = mi.Name,
                            Message = "skipped (skip-list)",
                            Type = TestResultType.Skipped,
                        };
                        yield return trs;
                        continue;
                    }

                    TestResult tr = RunTest(t, mi);

                    if (tr.Type != TestResultType.Message)
                    {
                        if (tr.Type == TestResultType.Fail)
                        {
                            FailCount++;
                        }
                        else if (tr.Type == TestResultType.Ok)
                        {
                            OkCount++;
                        }
                        else
                        {
                            SkippedCount++;
                        }

                        TotalCount++;
                    }

                    yield return tr;
                }
            }

            ConsoleWriteLine("");
            ConsoleWriteLine(
                "OK : {0}/{2}, Failed {1}/{2}, Skipped {3}/{2}",
                OkCount,
                FailCount,
                TotalCount,
                SkippedCount
            );
        }

        private void ConsoleWriteLine(string message, params object[] args)
        {
            _loggerAction(
                new TestResult()
                {
                    Type = TestResultType.Message,
                    Message = FormatString(message, args),
                }
            );
        }

        private static TestResult RunTest(Type t, MethodInfo mi)
        {
            if (mi.GetCustomAttributes(typeof(IgnoreAttribute), true).Length != 0)
            {
                return new TestResult()
                {
                    TestName = mi.Name,
                    Message = "skipped",
                    Type = TestResultType.Skipped,
                };
            }

            ExpectedExceptionAttribute expectedEx = mi.GetCustomAttributes(
                    typeof(ExpectedExceptionAttribute),
                    true
                )
                .OfType<ExpectedExceptionAttribute>()
                .FirstOrDefault();

            try
            {
                object o = Activator.CreateInstance(t);
                mi.Invoke(o, Array.Empty<object>());

                if (expectedEx != null)
                {
                    return new TestResult()
                    {
                        TestName = mi.Name,
                        Message = $"Exception {expectedEx.ExpectedException} expected",
                        Type = TestResultType.Fail,
                    };
                }
                else
                {
                    return new TestResult()
                    {
                        TestName = mi.Name,
                        Message = "ok",
                        Type = TestResultType.Ok,
                    };
                }
            }
            catch (TargetInvocationException tiex)
            {
                Exception ex = tiex.InnerException;

                if (ex is SkipThisTestException)
                {
                    return new TestResult()
                    {
                        TestName = mi.Name,
                        Message = "skipped",
                        Type = TestResultType.Skipped,
                    };
                }

                if (
                    expectedEx != null
                    && Framework.Do.IsInstanceOfType(expectedEx.ExpectedException, ex)
                )
                {
                    return new TestResult()
                    {
                        TestName = mi.Name,
                        Message = "ok",
                        Type = TestResultType.Ok,
                    };
                }
                else
                {
                    return new TestResult()
                    {
                        TestName = mi.Name,
                        Message = BuildExceptionMessage(ex),
                        Type = TestResultType.Fail,
                        Exception = ex,
                    };
                }
            }
        }

        private static string BuildExceptionMessage(Exception ex)
        {
            StringBuilder sb = new();

            for (Exception e = ex; e != null; e = e.InnerException)
            {
                sb.Append(">>> ");
                sb.Append(e.Message);
            }

            return sb.ToString();
        }

        internal static void Skip()
        {
            if (IsRunning)
            {
                throw new SkipThisTestException();
            }
        }

        private static string FormatString(string format, object[] args)
        {
            ArgumentNullException.ThrowIfNull(format);

            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
