namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NovaSharp.Interpreter.Interop.UserDataRegistries;
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    public sealed class UserDataIsolationTests
    {
        [Test]
        public void IsolationScopeRestoresRegisteredTypes()
        {
            Type isolatedType = typeof(IsolatedType);
            TypeDescriptorRegistry.UnregisterType(isolatedType);

            using (UserData.BeginIsolationScope())
            {
                UserData.RegisterType(isolatedType);
                _ = new IsolatedType();
                Assert.That(TypeDescriptorRegistry.IsTypeRegistered(isolatedType), Is.True);
            }

            Assert.That(TypeDescriptorRegistry.IsTypeRegistered(isolatedType), Is.False);
        }

        [TestCase(InteropAccessMode.Reflection)]
        [TestCase(InteropAccessMode.Preoptimized)]
        public void IsolationScopeRestoresDefaultAccessMode(InteropAccessMode targetAccessMode)
        {
            InteropAccessMode original = TypeDescriptorRegistry.DefaultAccessMode;

            using (UserData.BeginIsolationScope())
            {
                TypeDescriptorRegistry.DefaultAccessMode = targetAccessMode;
                Assert.That(TypeDescriptorRegistry.DefaultAccessMode, Is.EqualTo(targetAccessMode));
            }

            Assert.That(TypeDescriptorRegistry.DefaultAccessMode, Is.EqualTo(original));
            TypeDescriptorRegistry.DefaultAccessMode = original;
        }

        [Test]
        public void IsolationScopeRestoresRegistrationPolicy()
        {
            IRegistrationPolicy originalPolicy = TypeDescriptorRegistry.RegistrationPolicy;
            TestRegistrationPolicy customPolicy = new();

            using (UserData.BeginIsolationScope())
            {
                TypeDescriptorRegistry.RegistrationPolicy = customPolicy;
                Assert.That(TypeDescriptorRegistry.RegistrationPolicy, Is.SameAs(customPolicy));
            }

            Assert.That(TypeDescriptorRegistry.RegistrationPolicy, Is.SameAs(originalPolicy));
            TypeDescriptorRegistry.RegistrationPolicy = originalPolicy;
        }

        [Test]
        public void IsolationScopeCoexistsWithConcurrentRegistrations()
        {
            Type targetType = typeof(ConcurrentIsolationType);
            TypeDescriptorRegistry.UnregisterType(targetType);
            _ = new ConcurrentIsolationType();

            int workerPairs = Math.Max(Environment.ProcessorCount, 4);
            int iterationsPerWorker = 250;
            TestContext.WriteLine(
                "Concurrent isolation stress: workers={0}, iterations={1}",
                workerPairs * 2,
                iterationsPerWorker
            );

            Task[] tasks = new Task[workerPairs * 2];

            for (int i = 0; i < workerPairs; i++)
            {
                int workerId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int iteration = 0; iteration < iterationsPerWorker; iteration++)
                    {
                        using (UserData.BeginIsolationScope())
                        {
                            _ = TypeDescriptorRegistry.IsTypeRegistered(targetType);
                        }
                    }
                });
            }

            for (int i = 0; i < workerPairs; i++)
            {
                int workerId = workerPairs + i;
                tasks[workerPairs + i] = Task.Run(() =>
                {
                    for (int iteration = 0; iteration < iterationsPerWorker; iteration++)
                    {
                        _ = UserData.RegisterType(targetType);
                        TypeDescriptorRegistry.UnregisterType(targetType);
                    }
                });
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException aggregateException)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(
                    "Concurrent isolation scope cloning observed unexpected exceptions:"
                );

                foreach (Exception exception in aggregateException.Flatten().InnerExceptions)
                {
                    builder.AppendLine(exception.ToString());
                }

                Assert.Fail(builder.ToString());
            }
            finally
            {
                TypeDescriptorRegistry.UnregisterType(targetType);
            }
        }

        private sealed class IsolatedType { }

        private sealed class ConcurrentIsolationType { }

        private sealed class TestRegistrationPolicy : IRegistrationPolicy
        {
            public bool AllowTypeAutoRegistration(Type type) => false;

            public IUserDataDescriptor HandleRegistration(
                IUserDataDescriptor newDescriptor,
                IUserDataDescriptor oldDescriptor
            ) => newDescriptor ?? oldDescriptor;
        }
    }
}
