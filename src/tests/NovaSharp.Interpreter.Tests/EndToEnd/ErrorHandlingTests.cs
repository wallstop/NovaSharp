namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class ErrorHandlingTests
    {
        [Test]
        public void PCallMultipleReturns()
        {
            string script = @"return pcall(function() return 1,2,3 end)";

            Script s = new();
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(4));
            Assert.That(res.Tuple[0].Boolean, Is.EqualTo(true));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(1));
            Assert.That(res.Tuple[2].Number, Is.EqualTo(2));
            Assert.That(res.Tuple[3].Number, Is.EqualTo(3));
        }

        [Test]
        public void ErrorsPCallClrFunction()
        {
            string script =
                @"
				r, msg = pcall(assert, false, 'catched')
				return r, msg;
								";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(2));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Boolean));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[0].Boolean, Is.EqualTo(false));
        }

        [Test]
        public void ErrorsPCallMultiples()
        {
            string script =
                @"
function try(fn)
	local x, y = pcall(fn)
	
	if (x) then
		return y
	else
		return '!'
	end
end

function a()
	return try(b) .. 'a';
end

function b()
	return try(c) .. 'b';
end

function c()
	return try(d) .. 'c';
end

function d()
	local t = { } .. 'x'
end


return a()
";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("!cba"));
        }

        [Test]
        public void ErrorsTryCatchMultiples()
        {
            string script =
                @"
function a()
	return try(b) .. 'a';
end

function b()
	return try(c) .. 'b';
end

function c()
	return try(d) .. 'c';
end

function d()
	local t = { } .. 'x'
end


return a()
";
            Script s = new(default(CoreModules))
            {
                Globals =
                {
                    ["try"] = DynValue.NewCallback(
                        (c, a) =>
                        {
                            try
                            {
                                DynValue v = a[0].Function.Call();
                                return v;
                            }
                            catch (ScriptRuntimeException)
                            {
                                return DynValue.NewString("!");
                            }
                        }
                    ),
                },
            };

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("!cba"));
        }
    }
}
