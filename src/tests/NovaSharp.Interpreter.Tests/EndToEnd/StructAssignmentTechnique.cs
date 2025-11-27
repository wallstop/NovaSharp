namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using DataTypes;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    [UserDataIsolation]
    public class StructAssignmentTechnique
    {
        internal struct Vector3
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        internal sealed class Transform
        {
            public Vector3 Position { get; set; }
        }

        internal sealed class Vector3Accessor
        {
            private readonly Transform _transf;

            public Vector3Accessor(Transform t)
            {
                _transf = t;
            }

            public float X
            {
                get { return _transf.Position.X; }
                set
                {
                    Vector3 current = _transf.Position;
                    current.X = value;
                    _transf.Position = current;
                }
            }

            public float Y
            {
                get { return _transf.Position.Y; }
                set
                {
                    Vector3 current = _transf.Position;
                    current.Y = value;
                    _transf.Position = current;
                }
            }

            public float Z
            {
                get { return _transf.Position.Z; }
                set
                {
                    Vector3 current = _transf.Position;
                    current.Z = value;
                    _transf.Position = current;
                }
            }
        }

        //[Test]
        //public void StructFieldCanSetWithWorkaround()
        //{
        //	UserData.RegisterType<Vector3>();
        //	UserData.RegisterType<Vector3_Accessor>();

        //	DispatchingUserDataDescriptor descr = (DispatchingUserDataDescriptor)UserData.RegisterType<Transform>();

        //	descr.AddMember("Position", new

        //	Script S = new Script();

        //	Transform T = new Transform();

        //	T.Position.X = 3;

        //	S.Globals["transform"] = T;

        //	S.DoString("transform.Position.X = 15;");

        //	Assert.AreEqual(3, T.Position.X);
        //	UserData.UnregisterType<Transform>();
        //	UserData.UnregisterType<Vector3>();
        //	UserData.UnregisterType<Vector3_Accessor>();
        //}

        [Test]
        public void StructFieldCantSet()
        {
            UserData.RegisterType<Transform>();
            UserData.RegisterType<Vector3>();

            Script s = new();

            Transform t = new();
            Vector3Accessor accessor = new(t);
            _ = accessor.X;

            t.Position = new Vector3() { X = 3 };

            s.Globals["transform"] = t;

            s.DoString("transform.Position.X = 15;");

            Assert.That((int)t.Position.X, Is.EqualTo(3));
            UserData.UnregisterType<Transform>();
            UserData.UnregisterType<Vector3>();
        }
    }
}
