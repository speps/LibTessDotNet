using System;
using System.Collections.Generic;

namespace LibTessDotNet
{
    public static class ArrayPool<T>
    {
        private class PoolEntry
        {
            public T[][] Elements = new T[4][];
            public int Count;

            public void Add(T[] array)
            {
                if (Count == Elements.Length)
                {
                    var newElements = new T[Elements.Length * 2][];
                    Array.Copy(Elements, newElements, Elements.Length);
                    Elements = newElements;
                }

                Elements[Count++] = array;
            }
        }

        private static readonly Dictionary<int, PoolEntry> _unused = new Dictionary<int, PoolEntry>();

        public static T[] Create(int length, bool pow)
        {
            PoolEntry entry;

            if (pow)
            {
                var l = 2;
                while (l < length) l <<= 1;
                length = l;
            }

            if (_unused.TryGetValue(length, out entry))
            {
                if (entry.Count > 0)
                {
                    var result = entry.Elements[--entry.Count];
                    entry.Elements[entry.Count] = null;
                    return result;
                }
            }

            return new T[length];
        }

        public static void Resize(ref T[] array, int length, bool pow = true)
        {
            Free(array);
            array = Create(length, pow);
        }

        public static void Free(T[] array)
        {
            PoolEntry entry;

            if (!_unused.TryGetValue(array.Length, out entry))
            {
                entry = new PoolEntry();
                _unused.Add(array.Length, entry);
            }

            entry.Add(array);
        }
    }
}
