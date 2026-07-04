namespace NovaSharp
{
    /// <summary>
    /// Resolves and loads Lua source or dumped bytecode for a facade engine.
    /// </summary>
    public interface ILuaScriptLoader
    {
        /// <summary>
        /// Loads script content from a resolved file name.
        /// </summary>
        public object LoadFile(string file, LuaTable globals);

        /// <summary>
        /// Resolves a file name before loading.
        /// </summary>
        public string ResolveFileName(string filename, LuaTable globals);

        /// <summary>
        /// Resolves a module name before loading.
        /// </summary>
        public string ResolveModuleName(string moduleName, LuaTable globals);
    }
}
