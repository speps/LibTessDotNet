using System;
using System.Diagnostics;

namespace LibTessDotNet
{
    public struct Vec3
    {
        public readonly static Vec3 Zero = new Vec3();

        public float X, Y, Z;

        public Vec3(float x, float y, float z = 0f)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float this[int index]
        {
            get
            {
                if (index == 0) return X;
                if (index == 1) return Y;
                if (index == 2) return Z;
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else if (index == 2) Z = value;
                else throw new IndexOutOfRangeException();
            }
        }

        public static void Sub(ref Vec3 lhs, ref Vec3 rhs, out Vec3 result)
        {
            result.X = lhs.X - rhs.X;
            result.Y = lhs.Y - rhs.Y;
            result.Z = lhs.Z - rhs.Z;
        }

        public static void Neg(ref Vec3 v)
        {
            v.X = -v.X;
            v.Y = -v.Y;
            v.Z = -v.Z;
        }

        public static void Dot(ref Vec3 u, ref Vec3 v, out float dot)
        {
            dot = u.X * v.X + u.Y * v.Y + u.Z * v.Z;
        }
        public static void Normalize(ref Vec3 v)
        {
            float len = v.X * v.X + v.Y * v.Y + v.Z * v.Z;
            Debug.Assert(len >= 0.0f);
            len = 1.0f / (float)Math.Sqrt(len);
            v.X *= len;
            v.Y *= len;
            v.Z *= len;
        }
        public static int LongAxis(ref Vec3 v)
        {
            int i = 0;
            if (Math.Abs(v.Y) > Math.Abs(v.X)) i = 1;
            if (Math.Abs(v.Z) > Math.Abs(i == 0 ? v.X : v.Y)) i = 2;
            return i;
        }
    }
}