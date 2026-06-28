namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System.Globalization;
    using Cysharp.Text;

    /// <summary>
    /// A base class for many NovaSharp objects.
    /// Helds a ReferenceID property which gets a different value for every object instance, for debugging
    /// purposes. Note that the ID is not assigned in a thread safe manner for speed reason, so the IDs
    /// are guaranteed to be unique only if everything is running on one thread at a time.
    /// </summary>
    public class RefIdObject
    {
        private static int RefIdCounter;
        private readonly int _refId = ++RefIdCounter;

        /// <summary>
        /// Gets the reference identifier.
        /// </summary>
        /// <value>
        /// The reference identifier.
        /// </value>
        public int ReferenceId
        {
            get { return _refId; }
        }

        /// <summary>
        /// Formats a string with a type name and a ref-id
        /// </summary>
        /// <param name="typeString">The type name.</param>
        /// <returns></returns>
        public string FormatTypeString(string typeString)
        {
            using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
            sb.Append(typeString);
            sb.Append(": 0x");
            sb.Append(_refId.ToString("x", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
