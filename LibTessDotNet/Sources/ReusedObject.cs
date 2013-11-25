using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibTessDotNet
{
    public abstract class ReusedObject<T> where T : ReusedObject<T>, new()
    {
        private int index;

        private static int _unuseIndex;
        private static int _count;
        private static T[] _created = new T[4];

        public static T Create()
        {
            if (_unuseIndex < _count)
            {
                return _created[_unuseIndex++];
            }

            var result = new T();
            
            result.index = _count;

            if (_count == _created.Length)
            {
                var tmp = _created;
                _created = new T[_created.Length * 2];
                Array.Copy(tmp, _created, _count);
            }
            
            _unuseIndex++;

            _created[_count++] = result;

            return result;
        }

        public static void FreeAll()
        {
            _unuseIndex = 0;
        }

        public void Free(bool skipAssert = false)
        {
            if (index < _unuseIndex - 1)
            {
                var tmp = _created[--_unuseIndex];
                
                tmp.index = index;
                _created[index] = tmp;

                index = _unuseIndex;
                _created[_unuseIndex] = (T)this;
            }
            else
            {
                _unuseIndex--;
            }
        }
    }

}
