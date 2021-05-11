using LibTessDotNet;
using System;
using System.Runtime.InteropServices;

namespace TessBed.Externals
{
    public static class LibTess2
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr NewTessDelegate(IntPtr alloc);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DeleteTessDelegate(IntPtr tess);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void AddContourDelegate(IntPtr tess, int size, [In] float[] pointer, int stride, int count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void TesselateDelegate(IntPtr tess, int windingRule, int elementType, int polySize, int vertexSize, IntPtr normal);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetVertexCountDelegate(IntPtr tess);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetVerticesDelegate(IntPtr tess);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetElementCountDelegate(IntPtr tess);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetElementsDelegate(IntPtr tess);

        private static NewTessDelegate NewTess;
        private static DeleteTessDelegate DeleteTess;
        private static AddContourDelegate AddContour;
        private static TesselateDelegate Tesselate;
        private static GetVertexCountDelegate GetVertexCount;
        private static GetVerticesDelegate GetVertices;
        private static GetElementCountDelegate GetElementCount;
        private static GetElementsDelegate GetElements;

        static LibTess2()
        {
            var dll = LoadLibrary(@"libtess2.dll");
            if (dll != IntPtr.Zero)
            {
                NewTess = Marshal.GetDelegateForFunctionPointer<NewTessDelegate>(GetProcAddress(dll, "tessNewTess"));
                DeleteTess = Marshal.GetDelegateForFunctionPointer<DeleteTessDelegate>(GetProcAddress(dll, "tessDeleteTess"));
                AddContour = Marshal.GetDelegateForFunctionPointer<AddContourDelegate>(GetProcAddress(dll, "tessAddContour"));
                Tesselate = Marshal.GetDelegateForFunctionPointer<TesselateDelegate>(GetProcAddress(dll, "tessTesselate"));
                GetVertexCount = Marshal.GetDelegateForFunctionPointer<GetVertexCountDelegate>(GetProcAddress(dll, "tessGetVertexCount"));
                GetVertices = Marshal.GetDelegateForFunctionPointer<GetVerticesDelegate>(GetProcAddress(dll, "tessGetVertices"));
                GetElementCount = Marshal.GetDelegateForFunctionPointer<GetElementCountDelegate>(GetProcAddress(dll, "tessGetElementCount"));
                GetElements = Marshal.GetDelegateForFunctionPointer<GetElementsDelegate>(GetProcAddress(dll, "tessGetElements"));
            }
        }

        public static PolygonSet Tessellate(PolygonSet pset, int polySize, WindingRule rule)
        {
            if (NewTess == null)
            {
                return null;
            }
            var tess = NewTess(IntPtr.Zero);
            foreach (var poly in pset)
            {
                var contour = new float[poly.Count * 3];
                for (int i = 0; i < poly.Count; i++)
                {
                    contour[i * 3 + 0] = poly[i].X;
                    contour[i * 3 + 1] = poly[i].Y;
                    contour[i * 3 + 2] = poly[i].Z;
                }
                AddContour(tess, 3, contour, Marshal.SizeOf<float>() * 3, poly.Count);
            }
            Tesselate(tess, (int)rule, (int)ElementType.Polygons, polySize, 3, IntPtr.Zero);
            var output = new PolygonSet();
            unsafe
            {
                int elementCount = GetElementCount(tess);
                int* elements = (int*)GetElements(tess).ToPointer();
                float* vertices = (float*)GetVertices(tess).ToPointer();
                for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
                {
                    var poly = new Polygon();
                    for (int i = 0; i < polySize; i++)
                    {
                        int vertexIndex = elements[elementIndex * polySize + i];
                        poly.Add(new PolygonPoint
                        {
                            X = vertices[vertexIndex * 3 + 0],
                            Y = vertices[vertexIndex * 3 + 1],
                            Z = vertices[vertexIndex * 3 + 2],
                        });
                    }
                    output.Add(poly);
                }
            }
            DeleteTess(tess);
            return output;
        }
    }
}
