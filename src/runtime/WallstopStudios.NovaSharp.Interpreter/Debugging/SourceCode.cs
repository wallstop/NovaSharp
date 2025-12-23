namespace WallstopStudios.NovaSharp.Interpreter.Debugging
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

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
        /// Lazily-initialized source code lines (including synthetic header).
        /// Null until first access, then cached.
        /// </summary>
        private volatile string[] _lines;

        /// <summary>
        /// Gets the cached lines (including the synthetic header) for quick snippet extraction.
        /// Lines are computed lazily on first access to reduce compile-time allocations.
        /// </summary>
        public IReadOnlyList<string> Lines
        {
            get
            {
                // Fast path: already initialized
                string[] lines = _lines;
                if (lines != null)
                {
                    return lines;
                }

                // Slow path: double-checked locking for thread-safe lazy init
                // Use Refs as lock object to avoid allocating a separate lock object
                lock (Refs)
                {
                    lines = _lines;
                    if (lines == null)
                    {
                        lines = BuildLines();
                        _lines = lines;
                    }

                    return lines;
                }
            }
        }

        /// <summary>
        /// Gets the script owning this resource.
        /// </summary>
        public Script OwnerScript { get; private set; }

        /// <summary>
        /// Gets the source identifier inside a script
        /// </summary>
        public int SourceId { get; private set; }

        /// <summary>
        /// Gets the list of source references produced while parsing this chunk.
        /// </summary>
        internal List<SourceRef> Refs { get; private set; }

        internal SourceCode(string name, string code, int sourceId, Script ownerScript)
        {
            Refs = new List<SourceRef>();
            Name = name;
            Code = code;
            OwnerScript = ownerScript;
            SourceId = sourceId;
            // Lines are now lazily initialized on first access
        }

        /// <summary>
        /// Builds the lines array from the source code.
        /// Called lazily on first access to <see cref="Lines"/>.
        /// Uses span-based line enumeration to avoid intermediate allocations.
        /// </summary>
        private string[] BuildLines()
        {
            ReadOnlySpan<char> codeSpan = Code.AsSpan();

            // Count lines: start with 1 for synthetic header, plus 1 for initial line
            // Each '\n' adds one more line (the line after the newline)
            int lineCount = 2; // 1 header + 1 code line minimum
            foreach (char c in codeSpan)
            {
                if (c == '\n')
                {
                    lineCount++;
                }
            }

            string[] result = new string[lineCount];

            // Synthetic header line at index 0
            using (Utf16ValueStringBuilder sb = ZStringBuilder.Create())
            {
                sb.Append("-- Begin of chunk : ");
                sb.Append(Name);
                sb.Append(' ');
                result[0] = sb.ToString();
            }

            // Extract lines using span slicing (avoids Split allocation)
            int lineIndex = 1;
            int start = 0;
            for (int i = 0; i < codeSpan.Length; i++)
            {
                if (codeSpan[i] == '\n')
                {
                    result[lineIndex++] = codeSpan.Slice(start, i - start).ToString();
                    start = i + 1;
                }
            }

            // Handle final line (after last newline or if no newlines)
            if (lineIndex < result.Length)
            {
                result[lineIndex] = codeSpan.Slice(start).ToString();
            }

            return result;
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

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

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
