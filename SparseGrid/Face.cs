﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using QuickGraph;

//Basically everything under the SparseGrid namespace was authored by Vicente Soler.
namespace SparseGrid
{
    public class Face
    {
        public Voxel[] Voxels => GetVoxels();
        public Vector3Int Index;
        public Vector3 Center;
        public Axis Direction;
   
        Grid3d _grid;

        public bool IsActive => Voxels.Count(v => v != null && v.IsActive) == 2;

        public BoundaryType Boundary
        {
            get
            {
                bool left = Voxels[0]?.IsActive == true;
                bool right = Voxels[1]?.IsActive == true;

                if (!left && right) return BoundaryType.Left;
                if (left && !right) return BoundaryType.Right;
                if (left && right) return BoundaryType.Inside;
                return BoundaryType.Outside;
            }
        }

        public Vector3 Normal
        {
            get
            {
                int f = (int)Boundary;
                if (Boundary == BoundaryType.Outside) f = 0;

                if (Index.y == 0 && Direction == Axis.Y)
                {
                    f = Boundary == BoundaryType.Outside ? 1 : 0;
                }

                switch (Direction)
                {
                    case Axis.X:
                        return Vector3.right * f;
                    case Axis.Y:
                        return Vector3.up * f;
                    case Axis.Z:
                        return Vector3.forward * f;
                    default:
                        throw new Exception("Wrong direction.");
                }
            }
        }

        public bool IsClimbable
        {
            get
            {
                if (Index.y == 0 && Direction == Axis.Y)
                {
                    return Boundary == BoundaryType.Outside;
                }

                return Boundary == BoundaryType.Left || Boundary == BoundaryType.Right;
            }
        }

        public Face(int x, int y, int z, Axis direction, Grid3d grid)
        {
            _grid = grid;
            Index = new Vector3Int(x, y, z);
            Direction = direction;
            Center = GetCenter();
        }

        Vector3 GetCenter()
        {
            int x = Index.x;
            int y = Index.y;
            int z = Index.z;

            switch (Direction)
            {
                case Axis.X:
                    return new Vector3(x, y + 0.5f, z + 0.5f) * _grid.VoxelSize;
                case Axis.Y:
                    return new Vector3(x + 0.5f, y, z + 0.5f) * _grid.VoxelSize;
                case Axis.Z:
                    return new Vector3(x + 0.5f, y + 0.5f, z) * _grid.VoxelSize;
                default:
                    throw new Exception("Wrong direction.");
            }
        }

        Voxel[] GetVoxels()
        {
            int x = Index.x;
            int y = Index.y;
            int z = Index.z;

            switch (Direction)
            {
                case Axis.X:
                    {
                        return new[]
                        {
                            _grid.Voxels.TryGetValue(new Vector3Int(x - 1, y, z), out var left) ? left : null,
                            _grid.Voxels.TryGetValue(new Vector3Int(x, y, z), out var right) ? right : null,
                        };
                    }
                case Axis.Y:
                    {
                        return new[]
                        {
                            _grid.Voxels.TryGetValue(new Vector3Int(x, y - 1, z), out var left) ? left : null,
                             _grid.Voxels.TryGetValue(new Vector3Int(x, y, z), out var right) ? right : null,
                        };
                    }
                case Axis.Z:
                    {
                        return new[]
                        {
                            _grid.Voxels.TryGetValue(new Vector3Int(x, y , z - 1), out var left) ? left : null,
                             _grid.Voxels.TryGetValue(new Vector3Int(x, y, z), out var right) ? right : null,
                        };
                    }
                default:
                    throw new Exception("Wrong direction.");
            }
        }

    }
}