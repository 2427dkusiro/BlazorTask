
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BlazorTask.Dispatch;

/// <summary>
/// Represents a string-key dictionary accessable by <see cref="Span{char}"/>
/// </summary>
/// <typeparam name="TValue">Type of value.</typeparam>
internal class SpanStringDictionary<TValue>
{
    // Capacity(2^N) 
    private int capPow = 2;

    // Bit mask to get hash.
    private uint remMask;

    // Acctual capacity,
    private int cap;

    // Element count this container has.
    private int assigned;

    // Table of index accessable by hash.
    private int[] hashes;

    // Array of entry this container has.
    private HashTableEntry[] values;

    /// <summary>
    /// Create a new instance of <see cref="SpanStringDictionary{TValue}"/>.
    /// </summary>
    public SpanStringDictionary()
    {
        cap = 1 << capPow;
        remMask = ~(uint.MaxValue << capPow - 1);
        hashes = new int[cap];
        values = new HashTableEntry[cap];
    }

    /// <summary>
    /// Try to get value from <see cref="ReadOnlySpan{T}"/> key. If value found, returns <see langword="true"/>. Otherwise returns <see langword="false"/>. 
    /// </summary>
    /// <param name="key">Key to find.</param>
    /// <param name="value">Value. If not found, be <see langword="null"/> or default.</param>
    /// <returns></returns>
    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
    {
        var hash = string.GetHashCode(key) & remMask;
        var index = hashes[hash];
        if (index == 0)
        {
            value = default;
            return false;
        }
        while (index != 0)
        {
            ref var entry = ref values[index];
            if (key.SequenceEqual(entry.Key))
            {
                value = entry.Value;
                return true;
            }
            index = entry.Next;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Get value from key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public TValue this[ReadOnlySpan<char> key]
    {
        get
        {
            if (TryGetValue(key, out var result))
            {
                return result;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
    }

    /// <summary>
    /// Add new element to dictionary.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Add(ReadOnlySpan<char> key, TValue value)
    {
        if (assigned == cap)
        {
            Resize();
        }

        var hash = string.GetHashCode(key) & remMask;
        var index = hashes[hash];
        if (index == 0 && values[index].Key is null)
        {
            hashes[hash] = assigned;
            values[assigned++] = new HashTableEntry(new string(key), value);
            return;
        }
        ref HashTableEntry data = ref values[index];
        if (data.Next == -1)
        {
            data.Next = assigned;
            values[assigned++] = new HashTableEntry(new string(key), value);
            return;
        }
        for (var i = 0; i < cap; i++)
        {
            data = ref values[data.Next];
            if (data.Next == -1)
            {
                data.Next = assigned;
                values[assigned++] = new HashTableEntry(new string(key), value);
                return;
            }
        }
        throw new InvalidOperationException("Inifinity roop detected.");
    }

    private void Resize()
    {
        capPow++;
        if (capPow > 30)
        {
            throw new NotSupportedException("size too large.");
        }
        cap = 1 << capPow;
        remMask = ~(uint.MaxValue << capPow - 1);
        Array.Resize(ref hashes, cap);
        Array.Resize(ref values, cap);
    }

    /// <summary>
    /// Represents hash-table entry.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal struct HashTableEntry
    {
        /// <summary>
        /// Key of this entry.
        /// </summary>
        public readonly string Key;

        /// <summary>
        /// Next elements index. -1 if it is last elements.
        /// </summary>
        public int Next;

        /// <summary>
        /// Value of this entry.
        /// </summary>
        public readonly TValue Value;

        /// <summary>
        /// Create a new instance of <see cref="HashTableEntry"/> as a last element.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public HashTableEntry(string key, TValue value)
        {
            Key = key;
            Next = -1;
            Value = value;
        }
    }
}
