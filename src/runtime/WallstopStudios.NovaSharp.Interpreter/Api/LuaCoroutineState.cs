namespace NovaSharp
{
    /// <summary>
    /// Public coroutine state exposed by the facade API.
    /// </summary>
    public enum LuaCoroutineState
    {
        Unknown = 0,
        Main = 1,
        NotStarted = 2,
        Suspended = 3,
        ForceSuspended = 4,
        Running = 5,
        Dead = 6,
    }
}
