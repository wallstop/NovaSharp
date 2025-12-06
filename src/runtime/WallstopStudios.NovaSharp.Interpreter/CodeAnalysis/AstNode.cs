namespace WallstopStudios.NovaSharp.Interpreter.CodeAnalysis
{
    /// <summary>
    /// Marker base type for nodes emitted by the NovaSharp static analysis pipeline. Concrete node
    /// classes capture code insights/patterns that tools (linters, refactorings, etc.) operate on.
    /// </summary>
    internal class AstNode { }
}
