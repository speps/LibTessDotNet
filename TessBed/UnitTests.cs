using System;
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
            public string AssetName;
            public WindingRule Winding;
            public int ElementSize;

            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}", Winding, AssetName, ElementSize);
            }
        }

        public class TestData
        {
            public int ElementSize;
            public int[] Indices;
        }

        public bool OutputTestData = false;
        public const string TestDataPath = @"..\..\TessBed\TestData";


        [Test]
        public void Tesselate_WithSingleTriangle_ProducesSameTriangle()
        {
            string data = "0,0,0\n0,1,0\n1,1,0";
            var indices = new List<int>();
            var expectedIndices = new int[] { 0, 1, 2 };
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
            var pset = _loader.GetAsset(data.AssetName).Polygons;
            var tess = new Tess();
            PolyConvert.ToTess(pset, tess);
            tess.Tessellate(data.Winding, ElementType.Polygons, data.ElementSize);

            var resourceName = Assembly.GetExecutingAssembly().GetName().Name + ".TestData." + data.AssetName + ".testdat";
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
            foreach (var name in _loader.AssetNames)
            {
                var pset = _loader.GetAsset(name).Polygons;

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

                File.WriteAllLines(Path.Combine(TestDataPath, name + ".testdat"), lines);
            }
        }

        public static TestCaseData[] GetTestCaseData()
        {
            var data = new List<TestCaseData>();
            foreach (WindingRule winding in Enum.GetValues(typeof(WindingRule)))
            {
                foreach (var name in _loader.AssetNames)
                {
                    data.Add(new TestCaseData { AssetName = name, Winding = winding, ElementSize = 3 });
                }
            }
            return data.ToArray();
        }
    }
}
