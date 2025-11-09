using NovaSharp.Commands;
using NUnit.Framework;

namespace NovaSharp.Interpreter.Tests.Units
{
    [TestFixture]
    public class ShellCommandManagerTests
    {
        private TextWriter _originalOut = null!;
        private TextWriter _originalError = null!;
        private StringWriter _writer = null!;
        private ShellContext _context = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }
        }

        [SetUp]
        public void SetUp()
        {
            _context = new ShellContext(new Script());

            _writer = new StringWriter();
            _originalOut = Console.Out;
            _originalError = Console.Error;

            Console.SetOut(_writer);
            Console.SetError(_writer);
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
            _writer.Dispose();
        }

        [Test]
        public void HelpWithoutArgumentsListsCommands()
        {
            string output = Execute("help");

            StringAssert.Contains("Type Lua code to execute Lua code", output);
            StringAssert.Contains("Commands:", output);
            StringAssert.Contains("!help [command] - gets the list of possible commands", output);
            StringAssert.Contains("!run <filename> - Executes the specified Lua script", output);
        }

        [Test]
        public void HelpTrimsWhitespaceAroundArguments()
        {
            string output = Execute("   help   run   ");

            StringAssert.Contains("run <filename> - Executes the specified Lua script.", output);
        }

        [Test]
        public void UnknownCommandReportsAnError()
        {
            string output = Execute("nope");

            StringAssert.Contains("Invalid command 'nope'.", output);
        }

        private string Execute(string commandLine)
        {
            CommandManager.Execute(_context, commandLine);
            return _writer.ToString();
        }
    }
}
