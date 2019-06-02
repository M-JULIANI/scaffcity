using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SparseGrid
{
    public class Voxel
    {
        public Vector3Int Index;
        public Vector3 Center;
        public bool IsActive; ////all voxels are active, always
        public bool On { get; set; }
        public bool IsMovable{ get; set; } //determines whether voxel is landlocked or not
        public bool AtWork { get; set; } //at work and not to be disturbed
        public bool Commute { get; set; } //on the road and able to pivot if necessary
        public GameObject DisplayCell { get; set; } // the GO associated with that voxel
        public Job Job { get; set; } //job a voxel is pursuing/ workin on. 'Null' means return to HQ
        public bool Idle { get; set; } // voxel is idle when at HQ
        public bool HQ { get; set; } //helps dettermine what indices correspond to HQ

        public bool IsClimbable => IsActive && Faces.Any(f => f.IsClimbable);

        SparseGrid.Grid3d _grid;

        public Voxel(Vector3Int index, SparseGrid.Grid3d grid)
        {
            _grid = grid;
            Index = index;
            Center = new Vector3(index.x + 0.5f, index.y + 0.5f, index.z + 0.5f) * grid.VoxelSize;
            IsActive = true; //all voxels are active, always
            On = false;
            IsMovable = false; //updated on every coroutine run
            AtWork = false;
            Job = null;
            Idle = true;
            Commute = false;
            HQ = false;
        }

        public void MakeCommute()
        {
            Commute = true;
            AtWork = false;
            Idle = false;
        }

        public void MakeWork()
        {
            Commute = false;
            AtWork = true;
            Idle = false;
        }

        public void MakeIdle()
        {
            Commute = false;
            AtWork = false;
            Idle = true;
        }

        public void SwitchOn()
        {
            //DisplayCell = Instantiate()
            this.On = true;
            this.DisplayCell.gameObject.GetComponent<MeshRenderer>().enabled = true;
        }
        public void SwitchOff()
        {
            this.On = false;
            this.DisplayCell.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }

        public Face[] Faces
        {
            get
            {
                int x = Index.x;
                int y = Index.y;
                int z = Index.z;

                return new[]
                {
                  _grid.Faces[new AxisVector3Int(Axis.X, x - 1, y, z)],
                  _grid.Faces[new AxisVector3Int(Axis.X, x + 1, y, z)],
                  _grid.Faces[new AxisVector3Int(Axis.Y, x, y - 1, z)],
                  _grid.Faces[new AxisVector3Int(Axis.Y, x, y + 1, z)],
                  _grid.Faces[new AxisVector3Int(Axis.Z, x, y, z - 1)],
                  _grid.Faces[new AxisVector3Int(Axis.Z, x, y, z + 1)],
                };
            }
        }

        public IEnumerable<Voxel> GetCornerNeighbours()
        {
            for (int zi = -1; zi <= 1; zi++)
            {
                int z = zi + Index.z;

                for (int yi = -1; yi <= 1; yi++)
                {
                    int y = yi + Index.y;

                    for (int xi = -1; xi <= 1; xi++)
                    {
                        int x = xi + Index.x;

                        var i = new Vector3Int(x, y, z);
                        if (Index == i) continue;

                        if (_grid.Voxels.TryGetValue(i, out var voxel))
                            yield return voxel;
                    }
                }
            }
        }

        //excludes bottom
        //Method for determining if a voxel is landlocked
        public IEnumerable<SparseGrid.Voxel> GetFaceNeighbours()
        {
            int x = Index.x;
            int y = Index.y;
            int z = Index.z;

            var indices = new[]
            {
                new Vector3Int(x - 1, y, z),
                new Vector3Int(x + 1, y, z),
                new Vector3Int(x, y + 1, z),
                new Vector3Int(x, y, z - 1),
                new Vector3Int(x, y, z + 1),
            };

            foreach (var i in indices)
                if (_grid.Voxels.TryGetValue(i, out var voxel))
                    yield return voxel;
        }

        //If a voxel has empty space below, it must fall to the lowest Y location in the city,
        //which is why I need this method to find 'lower neighbor'
        public SparseGrid.Voxel GetBottomNeighbour()
        {
            int x = Index.x;
            int y = Index.y;
            int z = Index.z;

            var bottom = new Vector3Int(x, y - 1, z);

            if (_grid.Voxels.TryGetValue(bottom, out var voxel))
                return voxel;
            else return null;
        }
    }
}