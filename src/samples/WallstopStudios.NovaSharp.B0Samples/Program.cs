namespace WallstopStudios.NovaSharp.B0Samples
{
    using System;

    internal static class Program
    {
        public static int Main()
        {
            HelloWorldResult hello = HelloWorldSample.Run();
            double elapsed = PerFrameCallSample.Run(frameCount: 120, deltaTime: 1.0 / 60.0);
            SandboxedHostResult sandbox = SandboxedHostSample.Run();

            Console.WriteLine($"hello: {hello.PrintedLine}; answer={hello.Answer}");
            Console.WriteLine($"per-frame: elapsed={elapsed:0.000}");
            Console.WriteLine(
                $"sandbox: host={sandbox.HostName}; io={sandbox.IoKind}; load={sandbox.LoadKind}"
            );

            if (hello.Answer != 42 || hello.PrintedLine.Length == 0)
            {
                return 1;
            }

            if (elapsed <= 0)
            {
                return 2;
            }

            return sandbox.IoKind == "nil" && sandbox.LoadKind == "nil" ? 0 : 3;
        }
    }
}
