using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//This class if for all intents and purposes mine. Of course much of the logic has been picked up 
//from Unity tutorials/workshops.
public partial class BuildCity
{
    public class Pixel:MonoBehaviour
    {
        BuildCity mapGrid;
        public Vector3Int Position { get; set; }
        public Vector3 WorldPosition { get; set; }
        public bool HasStreet { get; set; } // helps identify streetfacing versus not
        public int Type { get; set; } //works with the assignment for plot types within the perlin noise plot assignment
        public GameObject DisplayBuilding { get; set; } //GO linked to pixel
        public string Name { get; set; } //helps identify plot in the UI

        //active is street or park
        //inactive is bldg
        public bool IsActive { get; set; }

        public Pixel(BuildCity mapGrid)
        {
            this.mapGrid = mapGrid;
        }



        //selecting pixels that have street frontage
        //pixel.active means you are a street, not a building
        public List<Pixel> getOrthoNeighbors(Pixel[,] mapGrid, int mapWidth, int mapHeight, int meX, int meZ)
        {
            List<Pixel> neighbors = new List<Pixel>();

            var lx = meX == 0 ? 0 : -1;
            var ux = meX == mapWidth - 1 ? 0 : 1;
            var lz = meZ == 0 ? 0 : -1;
            var uz = meZ == mapHeight - 1 ? 0 : 1;

            for (int i = meX + lx; i <= meX + ux; i++)
            {
                for (int j = meZ + lz; j <= meZ + uz; j++)
                {
                    //ensures only ortho neighbors
                    if ((i == meX || j == meZ))
                        if (mapGrid[i, j].IsActive)
                            neighbors.Add(mapGrid[i, j]);                  
                }
            }
            return neighbors;
        }

        //ortho neighbours
        //true if safe building selected, false if building is HQ neighbour
        public bool HQNotNeighbour(Pixel[,] mapGrid, int mapWidth, int mapHeight, int meX, int meZ)
        {
            //viableNeighbours = new List<Pixel>();
            bool excludesHQ = true;

            var lx = meX == 0 ? 0 : -1;
            var ux = meX == mapWidth - 1 ? 0 : 1;
            var lz = meZ == 0 ? 0 : -1;
            var uz = meZ == mapHeight - 1 ? 0 : 1;

            for (int i = meX + lx; i <= meX + ux; i++)
            {
                for (int j = meZ + lz; j <= meZ + uz; j++)
                {
                    //ensures only ortho neighbors
                    if ((i == meX || j == meZ))
                        if (mapGrid[i, j].Type == 11)
                            excludesHQ = false;
                }
            }
            return excludesHQ;
        }

        //selecting pixels that have street frontage
        //pixel.active means you are a street, not a building
        public List<Pixel> getAllNeighbors(Pixel[,] mapGrid, int mapWidth, int mapHeight, int meX, int meZ)
        {
            List<Pixel> neighbors = new List<Pixel>();

            var lx = meX == 0 ? 0 : -1;
            var ux = meX == mapWidth - 1 ? 0 : 1;
            var lz = meZ == 0 ? 0 : -1;
            var uz = meZ == mapHeight - 1 ? 0 : 1;

            for (int i = meX + lx; i <= meX + ux; i++)
            {
                for (int j = meZ + lz; j <= meZ + uz; j++)
                {
                        if (mapGrid[i, j].IsActive)
                            neighbors.Add(mapGrid[i, j]);
                }
            }
            return neighbors;
        }
    }
}
