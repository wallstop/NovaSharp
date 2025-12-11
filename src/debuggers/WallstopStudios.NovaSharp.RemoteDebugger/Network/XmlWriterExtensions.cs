namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    using System;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// Fluent helpers that simplify emitting debugger XML fragments via <see cref="XmlWriter"/>.
    /// </summary>
    internal static class XmlWriterExtensions
    {
        private sealed class RaiiExecutor : IDisposable
        {
            private readonly Action _action;

            public RaiiExecutor(Action a)
            {
                _action = a;
            }

            /// <summary>
            /// Executes the stored callback when the scope ends, mirroring RAII semantics.
            /// </summary>
            public void Dispose()
            {
                _action();
            }
        }

        /// <summary>
        /// Begins an element and returns a disposable scope that closes it automatically.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="name">Element name.</param>
        /// <returns>An <see cref="IDisposable"/> that closes the element upon disposal.</returns>
        public static IDisposable Element(this XmlWriter xw, string name)
        {
            xw.WriteStartElement(name);
            return new RaiiExecutor(() => xw.WriteEndElement());
        }

        /// <summary>
        /// Writes an attribute whose value is treated as a string.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="name">Attribute name.</param>
        /// <param name="val">Attribute value (converted to "(null)" when <c>null</c>).</param>
        /// <returns>The original writer for fluent chaining.</returns>
        public static XmlWriter Attribute(this XmlWriter xw, string name, string val)
        {
            if (val == null)
            {
                val = "(null)";
            }

            xw.WriteAttributeString(name, val);
            return xw;
        }

        /// <summary>
        /// Writes an attribute whose value is derived from any object.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="name">Attribute name.</param>
        /// <param name="val">Attribute value.</param>
        /// <returns>The original writer.</returns>
        public static XmlWriter Attribute(this XmlWriter xw, string name, object val)
        {
            if (val == null)
            {
                val = "(null)";
            }

            xw.WriteAttributeString(name, val.ToString());
            return xw;
        }

        /// <summary>
        /// Writes an element containing the provided string.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="name">Element name.</param>
        /// <param name="val">Element value.</param>
        /// <returns>The original writer.</returns>
        public static XmlWriter Element(this XmlWriter xw, string name, string val)
        {
            if (val == null)
            {
                val = "(null)";
            }

            xw.WriteElementString(name, val);
            return xw;
        }

        /// <summary>
        /// Writes an element that wraps the value inside a CDATA section.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="name">Element name.</param>
        /// <param name="val">Element value.</param>
        /// <returns>The original writer.</returns>
        public static XmlWriter ElementCData(this XmlWriter xw, string name, string val)
        {
            if (val == null)
            {
                val = "(null)";
            }

            xw.WriteStartElement(name);
            xw.WriteCData(val);
            xw.WriteEndElement();
            return xw;
        }

        /// <summary>
        /// Writes an XML comment if the provided text is non-null.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="text">Comment text.</param>
        /// <returns>The original writer.</returns>
        public static XmlWriter Comment(this XmlWriter xw, object text)
        {
            if (text == null)
            {
                return xw;
            }

            xw.WriteComment(text.ToString());
            return xw;
        }

        /// <summary>
        /// Writes an attribute by formatting a composite string.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="name">Attribute name.</param>
        /// <param name="format">Composite format string.</param>
        /// <param name="args">Arguments applied to <paramref name="format"/>.</param>
        /// <returns>The original writer.</returns>
        public static XmlWriter Attribute(
            this XmlWriter xw,
            string name,
            string format,
            params object[] args
        )
        {
            xw.WriteAttributeString(name, FormatString(format, args));
            return xw;
        }

        /// <summary>
        /// Writes an element by formatting a composite string.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="name">Element name.</param>
        /// <param name="format">Composite format string.</param>
        /// <param name="args">Arguments applied to <paramref name="format"/>.</param>
        /// <returns>The original writer.</returns>
        public static XmlWriter Element(
            this XmlWriter xw,
            string name,
            string format,
            params object[] args
        )
        {
            xw.WriteElementString(name, FormatString(format, args));
            return xw;
        }

        /// <summary>
        /// Writes a CDATA element by formatting a composite string.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="name">Element name.</param>
        /// <param name="format">Composite format string.</param>
        /// <param name="args">Arguments applied to <paramref name="format"/>.</param>
        /// <returns>The original writer.</returns>
        public static XmlWriter ElementCData(
            this XmlWriter xw,
            string name,
            string format,
            params object[] args
        )
        {
            xw.WriteStartElement(name);
            xw.WriteCData(FormatString(format, args));
            xw.WriteEndElement();
            return xw;
        }

        /// <summary>
        /// Writes an XML comment generated from a format string.
        /// </summary>
        /// <param name="xw">Target writer.</param>
        /// <param name="format">Composite format string.</param>
        /// <param name="args">Arguments applied to <paramref name="format"/>.</param>
        /// <returns>The original writer.</returns>
        public static XmlWriter Comment(this XmlWriter xw, string format, params object[] args)
        {
            xw.WriteComment(FormatString(format, args));
            return xw;
        }

        private static string FormatString(string format, object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
