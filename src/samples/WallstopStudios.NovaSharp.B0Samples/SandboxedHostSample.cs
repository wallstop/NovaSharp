namespace WallstopStudios.NovaSharp.B0Samples
{
    using Facade = global::NovaSharp;

    internal static class SandboxedHostSample
    {
        public static SandboxedHostResult Run()
        {
            Facade.LuaEngineOptions options = Facade.LuaEngineOptions.HardSandbox;

            using Facade.LuaEngine lua = Facade.LuaEngine.Create(options);
            lua.Globals["hostName"] = "NovaSharp";
            Facade.LuaTable state = lua.Run(
                    @"
return {
    host = hostName,
    ioKind = type(io),
    loadKind = type(load)
}"
                )
                .AsTable();

            return new SandboxedHostResult(
                state["host"].AsString(),
                state["ioKind"].AsString(),
                state["loadKind"].AsString()
            );
        }
    }

    internal sealed class SandboxedHostResult
    {
        public SandboxedHostResult(string hostName, string ioKind, string loadKind)
        {
            HostName = hostName;
            IoKind = ioKind;
            LoadKind = loadKind;
        }

        public string HostName { get; }

        public string IoKind { get; }

        public string LoadKind { get; }
    }
}
