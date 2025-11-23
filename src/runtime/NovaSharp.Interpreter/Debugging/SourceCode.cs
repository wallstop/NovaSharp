namespace NovaSharp.Interpreter.Debugging
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Class representing the source code of a given script
    /// </summary>
    public class SourceCode : IScriptPrivateResource
    {
        /// <summary>
        /// Gets the name of the source code
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the source code as a string
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Gets the source code lines.
        /// </summary>
        private string[] _lines = Array.Empty<string>();

        public IReadOnlyList<string> Lines => _lines;

        /// <summary>
        /// Gets the script owning this resource.
        /// </summary>
        public Script OwnerScript { get; private set; }

        /// <summary>
        /// Gets the source identifier inside a script
        /// </summary>
        public int SourceId { get; private set; }

        internal List<SourceRef> Refs { get; private set; }

        internal SourceCode(string name, string code, int sourceId, Script ownerScript)
        {
            Refs = new List<SourceRef>();

            List<string> lines = new();

            Name = name;
            Code = code;

            lines.Add($"-- Begin of chunk : {name} ");

            lines.AddRange(Code.Split('\n'));

            _lines = lines.ToArray();

            OwnerScript = ownerScript;
            SourceId = sourceId;
        }

        /// <summary>
        /// Gets the code snippet represented by a source ref
        /// </summary>
        /// <param name="sourceCodeRef">The source code reference.</param>
        /// <returns></returns>
        public string GetCodeSnippet(SourceRef sourceCodeRef)
        {
            if (sourceCodeRef == null)
            {
                throw new ArgumentNullException(nameof(sourceCodeRef));
            }

            if (sourceCodeRef.FromLine == sourceCodeRef.ToLine)
            {
                ReadOnlySpan<char> lineSpan = Lines[sourceCodeRef.FromLine].AsSpan();
                int from = AdjustStrIndex(lineSpan.Length, sourceCodeRef.FromChar);
                int to = AdjustStrIndex(lineSpan.Length, sourceCodeRef.ToChar);
                return lineSpan.Slice(from, to - from).ToString();
            }

            StringBuilder sb = new();

            for (int i = sourceCodeRef.FromLine; i <= sourceCodeRef.ToLine; i++)
            {
                if (i == sourceCodeRef.FromLine)
                {
                    ReadOnlySpan<char> lineSpan = Lines[i].AsSpan();
                    int from = AdjustStrIndex(lineSpan.Length, sourceCodeRef.FromChar);
                    sb.Append(lineSpan[from..]);
                }
                else if (i == sourceCodeRef.ToLine)
                {
                    ReadOnlySpan<char> lineSpan = Lines[i].AsSpan();
                    int to = AdjustStrIndex(lineSpan.Length, sourceCodeRef.ToChar);
                    int length = Math.Min(to + 1, lineSpan.Length);
                    sb.Append(lineSpan[..length]);
                }
                else
                {
                    sb.Append(Lines[i]);
                }
            }

            return sb.ToString();
        }

        private static int AdjustStrIndex(int length, int loc)
        {
            return Math.Max(Math.Min(length, loc), 0);
        }
    }
}
