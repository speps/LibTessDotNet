LibTessDotNet [![Build Status](https://ci.appveyor.com/api/projects/status/ypuw4wca67vr5k8u?svg=true)](https://ci.appveyor.com/project/speps/libtessdotnet)
=============

### Goal

Provide a robust and fast tessellator (polygons with N vertices in the output) for .NET, also does triangulation.

### Requirements

* .NET Standard 2.0 (see [here](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) for more information)

### Features

* Tessellate arbitrary complex polygons
    - self-intersecting (see "star-intersect" sample)
    - with coincident vertices (see "clipper" sample)
    - advanced winding rules : even/odd, non zero, positive, negative, |winding| >= 2 (see "redbook-winding" sample)
* Custom input
    - Custom vertex attributes (eg. UV coordinates) with merging callback
    - Force orientation of input contour (clockwise/counterclockwise, eg. for GIS systems, see "force-winding" sample)
* Choice of output
    - polygons with N vertices (with N >= 3)
    - connected polygons (didn't quite tried this yet, but should work)
    - boundary only (to have a basic union of two contours)
* Handles polygons computed with [Clipper](http://www.angusj.com/delphi/clipper.php) - an open source freeware polygon clipping library
* Single/Double precision support

### Screenshot

![Redbook winding example](https://raw.github.com/speps/LibTessDotNet/master/TessBed/Misc/screenshot.png)

### Comparison

![Benchmarks](https://raw.github.com/speps/LibTessDotNet/master/TessBed/Misc/benchmarks.png)

### Build

```
dotnet build
```

### Example

From [TessExample/Program.cs](https://github.com/speps/LibTessDotNet/blob/master/TessExample/Program.cs)

```csharp
using LibTessDotNet;
using System;
using System.Drawing;

namespace TessExample
{
    class Program
    {
        // The data array contains 4 values, it's the associated data of the vertices that resulted in an intersection.
        private static object VertexCombine(LibTessDotNet.Vec3 position, object[] data, float[] weights)
        {
            // Fetch the vertex data.
            var colors = new Color[] { (Color)data[0], (Color)data[1], (Color)data[2], (Color)data[3] };
            // Interpolate with the 4 weights.
            var rgba = new float[] {
                (float)colors[0].R * weights[0] + (float)colors[1].R * weights[1] + (float)colors[2].R * weights[2] + (float)colors[3].R * weights[3],
                (float)colors[0].G * weights[0] + (float)colors[1].G * weights[1] + (float)colors[2].G * weights[2] + (float)colors[3].G * weights[3],
                (float)colors[0].B * weights[0] + (float)colors[1].B * weights[1] + (float)colors[2].B * weights[2] + (float)colors[3].B * weights[3],
                (float)colors[0].A * weights[0] + (float)colors[1].A * weights[1] + (float)colors[2].A * weights[2] + (float)colors[3].A * weights[3]
            };
            // Return interpolated data for the new vertex.
            return Color.FromArgb((int)rgba[3], (int)rgba[0], (int)rgba[1], (int)rgba[2]);
        }

        static void Main(string[] args)
        {
            // Example input data in the form of a star that intersects itself.
            var inputData = new float[] { 0.0f, 3.0f, -1.0f, 0.0f, 1.6f, 1.9f, -1.6f, 1.9f, 1.0f, 0.0f };

            // Create an instance of the tessellator. Can be reused.
            var tess = new LibTessDotNet.Tess();

            // Construct the contour from inputData.
            // A polygon can be composed of multiple contours which are all tessellated at the same time.
            int numPoints = inputData.Length / 2;
            var contour = new LibTessDotNet.ContourVertex[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                // NOTE : Z is here for convenience if you want to keep a 3D vertex position throughout the tessellation process but only X and Y are important.
                contour[i].Position = new LibTessDotNet.Vec3(inputData[i * 2], inputData[i * 2 + 1], 0);
                // Data can contain any per-vertex data, here a constant color.
                contour[i].Data = Color.Azure;
            }
            // Add the contour with a specific orientation, use "Original" if you want to keep the input orientation.
            tess.AddContour(contour, LibTessDotNet.ContourOrientation.Clockwise);

            // Tessellate!
            // The winding rule determines how the different contours are combined together.
            // See http://www.glprogramming.com/red/chapter11.html (section "Winding Numbers and Winding Rules") for more information.
            // If you want triangles as output, you need to use "Polygons" type as output and 3 vertices per polygon.
            tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3, VertexCombine);

            // Same call but the last callback is optional. Data will be null because no interpolated data would have been generated.
            //tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3); // Some vertices will have null Data in this case.

            Console.WriteLine("Output triangles:");
            int numTriangles = tess.ElementCount;
            for (int i = 0; i < numTriangles; i++)
            {
                var v0 = tess.Vertices[tess.Elements[i * 3]].Position;
                var v1 = tess.Vertices[tess.Elements[i * 3 + 1]].Position;
                var v2 = tess.Vertices[tess.Elements[i * 3 + 2]].Position;
                Console.WriteLine("#{0} ({1:F1},{2:F1}) ({3:F1},{4:F1}) ({5:F1},{6:F1})", i, v0.X, v0.Y, v1.X, v1.Y, v2.X, v2.Y);
            }
            Console.ReadLine();
        }
    }
}
```

### Notes

* When using `ElementType.BoundaryContours`, `Tess.Elements` will contain a list of ranges `[startVertexIndex, vertexCount]`.
  Those ranges are to used with `Tess.Vertices`.

### TODO

* No allocations with the same input twice, all coming from pool
* Any suggestions are welcome ;)

### License

SGI FREE SOFTWARE LICENSE B (Version 2.0, Sept. 18, 2008)
More information in LICENSE.txt.

### Links
* [Reference implementation](http://oss.sgi.com/projects/ogl-sample) - the original SGI reference implementation
* [libtess2](https://github.com/memononen/libtess2) - Mikko Mononen cleaned up the original GLU tesselator
* [Poly2Tri](http://code.google.com/p/poly2tri/) - another triangulation library for .NET (other ports also available)
    - Does not support polygons from Clipper, more specifically vertices with same coordinates (coincident)
* [Clipper](http://www.angusj.com/delphi/clipper.php) - an open source freeware polygon clipping library
