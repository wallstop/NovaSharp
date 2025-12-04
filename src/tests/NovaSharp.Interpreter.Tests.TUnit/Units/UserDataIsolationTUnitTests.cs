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
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class UserDataIsolationTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IsolationScopeRestoresRegisteredTypes()
        {
            Type isolatedType = typeof(IsolatedType);
            TypeDescriptorRegistry.UnregisterType(isolatedType);

            using (UserDataIsolationScope.Begin())
            {
                // Intentional direct call: this test ensures isolation scopes rewind raw UserData registrations.
                UserData.RegisterType(isolatedType);
                _ = new IsolatedType();
                await Assert
                    .That(TypeDescriptorRegistry.IsTypeRegistered(isolatedType))
                    .IsTrue()
                    .ConfigureAwait(false);
            }

            await Assert
                .That(TypeDescriptorRegistry.IsTypeRegistered(isolatedType))
                .IsFalse()
                .ConfigureAwait(false);
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
            using StaticValueScope<IRegistrationPolicy> registrationPolicyScope =
                StaticValueScope<IRegistrationPolicy>.Capture(
                    () => TypeDescriptorRegistry.RegistrationPolicy,
                    value => TypeDescriptorRegistry.RegistrationPolicy = value
                );

            using (UserDataIsolationScope.Begin())
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

        [global::TUnit.Core.Test]
        public async Task IsolationScopeCoexistsWithConcurrentRegistrations()
        {
            Type targetType = typeof(ConcurrentIsolationType);
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track(
                targetType,
                ensureUnregistered: true
            );
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
                        using (UserDataIsolationScope.Begin())
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
                        // Intentional direct call: concurrent registration/unregistration exercises the raw API surface.
                        _ = UserData.RegisterType(targetType);
                        TypeDescriptorRegistry.UnregisterType(targetType);
                    }
                });
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
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
        }

        private static async Task IsolationScopeRestoresDefaultAccessModeAsync(
            InteropAccessMode targetAccessMode
        )
        {
            InteropAccessMode original = TypeDescriptorRegistry.DefaultAccessMode;

            using (UserDataIsolationScope.Begin())
            {
                TypeDescriptorRegistry.DefaultAccessMode = targetAccessMode;
                await Assert
                    .That(TypeDescriptorRegistry.DefaultAccessMode)
                    .IsEqualTo(targetAccessMode);
            }

            await Assert
                .That(TypeDescriptorRegistry.DefaultAccessMode)
                .IsEqualTo(original)
                .ConfigureAwait(false);
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
