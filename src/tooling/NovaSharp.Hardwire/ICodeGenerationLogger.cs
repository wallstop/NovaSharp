namespace NovaSharp.Hardwire
{
    public interface ICodeGenerationLogger
    {
        public void LogError(string message);
        public void LogWarning(string message);
        public void LogMinor(string message);
    }
}
