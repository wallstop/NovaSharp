namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class StructAssignmentTechniqueTUnitTests
    {
        [SuppressMessage(
            "Performance",
            "CA1815:Override equals and operator equals on value types",
            Justification = "Struct mirrors NUnit test fixture exactly; value equality not required."
        )]
        internal struct Vector3
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        [SuppressMessage(
            "Performance",
            "CA1812:Avoid uninstantiated internal classes",
            Justification = "Instances are created when struct assignment test registers the type."
        )]
        internal sealed class Transform
        {
            public Vector3 Position { get; set; }
        }

        [SuppressMessage(
            "Performance",
            "CA1812:Avoid uninstantiated internal classes",
            Justification = "Type is instantiated during accessor creation in the test."
        )]
        internal sealed class Vector3Accessor
        {
            private readonly Transform _transform;

            public Vector3Accessor(Transform transform)
            {
                _transform = transform;
            }

            public float X
            {
                get { return _transform.Position.X; }
                set
                {
                    Vector3 current = _transform.Position;
                    current.X = value;
                    _transform.Position = current;
                }
            }

            public float Y
            {
                get { return _transform.Position.Y; }
                set
                {
                    Vector3 current = _transform.Position;
                    current.Y = value;
                    _transform.Position = current;
                }
            }

            public float Z
            {
                get { return _transform.Position.Z; }
                set
                {
                    Vector3 current = _transform.Position;
                    current.Z = value;
                    _transform.Position = current;
                }
            }
        }

        [global::TUnit.Core.Test]
        public async Task StructFieldCantSetThroughLua()
        {
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Create();
            registrationScope.Add<Transform>(ensureUnregistered: true);
            registrationScope.Add<Vector3>(ensureUnregistered: true);

            registrationScope.RegisterType<Transform>();
            registrationScope.RegisterType<Vector3>();

            Script script = new();
            Transform transform = new();
            _ = new Vector3Accessor(transform);

            transform.Position = new Vector3() { X = 3 };
            script.Globals["transform"] = transform;
            script.DoString("transform.Position.X = 15;");

            await Assert.That((int)transform.Position.X).IsEqualTo(3).ConfigureAwait(false);
        }
    }
}
