LibTessDotNet
=============

### Goal

Provide a robust and fast tessellator (polygons with N vertices in the output) for .NET, also does triangulation.

### Requirements

* .NET framework 2.0 (pure CLR, should work with Mono, Unity or XNA)
* C# 3.0 compiler if you want to compile (I am guilty of using 'var')
    - WinForms for the testbed
    - Solution file for Visual Studio 2010

### Features

* Tessellate arbitrary complex polygons
    - self-intersecting (see "star-intersect" sample)
    - with coincident vertices (see "clipper" sample)
    - advanced winding rules : even/odd, non zero, positive, negative, |winding| >= 2 (see "redbook-winding" sample)
* Custom vertex attributes (eg. UV coordinates) with merging callback
* Choice of output
    - polygons with N vertices (with N >= 3)
    - connected polygons (didn't quite tried this yet, but should work)
    - boundary only (to have a basic union of two contours)
* Handles polygons computed with [Clipper](http://www.angusj.com/delphi/clipper.php) - an open source freeware polygon clipping library

### Comparison

TBD

### TODO

* Better performance timing (eg. multiple loops instead of one)
* Profile GC allocations

### License

SGI FREE SOFTWARE LICENSE B (Version 2.0, Sept. 18, 2008)
More information in LICENSE.txt.

### Links
* [Reference implementation](http://oss.sgi.com/projects/ogl-sample) - the original SGI reference implementation
* [libtess2](http://code.google.com/p/libtess2/) - Mikko Mononen cleaned up the original GLU tesselator