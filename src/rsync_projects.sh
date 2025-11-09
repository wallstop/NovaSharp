#!/bin/bash

# -- DO NOT CHANGE ORDER OTHERWISE WE RISK COPY OVERS...

echo Cleaning...
echo ... Unity
rm -R ./Unity/NovaSharp/Assets/Tests
rm -R ./Unity/NovaSharp/Assets/Plugins/NovaSharp/Interpreter
rm -R ./Unity/NovaSharp/Assets/Plugins/NovaSharp/Debugger
mkdir ./Unity/NovaSharp/Assets/Tests
mkdir ./Unity/NovaSharp/Assets/Plugins/NovaSharp/Interpreter
mkdir ./Unity/NovaSharp/Assets/Plugins/NovaSharp/Debugger

echo ... .NET Core
rm -R ./tests/TestRunners/DotNetCoreTestRunner/src
mkdir ./tests/TestRunners/DotNetCoreTestRunner/src

echo

echo Copying files...

echo ... Unity - interpreter
rsync -a --prune-empty-dirs --exclude 'AssemblyInfo.cs' --include '*/' --include '*.cs' --exclude '*' /git/my/NovaSharp/src/runtime/NovaSharp.Interpreter/ ./Unity/NovaSharp/Assets/Plugins/NovaSharp/Interpreter/

echo ... Unity - vscode debugger...
rsync -a --prune-empty-dirs --exclude 'AssemblyInfo.cs' --include '*/' --include '*.cs' --exclude '*' /git/my/NovaSharp/src/debuggers/NovaSharp.VsCodeDebugger/ ./Unity/NovaSharp/Assets/Plugins/NovaSharp/Debugger/

echo ... Unity - unit tests...
rsync -a --prune-empty-dirs --exclude 'AssemblyInfo.cs' --include '*/' --include '*.cs' --exclude '*' /git/my/NovaSharp/src/tests/NovaSharp.Interpreter.Tests.Legacy/ ./Unity/NovaSharp/Assets/Tests

echo ... .NET Core - unit tests...
rsync -a --prune-empty-dirs --exclude 'AssemblyInfo.cs' --include '*/' --include '*.cs' --exclude '*' /git/my/NovaSharp/src/tests/NovaSharp.Interpreter.Tests.Legacy/ ./tests/TestRunners/DotNetCoreTestRunner/src
