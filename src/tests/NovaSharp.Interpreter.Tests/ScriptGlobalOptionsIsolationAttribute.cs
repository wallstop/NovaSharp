namespace NovaSharp.Interpreter.Tests
{
    using System;
    using NovaSharp.Interpreter;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class ScriptGlobalOptionsIsolationAttribute : NUnitAttribute, ITestAction
    {
        private IDisposable _scope;

        public void BeforeTest(ITest test)
        {
            _scope = Script.BeginGlobalOptionsScope();
        }

        public void AfterTest(ITest test)
        {
            _scope?.Dispose();
            _scope = null;
        }

        public ActionTargets Targets => ActionTargets.Test;
    }
}
