/*
** SGI FREE SOFTWARE LICENSE B (Version 2.0, Sept. 18, 2008) 
** Copyright (C) 2011 Silicon Graphics, Inc.
** All Rights Reserved.
**
** Permission is hereby granted, free of charge, to any person obtaining a copy
** of this software and associated documentation files (the "Software"), to deal
** in the Software without restriction, including without limitation the rights
** to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
** of the Software, and to permit persons to whom the Software is furnished to do so,
** subject to the following conditions:
** 
** The above copyright notice including the dates of first publication and either this
** permission notice or a reference to http://oss.sgi.com/projects/FreeB/ shall be
** included in all copies or substantial portions of the Software. 
**
** THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
** INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
** PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL SILICON GRAPHICS, INC.
** BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
** TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
** OR OTHER DEALINGS IN THE SOFTWARE.
** 
** Except as contained in this notice, the name of Silicon Graphics, Inc. shall not
** be used in advertising or otherwise to promote the sale, use or other dealings in
** this Software without prior written authorization from Silicon Graphics, Inc.
*/
/*
** Original Author: Eric Veach, July 1994.
** libtess2: Mikko Mononen, http://code.google.com/p/libtess2/.
** LibTessDotNet: Remi Gillig, https://github.com/speps/LibTessDotNet
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibTessDotNet
{
    public class VertexPriorityQueue
    {
        private VertexPriorityHeap _heap;
        private MeshUtils.Vertex[] _keys;
        private int[] _order;

        private int _size, _max;
        private bool _initialized;

        public bool Empty { get { return _size == 0 && _heap.Empty; } }

        public VertexPriorityQueue(int initialSize)
        {
            _heap = new VertexPriorityHeap(initialSize);

            _keys = ArrayPool<MeshUtils.Vertex>.Create(initialSize, true);

            _size = 0;
            _max = initialSize;
            _initialized = false;
        }

        public void Free()
        {
            _heap.Free();
            ArrayPool<MeshUtils.Vertex>.Free(_keys);
            ArrayPool<int>.Free(_order);
        }

        struct StackItem
        {
            internal int p, r;
        };

        void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public void Init()
        {
            var stack = ArrayPool<StackItem>.Create(_size + 1, true);
            int stackPos = -1;
            
            int p, r, i, j, piv;
            uint seed = 2016473283;

            p = 0;
            r = _size - 1;
            _order = ArrayPool<int>.Create(_size + 1, true);
            for (piv = 0, i = p; i <= r; ++piv, ++i)
            {
                _order[i] = piv;
            }

            stack[++stackPos] = new StackItem { p = p, r = r };

            while (stackPos >= 0)
            {
                var top = stack[stackPos--];
                p = top.p;
                r = top.r;

                while (r > p + 10)
                {
                    seed = seed * 1539415821 + 1;
                    i = p + (int)(seed % (r - p + 1));
                    piv = _order[i];
                    _order[i] = _order[p];
                    _order[p] = piv;
                    i = p - 1;
                    j = r + 1;
                    do {
                        do { ++i; } while (!Geom.VertLeq(_keys[_order[i]], _keys[piv]));
                        do { --j; } while (!Geom.VertLeq(_keys[piv], _keys[_order[j]]));
                        Swap(ref _order[i], ref _order[j]);
                    } while (i < j);
                    Swap(ref _order[i], ref _order[j]);
                    if (i - p < r - j)
                    {
                        stack[++stackPos] = new StackItem { p = j + 1, r = r };
                        r = i - 1;
                    }
                    else
                    {
                        stack[++stackPos] = new StackItem { p = p, r = i - 1 };
                        p = j + 1;
                    }
                }
                for (i = p + 1; i <= r; ++i)
                {
                    piv = _order[i];
                    for (j = i; j > p && !Geom.VertLeq(_keys[piv], _keys[_order[j - 1]]); --j)
                    {
                        _order[j] = _order[j - 1];
                    }
                    _order[j] = piv;
                }
            }

#if DEBUG
            p = 0;
            r = _size - 1;
            for (i = p; i < r; ++i)
            {
                Debug.Assert(Geom.VertLeq(_keys[_order[i + 1]], _keys[_order[i]]), "Wrong sort");
            }
#endif
            
            ArrayPool<StackItem>.Free(stack);

            _max = _size;
            _initialized = true;
            _heap.Init();
        }

        internal int Insert(MeshUtils.Vertex value)
        {
            if (_initialized)
            {
                return _heap.Insert(value);
            }

            int curr = _size;
            if (++_size >= _max)
            {
                _max <<= 1;
                ArrayPool<MeshUtils.Vertex>.Resize(ref _keys, _max, true);
            }

            _keys[curr] = value;
            return -(curr + 1);
        }

        internal MeshUtils.Vertex ExtractMin()
        {
            Debug.Assert(_initialized);

            if (_size == 0)
            {
                return _heap.ExtractMin();
            }
            MeshUtils.Vertex sortMin = _keys[_order[_size - 1]];
            if (!_heap.Empty)
            {
                MeshUtils.Vertex heapMin = _heap.Minimum();
                if (Geom.VertLeq(heapMin, sortMin))
                    return _heap.ExtractMin();
            }
            do {
                --_size;
            } while (_size > 0 && _keys[_order[_size - 1]] == null);

            return sortMin;
        }

        internal MeshUtils.Vertex Minimum()
        {
            Debug.Assert(_initialized);

            if (_size == 0)
            {
                return _heap.Minimum();
            }
            MeshUtils.Vertex sortMin = _keys[_order[_size - 1]];
            if (!_heap.Empty)
            {
                MeshUtils.Vertex heapMin = _heap.Minimum();
                if (Geom.VertLeq(heapMin, sortMin))
                    return heapMin;
            }
            return sortMin;
        }

        public void Remove(int handle)
        {
            Debug.Assert(_initialized);

            int curr = handle;
            if (curr >= 0)
            {
                _heap.Remove(handle);
                return;
            }
            curr = -(curr + 1);
            Debug.Assert(curr < _max && _keys[curr] != null);

            _keys[curr] = null;
            while (_size > 0 && _keys[_order[_size - 1]] == null)
            {
                --_size;
            }
        }
    }
}
