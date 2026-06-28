namespace WallstopStudios.NovaSharp.Interpreter.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Caches compiled Lua scripts to avoid redundant lexing, parsing, and bytecode emission
    /// for scripts that have already been loaded with the same source text and source name.
    /// Uses LRU (Least Recently Used) eviction policy.
    /// </summary>
    /// <remarks>
    /// Script caching provides significant performance benefits for common patterns:
    /// <list type="bullet">
    /// <item>Hot-reloading development workflows where scripts are loaded repeatedly</item>
    /// <item>Game engines that load the same mod scripts for multiple entities</item>
    /// <item>Template-based script generation where the base script is reused</item>
    /// </list>
    /// The cache is per-Script instance, ensuring isolation between script contexts.
    /// LRU eviction ensures frequently-used scripts remain cached while rarely-used ones are evicted.
    /// </remarks>
    internal sealed class ScriptCompilationCache
    {
        /// <summary>
        /// Default maximum number of entries before eviction begins.
        /// </summary>
        internal const int DefaultMaxEntries = 64;

        private readonly Dictionary<SourceCacheKey, LinkedListNode<LruEntry>> _cache;
        private readonly LinkedList<LruEntry> _lruList;
        private readonly int _maxEntries;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCompilationCache"/> class.
        /// </summary>
        /// <param name="maxEntries">Maximum number of cached entries before eviction (default: 64).</param>
        internal ScriptCompilationCache(int maxEntries = DefaultMaxEntries)
        {
            if (maxEntries < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxEntries),
                    maxEntries,
                    "Maximum cache entries cannot be negative."
                );
            }

            _maxEntries = maxEntries;
            _cache = new Dictionary<SourceCacheKey, LinkedListNode<LruEntry>>(
                Math.Min(maxEntries, 16)
            );
            _lruList = new LinkedList<LruEntry>();
        }

        /// <summary>
        /// Gets the number of entries currently in the cache.
        /// </summary>
        internal int ApproximateCount
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }

        /// <summary>
        /// Attempts to retrieve a cached compilation result for the given script text and source name.
        /// On hit, promotes the entry to the front of the LRU list.
        /// </summary>
        /// <param name="code">The Lua source code text.</param>
        /// <param name="version">The Lua compatibility version used to compile the source.</param>
        /// <param name="sourceName">The explicit source name used for diagnostics, or <c>null</c> for anonymous chunks.</param>
        /// <param name="result">When this method returns true, contains the cached chunk information.</param>
        /// <returns><c>true</c> if a cached entry was found; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGet(
            string code,
            LuaCompatibilityVersion version,
            string sourceName,
            out CachedChunk result
        )
        {
            if (_maxEntries == 0)
            {
                result = default;
                return false;
            }

            SourceCacheKey key = SourceCacheKey.Create(code, version, sourceName);

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out LinkedListNode<LruEntry> node))
                {
                    // Move to front (most recently used)
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    result = node.Value._chunk;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Stores a compiled chunk in the cache for future reuse.
        /// </summary>
        /// <param name="code">The Lua source code text that was compiled.</param>
        /// <param name="version">The Lua compatibility version used to compile the source.</param>
        /// <param name="sourceName">The explicit source name used for diagnostics, or <c>null</c> for anonymous chunks.</param>
        /// <param name="entryPointAddress">The bytecode instruction pointer for the chunk's entry point.</param>
        /// <param name="sourceId">The source ID assigned to this chunk in the script's source list.</param>
        /// <remarks>
        /// If the cache has reached its maximum capacity, the least recently used entry is evicted.
        /// </remarks>
        internal void Store(
            string code,
            LuaCompatibilityVersion version,
            string sourceName,
            int entryPointAddress,
            int sourceId
        )
        {
            if (_maxEntries == 0)
            {
                return;
            }

            SourceCacheKey key = SourceCacheKey.Create(code, version, sourceName);
            CachedChunk chunk = new(entryPointAddress, sourceId);

            lock (_lock)
            {
                // Check if already exists
                if (_cache.TryGetValue(key, out LinkedListNode<LruEntry> existingNode))
                {
                    // Update value and move to front
                    existingNode.Value = new LruEntry(existingNode.Value._key, chunk);
                    _lruList.Remove(existingNode);
                    _lruList.AddFirst(existingNode);
                    return;
                }

                // Evict LRU entry if at capacity
                while (_cache.Count >= _maxEntries && _lruList.Last != null)
                {
                    LinkedListNode<LruEntry> lruNode = _lruList.Last;
                    _cache.Remove(lruNode.Value._key);
                    _lruList.RemoveLast();
                }

                // Add new entry at front (most recently used)
                LruEntry entry = new(key, chunk);
                LinkedListNode<LruEntry> newNode = _lruList.AddFirst(entry);
                _cache[key] = newNode;
            }
        }

        /// <summary>
        /// Clears all cached compilation results.
        /// </summary>
        internal void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _lruList.Clear();
            }
        }

        /// <summary>
        /// Internal entry for the LRU linked list containing both the source text and chunk data.
        /// </summary>
        private struct LruEntry
        {
            internal SourceCacheKey _key;
            internal CachedChunk _chunk;

            internal LruEntry(SourceCacheKey key, CachedChunk chunk)
            {
                _key = key;
                _chunk = chunk;
            }
        }
    }

    /// <summary>
    /// Cache key for compiled source text and source name in a specific Lua compatibility mode.
    /// </summary>
    internal readonly struct SourceCacheKey : IEquatable<SourceCacheKey>
    {
        private readonly string _code;
        private readonly LuaCompatibilityVersion _version;
        private readonly string _sourceName;
        private readonly int _hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceCacheKey"/> struct.
        /// </summary>
        /// <param name="code">The Lua source code text.</param>
        /// <param name="version">The Lua compatibility version used for compilation.</param>
        /// <param name="sourceName">The explicit source name used for diagnostics, or <c>null</c> for anonymous chunks.</param>
        /// <param name="hashCode">The precomputed hash code.</param>
        internal SourceCacheKey(
            string code,
            LuaCompatibilityVersion version,
            string sourceName,
            int hashCode
        )
        {
            _code = code;
            _version = version;
            _sourceName = sourceName;
            _hashCode = hashCode;
        }

        /// <summary>
        /// Creates a cache key for the specified source text, compatibility version, and source name.
        /// </summary>
        /// <param name="code">The Lua source code text.</param>
        /// <param name="version">The Lua compatibility version used for compilation.</param>
        /// <param name="sourceName">The explicit source name used for diagnostics, or <c>null</c> for anonymous chunks.</param>
        /// <returns>A cache key with a precomputed hash code.</returns>
        internal static SourceCacheKey Create(
            string code,
            LuaCompatibilityVersion version,
            string sourceName
        )
        {
            return new SourceCacheKey(
                code,
                version,
                sourceName,
                HashCodeHelper.HashCode(code, version, sourceName)
            );
        }

        /// <summary>
        /// Determines whether this key matches another key by source text, compatibility version, and source name.
        /// </summary>
        /// <param name="other">The key to compare with this key.</param>
        /// <returns><c>true</c> when both keys represent the same source text, compatibility version, and source name.</returns>
        public bool Equals(SourceCacheKey other)
        {
            return _hashCode == other._hashCode
                && _version == other._version
                && string.Equals(_code, other._code, StringComparison.Ordinal)
                && string.Equals(_sourceName, other._sourceName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether this key matches another object.
        /// </summary>
        /// <param name="obj">The object to compare with this key.</param>
        /// <returns><c>true</c> when the object is an equivalent <see cref="SourceCacheKey"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is SourceCacheKey other && Equals(other);
        }

        /// <summary>
        /// Gets the precomputed hash code for dictionary lookup.
        /// </summary>
        /// <returns>The precomputed hash code.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }
    }

    /// <summary>
    /// Represents a cached compilation result containing the bytecode entry point
    /// and associated source information.
    /// </summary>
    internal readonly struct CachedChunk
    {
        /// <summary>
        /// Gets the instruction pointer (IP) for the entry point of this compiled chunk.
        /// </summary>
        internal readonly int _entryPointAddress;

        /// <summary>
        /// Gets the source ID for this chunk in the script's source list.
        /// </summary>
        internal readonly int _sourceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedChunk"/> struct.
        /// </summary>
        /// <param name="entryPointAddress">The bytecode entry point address.</param>
        /// <param name="sourceId">The source ID in the script's source list.</param>
        internal CachedChunk(int entryPointAddress, int sourceId)
        {
            _entryPointAddress = entryPointAddress;
            _sourceId = sourceId;
        }
    }
}
