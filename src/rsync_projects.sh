#!/bin/bash

# -- DO NOT CHANGE ORDER OTHERWISE WE RISK COPY OVERS...

echo Cleaning...
echo ... Unity
rm -R ./Unity/MoonSharp/Assets/Tests
rm -R ./Unity/MoonSharp/Assets/Plugins/MoonSharp/Interpreter
rm -R ./Unity/MoonSharp/Assets/Plugins/MoonSharp/Debugger
mkdir ./Unity/MoonSharp/Assets/Tests
mkdir ./Unity/MoonSharp/Assets/Plugins/MoonSharp/Interpreter
mkdir ./Unity/MoonSharp/Assets/Plugins/MoonSharp/Debugger

echo ... .NET Core
rm -R ./tests/TestRunners/DotNetCoreTestRunner/src
mkdir ./tests/TestRunners/DotNetCoreTestRunner/src

echo

echo Copying files...

echo ... Unity - interpreter
rsync -a --prune-empty-dirs --exclude 'AssemblyInfo.cs' --include '*/' --include '*.cs' --exclude '*' /git/my/moonsharp/src/runtime/MoonSharp.Interpreter/ ./Unity/MoonSharp/Assets/Plugins/MoonSharp/Interpreter/

echo ... Unity - vscode debugger...
rsync -a --prune-empty-dirs --exclude 'AssemblyInfo.cs' --include '*/' --include '*.cs' --exclude '*' /git/my/moonsharp/src/debuggers/MoonSharp.VsCodeDebugger/ ./Unity/MoonSharp/Assets/Plugins/MoonSharp/Debugger/

echo ... Unity - unit tests...
rsync -a --prune-empty-dirs --exclude 'AssemblyInfo.cs' --include '*/' --include '*.cs' --exclude '*' /git/my/moonsharp/src/tests/MoonSharp.Interpreter.Tests.Legacy/ ./Unity/MoonSharp/Assets/Tests

echo ... .NET Core - unit tests...
rsync -a --prune-empty-dirs --exclude 'AssemblyInfo.cs' --include '*/' --include '*.cs' --exclude '*' /git/my/moonsharp/src/tests/MoonSharp.Interpreter.Tests.Legacy/ ./tests/TestRunners/DotNetCoreTestRunner/src
