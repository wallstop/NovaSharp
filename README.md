NovaSharp       [![Build Status](https://travis-ci.org/xanathar/NovaSharp.svg?branch=master)](https://travis-ci.org/xanathar/NovaSharp) [![Build Status](https://img.shields.io/nuget/v/NovaSharp.svg)](https://www.nuget.org/packages/NovaSharp/)
=========
http://www.NovaSharp.org   


# Overview

This project is a port of [NovaSharp](http://www.NovaSharp.org). NovaSharp has not had a commit in over 4 years, and its last stable release was over 9 years ago. It would be awesome to have an active, maintained version of this project.

## Project Goals

- Active maintenance
- Ease of understanding: add significant documentation, making onboarding trivial
- Modernize the stack. Remove support for old .Net versions, commonalize on .Net Standard 2.1
- Target Lua 5.4.8 semantics (see `docs/LuaCompatibility.md` for tracked gaps)
- Fix all outstanding bugs
- Significantly increase performance
- Unity/Mono/IL2CPP-first

## AI Warning

In order to accomplish the above, I will be utilizing AI coding assistance. I will do my best to vet the quality of all code and this repo.

# Legacy README

A complete Lua solution written entirely in C# for the .NET, Mono, Xamarin and Unity3D platforms.

Features:
* Designed for near-complete compatibility with Lua 5.4.8 (remaining gaps tracked in `docs/LuaCompatibility.md`) 
* Support for metalua style anonymous functions (lambda-style)
* Easy to use API
* **Debugger** support for Visual Studio Code (PCL targets not supported)
* Remote debugger accessible with a web browser and Flash (PCL targets not supported)
* Targets .NET Standard 2.1 for compatibility with modern .NET (6/7/8), Mono, and Unity 2021+ (including IL2CPP)
* Runs on Ahead-of-time platforms like iOS through IL2CPP
* No external dependencies, implemented in as few targets as possible
* Easy and performant interop with CLR objects, with runtime code generation where supported
* Interop with methods, extension methods, overloads, fields, properties and indexers supported
* Support for the complete Lua standard library with very few exceptions (mostly located on the 'debug' module) and a few extensions (in the string library, mostly)
* Async helpers available on Task-based runtimes
* Supports dumping/loading bytecode for obfuscation and quicker parsing at runtime
* An embedded JSON parser (with no dependencies) to convert between JSON and Lua tables
* Easy opt-out of Lua standard library modules to sandbox what scripts can access
* Easy to use error handling (script errors are exceptions)
* Support for coroutines, including invocation of coroutines as C# iterators 
* REPL interpreter, plus facilities to easily implement your own REPL in few lines of code
* Complete XML help, and walkthroughs on http://www.NovaSharp.org

For highlights on differences between NovaSharp and standard Lua, see http://www.NovaSharp.org/moonluadifferences.html

Please see http://www.NovaSharp.org for downloads, infos, tutorials, etc.

Additional documentation:
- [Documentation Index](docs/README.md)
- [Contributing Guide](docs/Contributing.md)
- [Performance Benchmarks](docs/Performance.md)
- [Testing Guide](docs/Testing.md)
- [Modernization Notes](docs/Modernization.md)
- [Unity Integration](docs/UnityIntegration.md)
- [Helper Scripts Index](scripts/README.md)
- [Helper Scripts Index](scripts/README.md)


**License**

The program and libraries are released under a 3-clause BSD license - see the license section.

Parts of the string library are based on the KopiLua project (https://github.com/NLua/KopiLua).
Debugger icons are from the Eclipse project (https://www.eclipse.org/).


**Usage**

Use of the library is easy as:

```C#
double NovaSharpFactorial()
{
	string script = @"    
		-- defines a factorial function
		function fact (n)
			if (n == 0) then
				return 1
			else
				return n*fact(n - 1)
			end
		end

	return fact(5)";

	DynValue res = Script.RunString(script);
	return res.Number;
}
```

For more in-depth tutorials, samples, etc. please refer to http://www.NovaSharp.org/getting_started.html





