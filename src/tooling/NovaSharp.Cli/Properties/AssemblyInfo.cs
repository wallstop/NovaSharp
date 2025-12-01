using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("NovaSharp.Cli")]
[assembly: AssemblyDescription("REPL Interpreter for the NovaSharp language")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://www.NovaSharp.org")]
[assembly: AssemblyProduct("NovaSharp.Cli")]
[assembly: AssemblyCopyright("Copyright © 2014-2015, Marco Mastropaolo")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("5298f488-204c-4936-9737-104f3dc4fc6c")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(NovaSharp.Interpreter.Script.VERSION)]
[assembly: AssemblyFileVersion(NovaSharp.Interpreter.Script.VERSION)]
[assembly: InternalsVisibleTo("NovaSharp.Interpreter.Tests")]
[assembly: InternalsVisibleTo("NovaSharp.Interpreter.Tests.TUnit")]
[assembly: InternalsVisibleTo("NovaSharp.RemoteDebugger.Tests.TUnit")]
