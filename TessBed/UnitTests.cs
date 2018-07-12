﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using LibTessDotNet;
using System.Reflection;

namespace TessBed
{
    [TestFixture]
    public class UnitTests
    {
        static DataLoader _loader = new DataLoader();

        public struct TestCaseData
        {
            public DataLoader.Asset Asset;
            public WindingRule Winding;
            public int ElementSize;

            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}", Winding, Asset.Name, ElementSize);
            }
        }

        public class TestData
        {
            public int ElementSize;
            public int[] Indices;
        }

        public class TestPool : IPool
        {
            private IDictionary<Type, int> _newCount = new Dictionary<Type, int>();
            private IDictionary<Type, int> _freeCount = new Dictionary<Type, int>();

            public override T Get<T>()
            {
                if (!_newCount.ContainsKey(typeof(T)))
                {
                    _newCount.Add(typeof(T), 0);
                }
                _newCount[typeof(T)]++;
                var obj = new T();
                obj.Init(this);
                return obj;
            }

            public override void Register<T>(ITypePool typePool)
            {
            }

            public override void Return<T>(T obj)
            {
                if (obj == null)
                {
                    return;
                }
                obj.Reset(this);
                if (!_freeCount.ContainsKey(typeof(T)))
                {
                    _freeCount.Add(typeof(T), 0);
                }
                _freeCount[typeof(T)]++;
                if (_freeCount[typeof(T)] > _newCount[typeof(T)])
                {
                    throw new InvalidOperationException();
                }
            }

            public void AssertCounts()
            {
                foreach (var type in _newCount)
                {
                    Assert.AreEqual(type.Value, _freeCount[type.Key], type.Key.ToString());
                }
            }
        }

        public bool OutputTestData = false;
        public static string TestDataPath = Path.Combine("..", "..", "TessBed", "TestData");


        [Test]
        public void Tesselate_WithSingleTriangle_ProducesSameTriangle()
        {
            string data = "0,0,0\n0,1,0\n1,1,0";
            var indices = new List<int>();
            var expectedIndices = new int[] { 0, 1, 2 };
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                var pset = DataLoader.LoadDat(stream);
                var pool = new TestPool();
                var tess = new Tess(pool);

                PolyConvert.ToTess(pset, tess);
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

                indices.Clear();
                for (int i = 0; i < tess.ElementCount; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = tess.Elements[i * 3 + j];
                        indices.Add(index);
                    }
                }

                Assert.AreEqual(expectedIndices, indices.ToArray());
                pool.AssertCounts();
            }
        }

        [Test]
        // From https://github.com/memononen/libtess2/issues/14
        public void Tesselate_WithThinQuad_DoesNotCrash()
        {
            string data = "9.5,7.5,-0.5\n9.5,2,-0.5\n9.5,2,-0.4999999701976776123\n9.5,7.5,-0.4999999701976776123";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                var pset = DataLoader.LoadDat(stream);
                var tess = new Tess();
                PolyConvert.ToTess(pset, tess);
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
            }
        }

        [Test]
        // From https://github.com/speps/LibTessDotNet/issues/1
        public void Tesselate_WithIssue1Quad_ReturnsSameResultAsLibtess2()
        {
            string data = "50,50\n300,50\n300,200\n50,200";
            var indices = new List<int>();
            var expectedIndices = new int[] { 0, 1, 2, 1, 0, 3 };
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                var pset = DataLoader.LoadDat(stream);
                var tess = new Tess();
                PolyConvert.ToTess(pset, tess);
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
                indices.Clear();
                for (int i = 0; i < tess.ElementCount; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = tess.Elements[i * 3 + j];
                        indices.Add(index);
                    }
                }
                Assert.AreEqual(expectedIndices, indices.ToArray());
            }
        }

        [Test]
        // From https://github.com/speps/LibTessDotNet/issues/1
        public void Tesselate_WithNoEmptyPolygonsTrue_RemovesEmptyPolygons()
        {
            string data = "2,0,4\n2,0,2\n4,0,2\n4,0,0\n0,0,0\n0,0,4";
            var indices = new List<int>();
            var expectedIndices = new int[] { 0, 1, 2, 2, 3, 4, 3, 1, 5 };
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                var pset = DataLoader.LoadDat(stream);
                var tess = new Tess();
                PolyConvert.ToTess(pset, tess);
                tess.NoEmptyPolygons = true;
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
                indices.Clear();
                for (int i = 0; i < tess.ElementCount; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = tess.Elements[i * 3 + j];
                        indices.Add(index);
                    }
                }
                Assert.AreEqual(expectedIndices, indices.ToArray());
            }
        }

        [Test]
        public void Tesselate_CalledTwiceOnSameInstance_DoesNotCrash()
        {
            string data = "0,0,0\n0,1,0\n1,1,0";
            var indices = new List<int>();
            var expectedIndices = new int[] { 0, 1, 2 };
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                var pset = DataLoader.LoadDat(stream);
                var tess = new Tess();

                // Call once
                PolyConvert.ToTess(pset, tess);
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

                indices.Clear();
                for (int i = 0; i < tess.ElementCount; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = tess.Elements[i * 3 + j];
                        indices.Add(index);
                    }
                }

                Assert.AreEqual(expectedIndices, indices.ToArray());

                // Call twice
                PolyConvert.ToTess(pset, tess);
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

                indices.Clear();
                for (int i = 0; i < tess.ElementCount; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = tess.Elements[i * 3 + j];
                        indices.Add(index);
                    }
                }

                Assert.AreEqual(expectedIndices, indices.ToArray());
            }
        }

        [Test, TestCaseSource("GetTestCaseData")]
        public void Tessellate_WithAsset_ReturnsExpectedTriangulation(TestCaseData data)
        {
            var pset = data.Asset.Polygons;
            var pool = new TestPool();
            var tess = new Tess(pool);
            PolyConvert.ToTess(pset, tess);
            tess.Tessellate(data.Winding, ElementType.Polygons, data.ElementSize);

            var resourceName = Assembly.GetExecutingAssembly().GetName().Name + ".TestData." + data.Asset.Name + ".testdat";
            var testData = ParseTestData(data.Winding, data.ElementSize, Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName));
            Assert.IsNotNull(testData);
            Assert.AreEqual(testData.ElementSize, data.ElementSize);

            var indices = new List<int>();
            for (int i = 0; i < tess.ElementCount; i++)
            {
                for (int j = 0; j < data.ElementSize; j++)
                {
                    int index = tess.Elements[i * data.ElementSize + j];
                    indices.Add(index);
                }
            }

            Assert.AreEqual(testData.Indices, indices.ToArray());
            pool.AssertCounts();
        }

        public TestData ParseTestData(WindingRule winding, int elementSize, Stream resourceStream)
        {
            var lines = new List<string>();

            bool found = false;
            using (var stream = new StreamReader(resourceStream))
            {
                string line;
                while ((line = stream.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (found && string.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    if (found)
                    {
                        lines.Add(line);
                    }
                    var parts = line.Split(' ');
                    if (parts.FirstOrDefault() == winding.ToString() && Int32.Parse(parts.LastOrDefault()) == elementSize)
                    {
                        found = true;
                    }
                }
            }
            var indices = new List<int>();
            foreach (var line in lines)
            {
                var parts = line.Split(' ');
                if (parts.Length != elementSize)
                {
                    continue;
                }
                foreach (var part in parts)
                {
                    indices.Add(Int32.Parse(part));
                }
            }
            if (found)
            {
                return new TestData()
                {
                    ElementSize = elementSize,
                    Indices = indices.ToArray()
                };
            }
            return null;
        }

        public static void GenerateTestData()
        {
            foreach (var asset in _loader.Assets)
            {
                var pset = asset.Polygons;

                var lines = new List<string>();
                var indices = new List<int>();

                foreach (WindingRule winding in Enum.GetValues(typeof(WindingRule)))
                {
                    var tess = new Tess();
                    PolyConvert.ToTess(pset, tess);
                    tess.Tessellate(winding, ElementType.Polygons, 3);

                    lines.Add(string.Format("{0} {1}", winding, 3));
                    for (int i = 0; i < tess.ElementCount; i++)
                    {
                        indices.Clear();
                        for (int j = 0; j < 3; j++)
                        {
                            int index = tess.Elements[i * 3 + j];
                            indices.Add(index);
                        }
                        lines.Add(string.Join(" ", indices));
                    }
                    lines.Add("");
                }

                File.WriteAllLines(Path.Combine(TestDataPath, asset.Name + ".testdat"), lines);
            }
        }

        public static TestCaseData[] GetTestCaseData()
        {
            var data = new List<TestCaseData>();
            foreach (WindingRule winding in Enum.GetValues(typeof(WindingRule)))
            {
                foreach (var asset in _loader.Assets)
                {
                    data.Add(new TestCaseData { Asset = asset, Winding = winding, ElementSize = 3 });
                }
            }
            return data.ToArray();
        }
    }
}
