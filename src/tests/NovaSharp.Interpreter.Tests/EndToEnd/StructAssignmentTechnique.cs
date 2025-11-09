namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NUnit.Framework;

    [TestFixture]
    public class StructAssignmentTechnique
    {
        public struct Vector3
        {
            public float x;
            public float y;
            public float z;
        }

        public class Transform
        {
            public Vector3 position;
        }

        public class Vector3Accessor
        {
            private readonly Transform _transf;

            public Vector3Accessor(Transform t)
            {
                _transf = t;
            }

            public float X
            {
                get { return _transf.position.x; }
                set { _transf.position.x = value; }
            }

            public float Y
            {
                get { return _transf.position.y; }
                set { _transf.position.y = value; }
            }

            public float Z
            {
                get { return _transf.position.z; }
                set { _transf.position.z = value; }
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

        //	T.position.X = 3;

        //	S.Globals["transform"] = T;

        //	S.DoString("transform.position.X = 15;");

        //	Assert.AreEqual(3, T.position.X);
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

            t.position.x = 3;

            s.Globals["transform"] = t;

            s.DoString("transform.position.X = 15;");

            Assert.That((int)t.position.x, Is.EqualTo(3));
            UserData.UnregisterType<Transform>();
            UserData.UnregisterType<Vector3>();
        }
    }
}
