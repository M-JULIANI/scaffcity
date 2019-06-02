using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
using QuickGraph;
using QuickGraph.Algorithms;
using Debug = UnityEngine.Debug;


//Basically everything under the SparseGrid namespace was authored by Vicente Soler.
namespace SparseGrid
{
    public class Grid3d
    {
        public Dictionary<Vector3Int, Voxel> Voxels = new Dictionary<Vector3Int, Voxel>();
        public Dictionary<AxisVector3Int, Face> Faces = new Dictionary<AxisVector3Int, Face>();

        public float VoxelSize;

        public static Grid3d MakeGridWithBounds(IEnumerable<Bounds> bounds, float voxelSize)
        {
            var grid = new Grid3d(voxelSize);

            foreach (var bound in bounds)
            {
                var min = grid.IndexFromPoint(bound.min);
                var max = grid.IndexFromPoint(bound.max);

                for (int z = min.z; z <= max.z; z++)
                    for (int y = min.y; y <= max.y; y++)
                        for (int x = min.x; x <= max.x; x++)
                        {
                            var index = new Vector3Int(x, y, z);
                            grid.AddVoxel(index);
                        }
            }

            return grid;
        }

        //This method was customized by me to generate the Sparsegrid from XYZ city bounds
        //while also subtracting plots occupied by buildings
        public static Grid3d MakeWithCity(Bounds bounds, MeshCollider [] voids, float voxelSize)
        {
            var watch = Stopwatch.StartNew();
            var grid = new Grid3d(voxelSize);
            int count = 0;

            var min = grid.IndexFromPoint(bounds.min);
            var max = grid.IndexFromPoint(bounds.max);

            for (int z = min.z; z <= max.z; z++)
                for (int y = min.y; y <= max.y; y++)
                    for (int x = min.x; x <= max.x; x++)
                    {
                        var index = new Vector3Int(x, y, z);
                        var point = new Vector3(index.x * voxelSize, index.y * voxelSize, index.z * voxelSize);

                        if(!grid.inVoids(point, voids))
                        { 
                            grid.AddVoxel(index);
                            count++;
                        }
                        
                    }

            Debug.Log($"Grid took: {watch.ElapsedMilliseconds} ms to create.\r\nGrid size: {count} voxels.");

            return grid;


        }

        public static bool IsInside(Vector3 point, IEnumerable<MeshCollider> obj)
        {
            Physics.queriesHitBackfaces = true;

            var sortedHits = new Dictionary<Collider, int>();
            foreach (var ob in obj)
                sortedHits.Add(ob, 0);

            while (Physics.Raycast(new Ray(point, Vector3.up), out RaycastHit hit))
            {
                var collider = hit.collider;

                if (sortedHits.ContainsKey(collider))
                    sortedHits[collider]++;

                point = hit.point + Vector3.up * 0.00001f;
            }

            bool isInside = sortedHits.Any(kv => kv.Value % 2 != 0);
            return isInside;
        }


        public bool inVoids(Vector3 voxCenter, MeshCollider[] voids)
        {
            bool isInside = IsInside(voxCenter, voids);
            return isInside;
        }

        public Grid3d(float voxelSize = 2.0f)
        {
            VoxelSize = voxelSize;
        }

        public bool AddVoxel(Vector3Int index)
        {
            if (Voxels.TryGetValue(index, out _)) return false;

            var voxel = new Voxel(index, this);
            Voxels.Add(index, voxel);

            int x = index.x;
            int y = index.y;
            int z = index.z;

            var indices =  new[]
                {
                  new AxisVector3Int(Axis.X, x - 1, y, z),
                  new AxisVector3Int(Axis.X, x + 1, y, z),
                  new AxisVector3Int(Axis.Y, x, y - 1, z),
                  new AxisVector3Int(Axis.Y, x, y + 1, z),
                  new AxisVector3Int(Axis.Z, x, y, z - 1),
                  new AxisVector3Int(Axis.Z, x, y, z + 1),
                };

            foreach(var i in indices)
            {
                if (Faces.TryGetValue(i, out _)) continue;

                var face = new Face(i.Index.x, i.Index.y, i.Index.z, i.Axis, this);
                Faces.Add(i, face);
            }

            return true;
        }

        public Vector3Int IndexFromPoint(Vector3 point)
        {
            point /= VoxelSize;
            point -= Vector3.one * 0.5f;
            return new Vector3Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), Mathf.RoundToInt(point.z));
        }

        public IEnumerable<Voxel> GetVoxels()
        {
            return Voxels.Select(v => v.Value);
        }

        public IEnumerable<Face> GetFaces()
        {
            return Faces.Select(v => v.Value);
        }

        public int GetConnectedComponents()
        {
            var graph = new UndirectedGraph<Voxel, Edge<Voxel>>();
            graph.AddVertexRange(GetVoxels().Where(v => v.IsActive));
            graph.AddEdgeRange(GetFaces().Where(f => f.IsActive).Select(f => new Edge<Voxel>(f.Voxels[0], f.Voxels[1])));

            var components = new Dictionary<Voxel, int>();
            var count = graph.ConnectedComponents(components);
            return count;
        }
    }
}