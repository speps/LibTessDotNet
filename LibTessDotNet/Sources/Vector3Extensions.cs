using System;
using System.Numerics;
using static System.Math;

namespace LibTessDotNet
{
    public static class Vector3Extensions
    {
        public static int LongAxis(this Vector3 v)
        {
            int i = 0;
            if (Abs(v.Y) > Abs(v.X)) i = 1;
            if (Abs(v.Z) > Abs(i == 0 ? v.X : v.Y)) i = 2;
            return i;
        }

        public static float GetAxis(this Vector3 v, int index)
        {
            if (index == 0) return v.X;
            if (index == 1) return v.Y;
            if (index == 2) return v.Z;
            throw new IndexOutOfRangeException();
        }

        public static void SetAxis(ref Vector3 v, int index, float value)
        {
            if (index == 0) v.X = value;
            else if (index == 1) v.Y = value;
            else if (index == 2) v.Z = value;
            else throw new IndexOutOfRangeException();
        }
    }
}