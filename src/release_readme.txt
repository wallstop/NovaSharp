MoonSharp [http://www.moonsharp.org]
------------------------------------

This archive contains all the files required to setup MoonSharp on your machine.

Contents:

 - /interpreter    -> The main DLL of the MoonSharp interpreter itself. 
                      Use this if you want to just embed the interpreter in your application.
                      
 - /vscodedebugger -> The DLL for the Visual Studio Code debugger facilities (plus the interpreter DLL itself). 
                      Use this if you want to embed the intepreter in your application with vscode debugging enabled.
                      
 - /remotedebugger -> The DLL for the remote debugger facilities (plus the interpreter DLL itself). 
                      Use this if you want to embed the intepreter in your application with remote debugging enabled.
                      
 - /repl           -> The REPL interpreter. It's not really meant for production as much as to quickly test scripts,
                      or to compile bytecode, or for hardwiring.

 - /unity          -> This contains a unity package you can use in your project. It includes interpreter and vscodedebugger.
                      
                      
Each directory contains, where applicable, subdirectories for the supported modern targets:

- netstandard2.1 :
  Use this build inside Unity 2021+, Mono, or any modern .NET deployment that consumes .NET Standard libraries.
  This is the canonical runtime surface and is compatible with .NET 6/7/8, IL2CPP, and current Unity editors.

- net8.0 :
  Desktop-first builds that light up additional tooling (CLI shell, benchmarks, test runner). Use these for development,
  automation, or when you need the latest .NET runtime features alongside MoonSharp.

- sources
This contains just the C# sources, with no project files. Import this in any project and you are ready to go. 
Stripped sources are available only for the interpreter and vscode debugger. For the other parts, see on github. 
Symbols might need to be defined to have it build correctly. Check the sources (you're on your own on this, sorry).


 


 
 


