﻿/*
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

using System.Diagnostics;

namespace LibTessDotNet
{
    public enum WindingRule
    {
        EvenOdd,
        NonZero,
        Positive,
        Negative,
        AbsGeqTwo,
        OddPositive,
        OddNegative,
        EvenPositive,
        EvenNegative
    }

    public enum ElementType
    {
        Polygons,
        ConnectedPolygons,
        BoundaryContours
    }

    public enum ContourOrientation
    {
        Original,
        Clockwise,
        CounterClockwise
    }

    public struct ContourVertex
    {
        public Vec3 Position;
        public object Data;
    }

    public delegate object CombineCallback(Vec3 position, object[] data, float[] weights);

    public partial class Tess
    {
        private Mesh _mesh;
        private Vec3 _normal;
        private Vec3 _sUnit;
        private Vec3 _tUnit;

        private float _bminX, _bminY, _bmaxX, _bmaxY;

        private WindingRule _windingRule;

        private ActiveRegionDict _dict;
        private VertexPriorityQueue _pq;
        private MeshUtils.Vertex _event;

        private CombineCallback _combineCallback;

        private ContourVertex[] _vertices;
        private int _vertexCount;
        private int[] _elements;
        private int _elementCount;

        public Vec3 Normal { get { return _normal; } set { _normal = value; } }

        public float SUnitX = 1.0f;
        public float SUnitY = 1.0f;

        public ContourVertex[] Vertices { get { return _vertices; } }
        public int VertexCount { get { return _vertexCount; } }

        public int[] Elements { get { return _elements; } }
        public int ElementCount { get { return _elementCount; } }

        public Tess()
        {
            _normal = Vec3.Zero;
            _bminX = _bminY = _bmaxX = _bmaxY = 0.0f;

            _windingRule = WindingRule.EvenOdd;
            _mesh = null;

            _vertices = null;
            _vertexCount = 0;
            _elements = null;
            _elementCount = 0;
        }

        private static float[] _minVal = new float[3];
        private static MeshUtils.Vertex[] _minVert = new MeshUtils.Vertex[3];
        private static float[] _maxVal = new float[3];
        private static MeshUtils.Vertex[] _maxVert = new MeshUtils.Vertex[3];

        private void ComputeNormal(ref Vec3 norm)
        {
            var v = _mesh._vHead._next;

            var minVal = _minVal;
            var minVert = _minVert;
            var maxVal = _maxVal;
            var maxVert = _maxVert;

            minVal[0] = maxVal[0] = v._coords.X;
            minVal[1] = maxVal[1] = v._coords.Y;
            minVal[2] = maxVal[2] = v._coords.Z;
            minVert[0] = minVert[1] = minVert[2] = maxVert[0] = maxVert[1] = maxVert[2] = v;

            /*
            var minVal = new float[3] { v._coords.X, v._coords.Y, v._coords.Z };
            var minVert = new MeshUtils.Vertex[3] { v, v, v };
            var maxVal = new float[3] { v._coords.X, v._coords.Y, v._coords.Z };
            var maxVert = new MeshUtils.Vertex[3] { v, v, v };
            */

            var head = _mesh._vHead;
            for (; v != head; v = v._next)
            {
                if (v._coords.X < minVal[0]) { minVal[0] = v._coords.X; minVert[0] = v; }
                if (v._coords.Y < minVal[1]) { minVal[1] = v._coords.Y; minVert[1] = v; }
                if (v._coords.Z < minVal[2]) { minVal[2] = v._coords.Z; minVert[2] = v; }
                if (v._coords.X > maxVal[0]) { maxVal[0] = v._coords.X; maxVert[0] = v; }
                if (v._coords.Y > maxVal[1]) { maxVal[1] = v._coords.Y; maxVert[1] = v; }
                if (v._coords.Z > maxVal[2]) { maxVal[2] = v._coords.Z; maxVert[2] = v; }
            }

            // Find two vertices separated by at least 1/sqrt(3) of the maximum
            // distance between any two vertices
            int i = 0;
            if (maxVal[1] - minVal[1] > maxVal[0] - minVal[0]) { i = 1; }
            if (maxVal[2] - minVal[2] > maxVal[i] - minVal[i]) { i = 2; }
            if (minVal[i] >= maxVal[i])
            {
                // All vertices are the same -- normal doesn't matter
                norm = new Vec3 { X = 0.0f, Y = 0.0f, Z = 1.0f };
                return;
            }

            // Look for a third vertex which forms the triangle with maximum area
            // (Length of normal == twice the triangle area)
            float maxLen2 = 0.0f, tLen2;
            var v1 = minVert[i];
            var v2 = maxVert[i];
            Vec3 d1, d2, tNorm;
            Vec3.Sub(ref v1._coords, ref v2._coords, out d1);
            for (v = head._next; v != head; v = v._next)
            {
                Vec3.Sub(ref v._coords, ref v2._coords, out d2);
                tNorm.X = d1.Y * d2.Z - d1.Z * d2.Y;
                tNorm.Y = d1.Z * d2.X - d1.X * d2.Z;
                tNorm.Z = d1.X * d2.Y - d1.Y * d2.X;
                tLen2 = tNorm.X*tNorm.X + tNorm.Y*tNorm.Y + tNorm.Z*tNorm.Z;
                if (tLen2 > maxLen2)
                {
                    maxLen2 = tLen2;
                    norm = tNorm;
                }
            }

            if (maxLen2 <= 0.0f)
            {
                // All points lie on a single line -- any decent normal will do
                norm = Vec3.Zero;
                i = Vec3.LongAxis(ref d1);
                norm[i] = 1.0f;
            }
        }

        private void CheckOrientation()
        {
            // When we compute the normal automatically, we choose the orientation
            // so that the the sum of the signed areas of all contours is non-negative.
            float area = 0.0f;
            for (var f = _mesh._fHead._next; f != _mesh._fHead; f = f._next)
            {
                var e = f._anEdge;
                if (e._winding <= 0)
                {
                    continue;
                }
                do {
                    area += (e._Org._s - e._Sym._Org._s) * (e._Org._t + e._Sym._Org._t);
                    e = e._Lnext;
                } while (e != f._anEdge);
            }
            if (area < 0.0f)
            {
                // Reverse the orientation by flipping all the t-coordinates
                for (var v = _mesh._vHead._next; v != _mesh._vHead; v = v._next)
                {
                    v._t = -v._t;
                }
                Vec3.Neg(ref _tUnit);
            }
        }

        private void ProjectPolygon()
        {
            var norm = _normal;

            bool computedNormal = false;
            if (norm.X == 0.0f && norm.Y == 0.0f && norm.Z == 0.0f)
            {
                ComputeNormal(ref norm);
                computedNormal = true;
            }

            int i = Vec3.LongAxis(ref norm);

            _sUnit[i] = 0.0f;
            _sUnit[(i + 1) % 3] = SUnitX;
            _sUnit[(i + 2) % 3] = SUnitY;

            _tUnit[i] = 0.0f;
            _tUnit[(i + 1) % 3] = norm[i] > 0.0f ? -SUnitY : SUnitY;
            _tUnit[(i + 2) % 3] = norm[i] > 0.0f ? SUnitX : -SUnitX;

            // Project the vertices onto the sweep plane
            var head = _mesh._vHead;
            for (var v = head._next; v != head; v = v._next)
            {
                Vec3.Dot(ref v._coords, ref _sUnit, out v._s);
                Vec3.Dot(ref v._coords, ref _tUnit, out v._t);
            }
            if (computedNormal)
            {
                CheckOrientation();
            }

            // Compute ST bounds.
            bool first = true;
            for (var v = head._next; v != head; v = v._next)
            {
                if (first)
                {
                    _bminX = _bmaxX = v._s;
                    _bminY = _bmaxY = v._t;
                    first = false;
                }
                else
                {
                    if (v._s < _bminX) _bminX = v._s;
                    else if (v._s > _bmaxX) _bmaxX = v._s;
                    if (v._t < _bminY) _bminY = v._t;
                    else if (v._t > _bmaxY) _bmaxY = v._t;
                }
            }
        }

        /// <summary>
        /// TessellateMonoRegion( face ) tessellates a monotone region
        /// (what else would it do??)  The region must consist of a single
        /// loop of half-edges (see mesh.h) oriented CCW.  "Monotone" in this
        /// case means that any vertical line intersects the interior of the
        /// region in a single interval.  
        /// 
        /// Tessellation consists of adding interior edges (actually pairs of
        /// half-edges), to split the region into non-overlapping triangles.
        /// 
        /// The basic idea is explained in Preparata and Shamos (which I don't
        /// have handy right now), although their implementation is more
        /// complicated than this one.  The are two edge chains, an upper chain
        /// and a lower chain.  We process all vertices from both chains in order,
        /// from right to left.
        /// 
        /// The algorithm ensures that the following invariant holds after each
        /// vertex is processed: the untessellated region consists of two
        /// chains, where one chain (say the upper) is a single edge, and
        /// the other chain is concave.  The left vertex of the single edge
        /// is always to the left of all vertices in the concave chain.
        /// 
        /// Each step consists of adding the rightmost unprocessed vertex to one
        /// of the two chains, and forming a fan of triangles from the rightmost
        /// of two chain endpoints.  Determining whether we can add each triangle
        /// to the fan is a simple orientation test.  By making the fan as large
        /// as possible, we restore the invariant (check it yourself).
        /// </summary>
        private void TessellateMonoRegion(MeshUtils.Face face)
        {
            // All edges are oriented CCW around the boundary of the region.
            // First, find the half-edge whose origin vertex is rightmost.
            // Since the sweep goes from left to right, face->anEdge should
            // be close to the edge we want.
            var up = face._anEdge;
            Debug.Assert(up._Lnext != up && up._Lnext._Lnext != up);

            for (; Geom.VertLeq(up._Sym._Org, up._Org); up = up._Lprev);
            for (; Geom.VertLeq(up._Org, up._Sym._Org); up = up._Lnext);

            var lo = up._Lprev;

            while (up._Lnext != lo)
            {
                if (Geom.VertLeq(up._Sym._Org, lo._Org))
                {
                    // up.Dst is on the left. It is safe to form triangles from lo.Org.
                    // The EdgeGoesLeft test guarantees progress even when some triangles
                    // are CW, given that the upper and lower chains are truly monotone.
                    while (lo._Lnext != up && (Geom.EdgeGoesLeft(lo._Lnext)
                        || Geom.EdgeSign(lo._Org, lo._Sym._Org, lo._Lnext._Sym._Org) <= 0.0f))
                    {
                        lo = _mesh.Connect(lo._Lnext, lo)._Sym;
                    }
                    lo = lo._Lprev;
                }
                else
                {
                    // lo.Org is on the left.  We can make CCW triangles from up.Dst.
                    while (lo._Lnext != up && (Geom.EdgeGoesRight(up._Lprev)
                        || Geom.EdgeSign(up._Sym._Org, up._Org, up._Lprev._Org) >= 0.0f))
                    {
                        up = _mesh.Connect(up, up._Lprev)._Sym;
                    }
                    up = up._Lnext;
                }
            }

            // Now lo.Org == up.Dst == the leftmost vertex.  The remaining region
            // can be tessellated in a fan from this leftmost vertex.
            Debug.Assert(lo._Lnext != up);
            while (lo._Lnext._Lnext != up)
            {
                lo = _mesh.Connect(lo._Lnext, lo)._Sym;
            }
        }

        /// <summary>
        /// TessellateInterior( mesh ) tessellates each region of
        /// the mesh which is marked "inside" the polygon. Each such region
        /// must be monotone.
        /// </summary>
        private void TessellateInterior()
        {
            MeshUtils.Face f, next;
            for (f = _mesh._fHead._next; f != _mesh._fHead; f = next)
            {
                // Make sure we don't try to tessellate the new triangles.
                next = f._next;
                if (f._inside)
                {
                    TessellateMonoRegion(f);
                }
            }
        }

        /// <summary>
        /// DiscardExterior zaps (ie. sets to null) all faces
        /// which are not marked "inside" the polygon.  Since further mesh operations
        /// on NULL faces are not allowed, the main purpose is to clean up the
        /// mesh so that exterior loops are not represented in the data structure.
        /// </summary>
        private void DiscardExterior()
        {
            MeshUtils.Face f, next;

            for (f = _mesh._fHead._next; f != _mesh._fHead; f = next)
            {
                // Since f will be destroyed, save its next pointer.
                next = f._next;
                if( ! f._inside ) {
                    _mesh.ZapFace(f);
                }
            }
        }

        /// <summary>
        /// SetWindingNumber( value, keepOnlyBoundary ) resets the
        /// winding numbers on all edges so that regions marked "inside" the
        /// polygon have a winding number of "value", and regions outside
        /// have a winding number of 0.
        /// 
        /// If keepOnlyBoundary is TRUE, it also deletes all edges which do not
        /// separate an interior region from an exterior one.
        /// </summary>
        private void SetWindingNumber(int value, bool keepOnlyBoundary)
        {
            MeshUtils.Edge e, eNext;

            for (e = _mesh._eHead._next; e != _mesh._eHead; e = eNext)
            {
                eNext = e._next;
                if (e._Rface._inside != e._Lface._inside)
                {

                    /* This is a boundary edge (one side is interior, one is exterior). */
                    e._winding = (e._Lface._inside) ? value : -value;
                }
                else
                {

                    /* Both regions are interior, or both are exterior. */
                    if (!keepOnlyBoundary)
                    {
                        e._winding = 0;
                    }
                    else
                    {
                        _mesh.Delete(e);
                    }
                }
            }

        }

        private int GetNeighbourFace(MeshUtils.Edge edge)
        {
            if (edge._Rface == null)
                return MeshUtils.Undef;
            if (!edge._Rface._inside)
                return MeshUtils.Undef;
            return edge._Rface._n;
        }

        private void OutputPolymesh(ElementType elementType, int polySize)
        {
            MeshUtils.Vertex v;
            MeshUtils.Face f;
            MeshUtils.Edge edge;
            int maxFaceCount = 0;
            int maxVertexCount = 0;
            int faceVerts, i;

            if (polySize < 3)
            {
                polySize = 3;
            }
            // Assume that the input data is triangles now.
            // Try to merge as many polygons as possible
            if (polySize > 3)
            {
                _mesh.MergeConvexFaces(polySize);
            }

            // Mark unused
            for (v = _mesh._vHead._next; v != _mesh._vHead; v = v._next)
                v._n = MeshUtils.Undef;

            // Create unique IDs for all vertices and faces.
            for (f = _mesh._fHead._next; f != _mesh._fHead; f = f._next)
            {
                f._n = MeshUtils.Undef;
                if (!f._inside) continue;

                edge = f._anEdge;
                faceVerts = 0;
                do {
                    v = edge._Org;
                    if (v._n == MeshUtils.Undef)
                    {
                        v._n = maxVertexCount;
                        maxVertexCount++;
                    }
                    faceVerts++;
                    edge = edge._Lnext;
                }
                while (edge != f._anEdge);

                Debug.Assert(faceVerts <= polySize);

                f._n = maxFaceCount;
                ++maxFaceCount;
            }

            _elementCount = maxFaceCount;
            if (elementType == ElementType.ConnectedPolygons)
                maxFaceCount *= 2;
            _elements = ArrayPool<int>.Create(maxFaceCount * polySize, true); //new int[maxFaceCount * polySize];

            _vertexCount = maxVertexCount;
            _vertices = ArrayPool<ContourVertex>.Create(_vertexCount, true); //new ContourVertex[_vertexCount];
            
            var elems = _elements;
            var verts = _vertices;

            // Output vertices.
            for (v = _mesh._vHead._next; v != _mesh._vHead; v = v._next)
            {
                if (v._n != MeshUtils.Undef)
                {
                    // Store coordinate
                    int n = v._n;
                    verts[n].Position = v._coords;
                    verts[n].Data = v._data;
                }
            }

            // Output indices.
            int elementIndex = 0;
            for (f = _mesh._fHead._next; f != _mesh._fHead; f = f._next)
            {
                if (!f._inside) continue;

                // Store polygon
                edge = f._anEdge;
                faceVerts = 0;
                do {
                    v = edge._Org;
                    elems[elementIndex++] = v._n;
                    faceVerts++;
                    edge = edge._Lnext;
                } while (edge != f._anEdge);
                // Fill unused.
                for (i = faceVerts; i < polySize; ++i)
                {
                    elems[elementIndex++] = MeshUtils.Undef;
                }

                // Store polygon connectivity
                if (elementType == ElementType.ConnectedPolygons)
                {
                    edge = f._anEdge;
                    do
                    {
                        elems[elementIndex++] = GetNeighbourFace(edge);
                        edge = edge._Lnext;
                    } while (edge != f._anEdge);
                    // Fill unused.
                    for (i = faceVerts; i < polySize; ++i)
                    {
                        elems[elementIndex++] = MeshUtils.Undef;
                    }
                }
            }
        }

        private void OutputContours()
        {
            MeshUtils.Face f;
            MeshUtils.Edge edge, start;
            int startVert = 0;
            int vertCount = 0;

            _vertexCount = 0;
            _elementCount = 0;

            for (f = _mesh._fHead._next; f != _mesh._fHead; f = f._next)
            {
                if (!f._inside) continue;

                start = edge = f._anEdge;
                do
                {
                    ++_vertexCount;
                    edge = edge._Lnext;
                }
                while (edge != start);

                ++_elementCount;
            }

            _elements = ArrayPool<int>.Create(_elementCount * 2, true); //new int[_elementCount * 2];
            _vertices = ArrayPool<ContourVertex>.Create(_vertexCount, true); //new ContourVertex[_vertexCount];

            var elems = _elements;
            var verts = _vertices;

            int vertIndex = 0;
            int elementIndex = 0;

            startVert = 0;

            for (f = _mesh._fHead._next; f != _mesh._fHead; f = f._next)
            {
                if (!f._inside) continue;

                vertCount = 0;
                start = edge = f._anEdge;
                do {
                    verts[vertIndex++].Position = edge._Org._coords;
                    verts[vertIndex++].Data = edge._Org._data;
                    ++vertCount;
                    edge = edge._Lnext;
                } while (edge != start);

                elems[elementIndex++] = startVert;
                elems[elementIndex++] = vertCount;

                startVert += vertCount;
            }
        }

        private float SignedArea(ContourVertex[] vertices, int count)
        {
            float area = 0.0f;

            for (int i = 0; i < count; i++)
            {
                var v0 = vertices[i];
                var v1 = vertices[(i + 1) % vertices.Length];

                area += v0.Position.X * v1.Position.Y;
                area -= v0.Position.Y * v1.Position.X;
            }

            return area * 0.5f;
        }

        public void AddContour(ContourVertex[] vertices)
        {
            AddContour(vertices, vertices.Length, ContourOrientation.Original);
        }

        public void AddContour(ContourVertex[] vertices, int count)
        {
            AddContour(vertices, count, ContourOrientation.Original);
        }

        public void AddContour(ContourVertex[] vertices, ContourOrientation forceOrientation)
        {
            AddContour(vertices, vertices.Length, ContourOrientation.Original);
        }

        public void AddContour(ContourVertex[] vertices, int count, ContourOrientation forceOrientation)
        {
            if (_mesh == null)
            {
                _mesh = new Mesh();
            }

            bool reverse = false;
            if (forceOrientation != ContourOrientation.Original)
            {
                float area = SignedArea(vertices, count);
                reverse = (forceOrientation == ContourOrientation.Clockwise && area < 0.0f) || (forceOrientation == ContourOrientation.CounterClockwise && area > 0.0f);
            }

            MeshUtils.Edge e = null;
            for (int i = 0; i < count; ++i)
            {
                if (e == null)
                {
                    e = _mesh.MakeEdge();
                    _mesh.Splice(e, e._Sym);
                }
                else
                {
                    // Create a new vertex and edge which immediately follow e
                    // in the ordering around the left face.
                    _mesh.SplitEdge(e);
                    e = e._Lnext;
                }

                int index = reverse ? count - 1 - i : i;
                // The new vertex is now e._Org.
                e._Org._coords = vertices[index].Position;
                e._Org._data = vertices[index].Data;

                // The winding of an edge says how the winding number changes as we
                // cross from the edge's right face to its left face.  We add the
                // vertices in such an order that a CCW contour will add +1 to
                // the winding number of the region inside the contour.
                e._winding = 1;
                e._Sym._winding = -1;
            }
        }

        public void Tessellate(WindingRule windingRule, ElementType elementType, int polySize)
        {
            Tessellate(windingRule, elementType, polySize, null);
        }

        public void Tessellate(WindingRule windingRule, ElementType elementType, int polySize, CombineCallback combineCallback)
        {
            if (_vertices != null)
            {
                ArrayPool<ContourVertex>.Free(_vertices);
                ArrayPool<int>.Free(_elements);

                _vertices = null;
                _elements = null;
            }

            _windingRule = windingRule;
            _combineCallback = combineCallback;

            if (_mesh == null)
            {
                return;
            }

            // Determine the polygon normal and project vertices onto the plane
            // of the polygon.
            ProjectPolygon();

            // ComputeInterior computes the planar arrangement specified
            // by the given contours, and further subdivides this arrangement
            // into regions.  Each region is marked "inside" if it belongs
            // to the polygon, according to the rule given by windingRule.
            // Each interior region is guaranteed be monotone.
            ComputeInterior();

            // If the user wants only the boundary contours, we throw away all edges
            // except those which separate the interior from the exterior.
            // Otherwise we tessellate all the regions marked "inside".
            if (elementType == ElementType.BoundaryContours)
            {
                SetWindingNumber(1, true);
            }
            else
            {
                TessellateInterior();
            }

            _mesh.Check();

            if (elementType == ElementType.BoundaryContours)
            {
                OutputContours();
            }
            else
            {
                OutputPolymesh(elementType, polySize);
            }

            MeshUtils.Edge.FreeAll();
            MeshUtils.Vertex.FreeAll();
            ActiveRegion.FreeAll();
            MeshUtils.Face.FreeAll();
            ActiveRegionDict.Node.FreeAll();

            _mesh = null;
        }
    }
}
