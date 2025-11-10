namespace NovaSharp.Interpreter.Tests
{
    using System;
    using System.Collections.Generic;
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
        public string testName;
        public string message;
        public Exception exception;
        public TestResultType type;
    }

    internal sealed class SkipThisTestException : Exception { }

    public class TestRunner
    {
        private readonly Action<TestResult> _loggerAction;
        public int ok = 0;
        public int fail = 0;
        public int total = 0;
        public int skipped = 0;

        public static bool IsRunning { get; private set; }

        public TestRunner(Action<TestResult> loggerAction)
        {
            IsRunning = true;

            _loggerAction = loggerAction;

            Console_WriteLine(
                "NovaSharp Test Suite Runner - {0} [{1}]",
                Script.VERSION,
                Script.GlobalOptions.Platform.GetPlatformName()
            );
            Console_WriteLine("http://www.NovaSharp.org");
            Console_WriteLine("");
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
                        Framework
                            .Do.GetCustomAttributes(t, typeof(TestFixtureAttribute), true)
                            .Any()
                    )
                    .ToArray();

#if UNITY_EDITOR_OSX
            System.IO.File.WriteAllLines("/temp/types.cs", types.Select(t => t.FullName).ToArray());
#endif

            Console_WriteLine("Found {0} test types.", types.Length);

            foreach (Type t in types)
            {
                MethodInfo[] tests = Framework
                    .Do.GetMethods(t)
                    .Where(m => m.GetCustomAttributes(typeof(TestAttribute), true).Any())
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
                        ++skipped;
                        TestResult trs = new()
                        {
                            testName = mi.Name,
                            message = "skipped (skip-list)",
                            type = TestResultType.Skipped,
                        };
                        yield return trs;
                        continue;
                    }

                    TestResult tr = RunTest(t, mi);

                    if (tr.type != TestResultType.Message)
                    {
                        if (tr.type == TestResultType.Fail)
                        {
                            ++fail;
                        }
                        else if (tr.type == TestResultType.Ok)
                        {
                            ++ok;
                        }
                        else
                        {
                            ++skipped;
                        }

                        ++total;
                    }

                    yield return tr;
                }
            }

            Console_WriteLine("");
            Console_WriteLine(
                "OK : {0}/{2}, Failed {1}/{2}, Skipped {3}/{2}",
                ok,
                fail,
                total,
                skipped
            );
        }

        private void Console_WriteLine(string message, params object[] args)
        {
            _loggerAction(
                new TestResult()
                {
                    type = TestResultType.Message,
                    message = string.Format(message, args),
                }
            );
        }

        private static TestResult RunTest(Type t, MethodInfo mi)
        {
            if (mi.GetCustomAttributes(typeof(IgnoreAttribute), true).Any())
            {
                return new TestResult()
                {
                    testName = mi.Name,
                    message = "skipped",
                    type = TestResultType.Skipped,
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
                mi.Invoke(o, new object[0]);

                if (expectedEx != null)
                {
                    return new TestResult()
                    {
                        testName = mi.Name,
                        message = $"Exception {expectedEx.ExpectedException} expected",
                        type = TestResultType.Fail,
                    };
                }
                else
                {
                    return new TestResult()
                    {
                        testName = mi.Name,
                        message = "ok",
                        type = TestResultType.Ok,
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
                        testName = mi.Name,
                        message = "skipped",
                        type = TestResultType.Skipped,
                    };
                }

                if (
                    expectedEx != null
                    && Framework.Do.IsInstanceOfType(expectedEx.ExpectedException, ex)
                )
                {
                    return new TestResult()
                    {
                        testName = mi.Name,
                        message = "ok",
                        type = TestResultType.Ok,
                    };
                }
                else
                {
                    return new TestResult()
                    {
                        testName = mi.Name,
                        message = BuildExceptionMessage(ex),
                        type = TestResultType.Fail,
                        exception = ex,
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
    }
}
