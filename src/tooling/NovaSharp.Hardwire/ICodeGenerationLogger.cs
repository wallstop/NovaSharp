namespace NovaSharp.Hardwire
{
    public interface ICodeGenerationLogger
    {
        void LogError(string message);
        void LogWarning(string message);
        void LogMinor(string message);
    }
}
