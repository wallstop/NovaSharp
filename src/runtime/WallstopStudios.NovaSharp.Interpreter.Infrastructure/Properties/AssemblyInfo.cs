using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("NovaSharp.Interpreter.Infrastructure")]
[assembly: AssemblyDescription(
    "Shared infrastructure primitives for the NovaSharp Lua interpreter"
)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://www.NovaSharp.org")]
[assembly: AssemblyProduct("NovaSharp.Interpreter.Infrastructure")]
[assembly: AssemblyCopyright("Copyright © 2014-2015, Marco Mastropaolo")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d5f8e3a1-7c2b-4e9f-8a6d-3b1c5e7f9d2a")]

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
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]

// Grant friend assembly access to tests
[assembly: InternalsVisibleTo("WallstopStudios.NovaSharp.Interpreter")]
[assembly: InternalsVisibleTo("WallstopStudios.NovaSharp.Interpreter.Tests")]
[assembly: InternalsVisibleTo("WallstopStudios.NovaSharp.Interpreter.Tests.TUnit")]
