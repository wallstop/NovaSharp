namespace WallstopStudios.NovaSharp.Interpreter.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Caches compiled Lua scripts to avoid redundant lexing, parsing, and bytecode emission
    /// for scripts that have already been loaded with the same source text.
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

        private readonly Dictionary<int, LinkedListNode<LruEntry>> _cache;
        private readonly LinkedList<LruEntry> _lruList;
        private readonly int _maxEntries;
        private readonly LuaCompatibilityVersion _version;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCompilationCache"/> class.
        /// </summary>
        /// <param name="version">The Lua compatibility version used for this script's cache key generation.</param>
        /// <param name="maxEntries">Maximum number of cached entries before eviction (default: 64).</param>
        internal ScriptCompilationCache(
            LuaCompatibilityVersion version,
            int maxEntries = DefaultMaxEntries
        )
        {
            _version = version;
            _maxEntries = maxEntries;
            _cache = new Dictionary<int, LinkedListNode<LruEntry>>(Math.Min(maxEntries, 16));
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
        /// Attempts to retrieve a cached compilation result for the given script text.
        /// On hit, promotes the entry to the front of the LRU list.
        /// </summary>
        /// <param name="code">The Lua source code text.</param>
        /// <param name="result">When this method returns true, contains the cached chunk information.</param>
        /// <returns><c>true</c> if a cached entry was found; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGet(string code, out CachedChunk result)
        {
            int key = ComputeCacheKey(code);

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
        /// <param name="entryPointAddress">The bytecode instruction pointer for the chunk's entry point.</param>
        /// <param name="sourceId">The source ID assigned to this chunk in the script's source list.</param>
        /// <remarks>
        /// If the cache has reached its maximum capacity, the least recently used entry is evicted.
        /// </remarks>
        internal void Store(string code, int entryPointAddress, int sourceId)
        {
            int key = ComputeCacheKey(code);
            CachedChunk chunk = new(entryPointAddress, sourceId);
            LruEntry entry = new(key, chunk);

            lock (_lock)
            {
                // Check if already exists
                if (_cache.TryGetValue(key, out LinkedListNode<LruEntry> existingNode))
                {
                    // Update value and move to front
                    existingNode.Value = entry;
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
        /// Computes a cache key from the script source code and version.
        /// </summary>
        /// <param name="code">The Lua source code.</param>
        /// <returns>A hash code suitable for use as a cache key.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ComputeCacheKey(string code)
        {
            // Use HashCodeHelper for deterministic hashing
            // Include both code hash and version to ensure different versions don't collide
            return HashCodeHelper.HashCode(code, _version);
        }

        /// <summary>
        /// Internal entry for the LRU linked list containing both the cache key and chunk data.
        /// </summary>
        private struct LruEntry
        {
            internal int _key;
            internal CachedChunk _chunk;

            internal LruEntry(int key, CachedChunk chunk)
            {
                _key = key;
                _chunk = chunk;
            }
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
