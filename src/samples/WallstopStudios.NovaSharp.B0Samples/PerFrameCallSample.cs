namespace WallstopStudios.NovaSharp.B0Samples
{
    using Facade = global::NovaSharp;

    internal static class PerFrameCallSample
    {
        public static double Run(int frameCount, double deltaTime)
        {
            using Facade.LuaEngine lua = Facade.LuaEngine.Create();
            Facade.LuaFunction onUpdate = lua.Run(
                    @"
local elapsed = 0
return function(dt)
    elapsed = elapsed + dt
    return elapsed
end"
                )
                .AsFunction();

            Facade.LuaValue elapsed = Facade.LuaValue.Nil;
            for (int frame = 0; frame < frameCount; frame++)
            {
                elapsed = onUpdate.Call(deltaTime);
            }

            return elapsed.AsNumber();
        }
    }
}
