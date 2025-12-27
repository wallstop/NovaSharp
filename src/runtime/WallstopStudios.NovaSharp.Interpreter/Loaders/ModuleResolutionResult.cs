namespace WallstopStudios.NovaSharp.Interpreter.Loaders
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the result of attempting to resolve a module name to a file path.
    /// Contains both the resolved path (if found) and the list of paths that were searched.
    /// </summary>
    /// <remarks>
    /// This is a readonly struct for minimal allocations when passed by value or stored in locals.
    /// </remarks>
    public readonly struct ModuleResolutionResult : IEquatable<ModuleResolutionResult>
    {
        private static readonly IReadOnlyList<string> EmptyPaths = Array.Empty<string>();

        /// <summary>
        /// Gets the resolved file path if the module was found; otherwise, <c>null</c>.
        /// </summary>
        public string ResolvedPath { get; }

        /// <summary>
        /// Gets the list of file paths that were searched during resolution.
        /// </summary>
        public IReadOnlyList<string> SearchedPaths { get; }

        /// <summary>
        /// Gets a value indicating whether the module was successfully resolved.
        /// </summary>
        public bool IsResolved => ResolvedPath != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleResolutionResult"/> struct.
        /// </summary>
        /// <param name="resolvedPath">The resolved file path, or <c>null</c> if not found.</param>
        /// <param name="searchedPaths">The list of paths that were searched.</param>
        public ModuleResolutionResult(string resolvedPath, IReadOnlyList<string> searchedPaths)
        {
            ResolvedPath = resolvedPath;
            SearchedPaths = searchedPaths ?? EmptyPaths;
        }

        /// <summary>
        /// Creates a successful resolution result.
        /// </summary>
        /// <param name="resolvedPath">The resolved file path.</param>
        /// <param name="searchedPaths">The list of paths that were searched.</param>
        /// <returns>A new <see cref="ModuleResolutionResult"/> indicating success.</returns>
        public static ModuleResolutionResult Success(
            string resolvedPath,
            IReadOnlyList<string> searchedPaths
        )
        {
            return new ModuleResolutionResult(resolvedPath, searchedPaths);
        }

        /// <summary>
        /// Creates a failed resolution result.
        /// </summary>
        /// <param name="searchedPaths">The list of paths that were searched.</param>
        /// <returns>A new <see cref="ModuleResolutionResult"/> indicating failure.</returns>
        public static ModuleResolutionResult NotFound(IReadOnlyList<string> searchedPaths)
        {
            return new ModuleResolutionResult(null, searchedPaths);
        }

        /// <inheritdoc />
        public bool Equals(ModuleResolutionResult other)
        {
            return string.Equals(ResolvedPath, other.ResolvedPath, StringComparison.Ordinal)
                && ReferenceEquals(SearchedPaths, other.SearchedPaths);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ModuleResolutionResult other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return DataStructs.HashCodeHelper.HashCode(ResolvedPath, SearchedPaths);
        }

        /// <summary>
        /// Determines whether two <see cref="ModuleResolutionResult"/> instances are equal.
        /// </summary>
        public static bool operator ==(ModuleResolutionResult left, ModuleResolutionResult right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="ModuleResolutionResult"/> instances are not equal.
        /// </summary>
        public static bool operator !=(ModuleResolutionResult left, ModuleResolutionResult right)
        {
            return !left.Equals(right);
        }
    }
}
