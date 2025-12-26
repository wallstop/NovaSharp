namespace WallstopStudios.NovaSharp.Interpreter.Utilities.OneOf
{
    using System;

    /// <summary>
    /// Represents the absence of a value in a discriminated union.
    /// </summary>
    public readonly struct None : IEquatable<None>
    {
        /// <summary>
        /// The default (and only) instance of <see cref="None"/>.
        /// </summary>
#pragma warning disable CA1805 // Do not initialize unnecessarily - explicit default for clarity
        public static readonly None Default = default;
#pragma warning restore CA1805

        /// <summary>
        /// Determines whether this instance equals another <see cref="None"/> instance.
        /// Always returns <c>true</c> since all <see cref="None"/> instances are equal.
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns>Always <c>true</c>.</returns>
        public bool Equals(None other)
        {
            return true;
        }

        /// <summary>
        /// Determines whether this instance equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="None"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is None;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>Always <c>0</c>.</returns>
        public override int GetHashCode()
        {
            return 0;
        }

        /// <summary>
        /// Determines whether two <see cref="None"/> instances are equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>Always <c>true</c>.</returns>
        public static bool operator ==(None left, None right)
        {
            return true;
        }

        /// <summary>
        /// Determines whether two <see cref="None"/> instances are not equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>Always <c>false</c>.</returns>
        public static bool operator !=(None left, None right)
        {
            return false;
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>The string "None".</returns>
        public override string ToString()
        {
            return "None";
        }
    }
}
