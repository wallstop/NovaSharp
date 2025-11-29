#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NovaSharp.Interpreter.Interop.UserDataRegistries;
    using NovaSharp.Interpreter.Tests;

    [UserDataIsolation]
    public sealed class UserDataIsolationTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IsolationScopeRestoresRegisteredTypes()
        {
            Type isolatedType = typeof(IsolatedType);
            TypeDescriptorRegistry.UnregisterType(isolatedType);

            using (UserData.BeginIsolationScope())
            {
                UserData.RegisterType(isolatedType);
                _ = new IsolatedType();
                await Assert.That(TypeDescriptorRegistry.IsTypeRegistered(isolatedType)).IsTrue();
            }

            await Assert.That(TypeDescriptorRegistry.IsTypeRegistered(isolatedType)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public Task IsolationScopeRestoresDefaultAccessModeReflection()
        {
            return IsolationScopeRestoresDefaultAccessModeAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task IsolationScopeRestoresDefaultAccessModePreoptimized()
        {
            return IsolationScopeRestoresDefaultAccessModeAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public async Task IsolationScopeRestoresRegistrationPolicy()
        {
            IRegistrationPolicy originalPolicy = TypeDescriptorRegistry.RegistrationPolicy;
            TestRegistrationPolicy customPolicy = new();

            try
            {
                using (UserData.BeginIsolationScope())
                {
                    TypeDescriptorRegistry.RegistrationPolicy = customPolicy;
                    await Assert
                        .That(TypeDescriptorRegistry.RegistrationPolicy)
                        .IsSameReferenceAs(customPolicy);
                }

                await Assert
                    .That(TypeDescriptorRegistry.RegistrationPolicy)
                    .IsSameReferenceAs(originalPolicy);
            }
            finally
            {
                TypeDescriptorRegistry.RegistrationPolicy = originalPolicy;
            }
        }

        [global::TUnit.Core.Test]
        public async Task IsolationScopeCoexistsWithConcurrentRegistrations()
        {
            Type targetType = typeof(ConcurrentIsolationType);
            TypeDescriptorRegistry.UnregisterType(targetType);
            _ = new ConcurrentIsolationType();

            int workerPairs = Math.Max(Environment.ProcessorCount, 4);
            int iterationsPerWorker = 250;

            Task[] tasks = new Task[workerPairs * 2];

            for (int i = 0; i < workerPairs; i++)
            {
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
                await Task.WhenAll(tasks);
            }
            catch (Exception exception)
            {
                StringBuilder builder = new();
                builder.AppendLine(
                    "Concurrent isolation scope cloning observed unexpected exceptions:"
                );

                if (exception is AggregateException aggregate)
                {
                    foreach (Exception inner in aggregate.Flatten().InnerExceptions)
                    {
                        builder.AppendLine(inner.ToString());
                    }
                }
                else
                {
                    builder.AppendLine(exception.ToString());
                }

                throw new InvalidOperationException(builder.ToString(), exception);
            }
            finally
            {
                TypeDescriptorRegistry.UnregisterType(targetType);
            }
        }

        private static async Task IsolationScopeRestoresDefaultAccessModeAsync(
            InteropAccessMode targetAccessMode
        )
        {
            InteropAccessMode original = TypeDescriptorRegistry.DefaultAccessMode;

            try
            {
                using (UserData.BeginIsolationScope())
                {
                    TypeDescriptorRegistry.DefaultAccessMode = targetAccessMode;
                    await Assert
                        .That(TypeDescriptorRegistry.DefaultAccessMode)
                        .IsEqualTo(targetAccessMode);
                }

                await Assert.That(TypeDescriptorRegistry.DefaultAccessMode).IsEqualTo(original);
            }
            finally
            {
                TypeDescriptorRegistry.DefaultAccessMode = original;
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
            )
            {
                return newDescriptor ?? oldDescriptor;
            }
        }
    }
}
#pragma warning restore CA2007
