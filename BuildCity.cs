using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;


//This file contains probably the most important classes for both logic and visualization.
//BuildCity
//Job
//Army
//BuilCity is the sort of umbrella class in which multiple instances of 'Job' exist as well 
//as one instance of 'Army'.

//The city grid logic is taken from a procedural city tutorial series: https://www.youtube.com/watch?v=sLtelfckEjc
//The street assets themselves are downlaoded from the Unity Asset Store, the package is called 'Simple Modular Street Kit'


public partial class BuildCity : MonoBehaviour
{

    [SerializeField]
    GUISkin _skin;

    //the three masterlists of jobs
    public List<Job> potentialJobs; //those that your could pursue, which time out after a while if not clicked on
    public List<Job> currentJobs; // those you have clicked on and your army is pursuing
    public List<Job> activeJobs; //job that are under construction
    int proxyJobCount;

    ProceduralBuilding pBuilding;
    GameObject xStreets;
    GameObject zStreets;
    GameObject crossroad;
    GameObject _grass;
    GameObject _hq;
    public MeshCollider globalHQ;
    public Bounds cityBounds;
 
    Material buildingMat;
    GameObject mainController;

    public int mapWidth;
    public int mapHeight;
    public Pixel[,] mapPixels;
    public int buildingFootPrint;
    public float multiplierSpacing;
    float seed;
    public int connected;
    public float voxelSize = 2.0f;
    public List<MeshCollider> voids;
    public List<MeshCollider> voidsOther;
    public List<Vector3Int> totalJobIndices;
    public int allJobCount;

    UndirectedGraph<SparseGrid.Voxel, TaggedEdge<SparseGrid.Voxel, SparseGrid.Face>> graph = null;

    public float _revenue;


    public Army army;
    public int fleetCountIdle;
    public int fleetCountActive;

    public BuildCity(int cityWidth, int cityLength)
    {
        cityBounds = new Bounds();
        mapWidth = cityWidth;
        mapHeight = cityLength;
        mapPixels = new Pixel[mapWidth, mapHeight];
        buildingFootPrint = 12;
        multiplierSpacing = 1.0f;
        xStreets = Resources.Load("StreetX", typeof(GameObject)) as GameObject;
        zStreets = Resources.Load("StreetZ", typeof(GameObject)) as GameObject;
        crossroad = Resources.Load("CrossX", typeof(GameObject)) as GameObject;
        _grass = Resources.Load("Grass", typeof(GameObject)) as GameObject;
        _hq = Resources.Load("Capsule", typeof(GameObject)) as GameObject;
        buildingMat = Resources.Load<Material>("Materials/_tiledBuilding");
        totalJobIndices = new List<Vector3Int>();
        proxyJobCount = 0;
        _revenue = 0.0f;
        allJobCount = 0;
        fleetCountActive = 0;
        fleetCountIdle = 0;

    }

    public void Awake()
    {
        mainController = GameObject.Find("MainController");
    }
    // Start is called before the first frame update
    public void Start()
    {
        seed = 42; // I trust this number will yield the best possible version of my city
        voids = new List<MeshCollider>();
        voidsOther = new List<MeshCollider>();
        potentialJobs = new List<Job>();
        currentJobs = new List<Job>();
        activeJobs = new List<Job>();
        pBuilding = new ProceduralBuilding(buildingFootPrint, multiplierSpacing, this);
        CityGen();

        army = new Army(this);
     

        //initiating one potential job between the 'WakeCity' coroutine
        var j1 = new Job(this);
        potentialJobs.Add(j1);
        totalJobIndices.AddRange(j1.indices);

    }

    public IEnumerator WakeCity()
    {
        for (int i = 0; i < 120; i++)
        {
            bool canCreate = true;
            float delay = (float)Random.Range(5, 15);
            if (allJobCount < 6)
            {
                var a = new Job(this);

                foreach (var j in potentialJobs)
                    foreach (var c in currentJobs)
                        if (a.name == j.name || a.name == c.name)
                        {
                            canCreate = false;
                            continue;
                        }

                if (canCreate == true)
                { 
                    var _streets = a.streets;
                    var _target = a.targetBuilding;
                    var area = a.size;

                    potentialJobs.Add(a);
                    totalJobIndices.AddRange(a.indices);
                }
            }
            yield return new WaitForSeconds(delay);
        }
    }

    //This method is used to identify 'pixels' which require street light payments.
    public List<BuildCity.Pixel> GetIntersectionPixels()
    {
        List<BuildCity.Pixel> output = new List<BuildCity.Pixel>();
        
        for (int i = 0; i < mapHeight; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                var pix = mapPixels[i, j];
                if (pix.Type == -3 || pix.Type == -2 || pix.Type == -1)
                    output.Add(pix);
            }
        }
        return output;
    }

    public int getFleetSize()
    {
        var armyList = army.grid.GetVoxels().Where(v => v.On).ToList();
        return armyList.Count;
    }

    public void UpdateFleetStatus()
    {
        fleetCountIdle = army.grid.GetVoxels().Where(v => v.On)
            .Where(v => v.Idle == true).Count();

        fleetCountActive = army.grid.GetVoxels().Where(v => v.On)
            .Where(v => v.Idle == false).Count();
    }

    public int getFootPrint()
    {
        return buildingFootPrint;
    }

    public int getCityWidth()
    {
        return mapWidth;
    }

    public List<Job> UpdateAllJobs()
    {
        var allJobs = new List<Job>();
        allJobs.AddRange(potentialJobs);
        allJobs.AddRange(currentJobs);
        return allJobs;
    }

    public void JobCount()
    {
        var count = potentialJobs.Count + currentJobs.Count;
        allJobCount = count;
    }

    public List<GameObject> PickStreets(Pixel building, int index)
    {
        var streets = new List<GameObject>();
        List<Pixel> closestStreets = FindClosestStreetPixels(building, index);

        for (int i = 0; i < closestStreets.Count; i++)
        {
            if (closestStreets[i].IsActive)
            {
                var parent = closestStreets[i].DisplayBuilding;
                var child = parent.transform.GetChild(0).gameObject;
                streets.Add(child);
            }
        }
        return streets;
    }

    public List<Pixel> neighborBlocks(BuildCity.Pixel active)
    {
        List<Pixel> outputBlocks = new List<Pixel>();
        var neighbors = active.getAllNeighbors(mapPixels, mapWidth, mapHeight, active.Position.x, active.Position.z);

        foreach (var n in neighbors)
            if (!n.IsActive)
                outputBlocks.Add(n);

        return outputBlocks;
    }

    public GameObject PickBuilding(out BuildCity.Pixel plot, out int index)
    {
        Pixel buildingNew = new Pixel(this);
        GameObject GO = null;
        index = new int();

        bool selected = false;
        while (selected == false)
        {
            int r1 = Random.Range(0, mapHeight);
            int r2 = Random.Range(0, mapWidth);

            if (mapPixels[r1, r2].IsActive == false && mapPixels[r1, r2].HasStreet)
            {
                if (mapPixels[r1, r2].HQNotNeighbour(mapPixels, mapWidth, mapHeight, r1, r2))
                    {
                    var proxy = mapPixels[r1, r2].DisplayBuilding;

                    var proxyCount = proxy.transform.childCount;
                    if (proxyCount >= 2)
                    {
                        selected = true;
                        buildingNew = mapPixels[r1, r2];
                    }
                }
            }
        }
        plot = buildingNew;


        var closestStreet = FindClosestStreetPixel(buildingNew);

        var selectedGo = buildingNew.DisplayBuilding;
        var goPos = buildingNew.WorldPosition;
        var childCount = selectedGo.transform.childCount;

        var tempGO = new List<GOScore>();

        //gameobject within pixel
        if (childCount > 3)
        {
            for (int i = 0; i < childCount; i++)
            {
                if (i != 0)
                {
                    var bldg = selectedGo.transform.GetChild(i).gameObject;
                    var _index = bldg.transform.GetSiblingIndex();
                    var modPos = bldg.transform.localPosition + goPos;

                    var score = Vector3.Distance(modPos, closestStreet.WorldPosition);
                    tempGO.Add(new GOScore(bldg, score, _index));
                }
            }

            var sortedTemp = tempGO.OrderBy(s => s.score).ToList();
            var selTemp = sortedTemp[0].GO;
            GO = selTemp;
            index = sortedTemp[0].index;


        }

        else if (childCount == 3)
        {
            for (int i = 0; i < childCount; i++)
            {
                if (i != 0)
                {
                    var bldg = selectedGo.transform.GetChild(i).gameObject;
                    var _index = bldg.transform.GetSiblingIndex();
                    var modPos = bldg.transform.localPosition + goPos;
                    var score = Vector3.Distance(modPos, closestStreet.WorldPosition);
                    tempGO.Add(new GOScore(bldg, score, _index));
                }
            }

            var sortedTemp = tempGO.OrderBy(s => s.score).ToList();
            var selTemp = sortedTemp[0].GO;
            GO = selTemp;
            index = sortedTemp[0].index;
        }

        return GO;
    }

    public Pixel FindClosestStreetPixel(Pixel activeBuilding)
    {
        Pixel outPix;
        List<PixelScore> tempPixels = new List<PixelScore>();

        Vector3Int activePos = activeBuilding.Position;

        for (int i = 0; i < mapHeight; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                var pix = mapPixels[i, j];

                //if is street
                if (pix.IsActive)
                {
                    var dist = Vector3.Distance(activePos, pix.Position);
                    var scored = new PixelScore(pix, dist);
                    tempPixels.Add(scored);
                }
            }
        }

        var sortedPix = tempPixels.OrderBy(d => d.getScore).ToList();

        outPix = sortedPix[0].getPixel;
        return outPix;
    }

    public List<Pixel> FindClosestStreetPixels(Pixel activeBuilding, int index)
    {
        List<Pixel> outPix = new List<Pixel>();
        List<PixelScore> tempPixels = new List<PixelScore>();

        var bldgChild = activeBuilding.DisplayBuilding.transform.GetChild(index);
        var localPos = bldgChild.transform.localPosition;

        var combinedPos = localPos + activeBuilding.WorldPosition;

        Vector3 activePos = activeBuilding.Position;

        for (int i = 0; i < mapHeight; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                var pix = mapPixels[i, j];

                //if is street
                if (pix.IsActive)
                {
                    var dist = Vector3.Distance(combinedPos, pix.WorldPosition);
                    var scored = new PixelScore(pix, dist);
                    tempPixels.Add(scored);
                }
            }
        }

        var sortedPix = tempPixels.OrderBy(d => d.getScore).ToList();

        List<PixelScore> selectedPix = new List<PixelScore>();

        selectedPix = sortedPix.GetRange(0, 2);

        for (int i = 0; i < selectedPix.Count; i++)
        {
            var pix = selectedPix[i].getPixel;
            outPix.Add(pix);
        }

        return outPix;
    }

    public void FindAccessibleBuildings()
    {
        for (int i = 0; i < mapHeight; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                var activePix = mapPixels[i, j];
                if (activePix.IsActive == false)
                {
                    var neighbors = activePix.getOrthoNeighbors(mapPixels, mapWidth, mapHeight, i, j);
                    if (neighbors.Count > 0) activePix.HasStreet = true;
                    else activePix.HasStreet = false;
                }
                else
                    activePix.HasStreet = false;
            }
        }
    }

    public void CityGen()
    {
        mapPixels = new Pixel[mapWidth, mapHeight];
        ///gen map data
        for (int h = 0; h < mapHeight; h++)
        {
            for (int w = 0; w < mapWidth; w++)
            {
                mapPixels[w, h] = new Pixel(this);
                mapPixels[w, h].Type = (int)(Mathf.PerlinNoise(w / 10.0f + seed, h / 10.0f + seed) * 10);
                mapPixels[w, h].IsActive = false;
                mapPixels[w, h].HasStreet = false;
                mapPixels[w, h].Position = new Vector3Int(w, h, 0);
                mapPixels[w, h].Name = "BLDG " + w.ToString() + h.ToString();
            }
        }

        //build streets
        //street gen largely taken from tutorial
        int x = Random.Range(1, 5);
        for (int n = 0; n < mapHeight; n++)
        {
            for (int h = 0; h < mapWidth; h++)
            {
                if (x >= mapWidth)
                    x = (int)(x - (x / 2));

                mapPixels[x, h].Type = -1;
                mapPixels[x, h].IsActive = true;
                mapPixels[x, h].HasStreet = false;
            }
            x += Random.Range(3, 10);
            if (x > mapWidth) break;
        }

        int z = Random.Range(1, 5);
        for (int n = 0; n < mapHeight; n++)
        {
            for (int w = 0; w < mapWidth; w++)
            {

                if (z >= mapHeight)
                    z = (int)(z - (z / 2));

                if (mapPixels[w, z].Type == -1)
                {
                    mapPixels[w, z].Type = -3;
                    mapPixels[w, z].IsActive = true;
                    mapPixels[w, z].HasStreet = false;
                }
                else
                {
                    mapPixels[w, z].Type = -2;
                    mapPixels[w, z].IsActive = true;
                    mapPixels[w, z].HasStreet = false;
                }
            }
            z += Random.Range(3, 10);
            if (z >= mapHeight) break;
        }

        //Find buildings with street print
        FindAccessibleBuildings();

        bool hqAssigned = false;

        while (hqAssigned == false)
        {
            var randomX = Random.Range(0, mapWidth);
            var randomZ = Random.Range(0, mapHeight);

            if (randomX < mapWidth && randomZ < mapHeight && randomX > 0 && randomZ > 0)
            {
                if (mapPixels[randomX, randomZ].HasStreet)
                {
                    hqAssigned = true;
                    mapPixels[randomX, randomZ].Type = 11;
                    mapPixels[randomX, randomZ].IsActive = true;
                    mapPixels[randomX, randomZ].HasStreet = false;
                }
            }
        }


        Color lerpedColor = Color.gray;
        //gen city
        for (int h = 0; h < mapHeight; h++)
        {
            for (int w = 0; w < mapWidth; w++)
            {
                int result = mapPixels[w, h].Type;
                var regPos = mapPixels[w, h].WorldPosition;
                var origin = new Vector3(0, 0, 0);

                float deltaMoveX = (mapWidth * buildingFootPrint / 2);
                float deltaMoveY = (mapHeight * buildingFootPrint / 2);

                Vector3 pos = new Vector3((w * buildingFootPrint) - deltaMoveX, 0, (h * buildingFootPrint) - deltaMoveY);
                mapPixels[w, h].WorldPosition = pos;

                if (result < -2)
                {
                    GameObject _parent = new GameObject();
                    _parent.isStatic = true;

                   var localPos = pos + new Vector3(0, voxelSize * 1.0f, 0);
                    GameObject street = Instantiate(crossroad, localPos, crossroad.transform.rotation);
                    street.isStatic = true;
                    street.transform.SetParent(_parent.transform, false);

                    mapPixels[w, h].DisplayBuilding = _parent;
                    mapPixels[w, h].IsActive = true;
                }
                else if (result < -1)
                {
                    GameObject _parent = new GameObject();
                    _parent.isStatic = true;

                    var localPos = pos + new Vector3(0, voxelSize * 1.0f, 0);
                    GameObject streetx = Instantiate(xStreets, localPos, xStreets.transform.rotation);
                    streetx.transform.SetParent(_parent.transform, false);
                    streetx.isStatic = true;

                    mapPixels[w, h].DisplayBuilding = _parent;
                    mapPixels[w, h].IsActive = true;
                }
                else if (result < 0)
                {
                    GameObject _parent = new GameObject();
                    _parent.isStatic = true;

                    var localPos = pos + new Vector3(0, voxelSize *1.0f, 0);
                    GameObject streetz = Instantiate(zStreets, localPos, zStreets.transform.rotation);
                    streetz.isStatic = true;
                    streetz.transform.SetParent(_parent.transform, false);

                    mapPixels[w, h].DisplayBuilding = _parent;
                    mapPixels[w, h].IsActive = true;
                }
                else if (result < 1)
                {
                    GameObject bldg = ChooseBuilding(45, 65, pos, lerpedColor);
                    mapPixels[w, h].DisplayBuilding = bldg;

                }
                else if (result < 2)
                {
                    GameObject bldg = ChooseBuilding(30, 45, pos, lerpedColor);
                    mapPixels[w, h].DisplayBuilding = bldg;
                }
                else if (result < 4)
                {
                    GameObject bldg = ChooseBuilding(20, 30, pos, lerpedColor);
                    mapPixels[w, h].DisplayBuilding = bldg;

                }
                else if (result < 5)
                {
                    GameObject bldg = ChooseBuilding(15, 20, pos, lerpedColor);
                    mapPixels[w, h].DisplayBuilding = bldg;
                }
                else if (result < 6)
                {
                    GameObject bldg = ChooseBuilding(10, 15, pos, lerpedColor);
                    mapPixels[w, h].DisplayBuilding = bldg;
                }
                //grass
                else if (result < 7)
                {
                    GameObject bldg = ChooseBuilding(6, 10, pos, lerpedColor);
                    mapPixels[w, h].DisplayBuilding = bldg;
                }
                else if (result < 10)
                {
                    GameObject _parent = new GameObject();
                    GameObject grass = Instantiate(_grass, pos, Quaternion.identity);
                    grass.transform.SetParent(_parent.transform, false);

                    mapPixels[w, h].DisplayBuilding = _parent;
                }

                else if (result == 11)
                {
                    GameObject _parent = new GameObject();
                    GameObject hq = Instantiate(_hq, pos + new Vector3(0, (_hq.transform.localScale.y * 0.5f) + (voxelSize * 0.5f), 0), Quaternion.identity);
                    hq.transform.SetParent(_parent.transform, false);
                    hq.GetComponent<MeshRenderer>().material.color = Color.black;
                    hq.GetComponent<MeshRenderer>().enabled = false;

                    mapPixels[w, h].DisplayBuilding = _parent;
                    mapPixels[w, h].IsActive = true;
                    globalHQ = hq.GetComponent<MeshCollider>();
                }
            }
        }

        getVoids();
        setBounds();
    }

    public GameObject ChooseBuilding(int min, int max, Vector3 pos, Color color)
    {
        var rand = Random.Range(0, 4);
        GameObject output = new GameObject();

        if (rand == 0)
            output = pBuilding.BuildingA(min, max, pos, buildingMat, color);
        else if (rand == 1)
            output = pBuilding.BuildingB(min, max, pos, buildingMat, color);
        else if (rand == 2)
            output = pBuilding.BuildingC(min, max, pos, buildingMat, color);
        else if (rand == 3)
            output = pBuilding.BuildingD(min, max, pos, buildingMat, color);

        return output;
    }



    //check to see if each voxel has reached its target and update targets
    //set voxels that have reached a target to 'active'/ 'working'
    //make next index in indices the target for that job
    public void UpdateJobTarget()
    {
        if (currentJobs.Count > 0)
        {
            foreach (var job in currentJobs)
            {
                if (job.indices.Count < 1) continue;
                else
                {
                    var activeVox = army.grid.GetVoxels().Where(v => v.On)
                    .Where(v => v.Job == job)/*.Where(v => v.Idle == false)*/;

                    foreach (var v in activeVox)
                    {
                        for (int i = 0; i < job.indices.Count; i++)
                        {
                            if (v.Index == job.indices[i])
                            {
                                v.MakeWork();
                                job.indices.RemoveAt(i);

                                for (int t = 0; t < totalJobIndices.Count; t++)
                                {
                                    if (v.Index == totalJobIndices[t])
                                        totalJobIndices.Remove(totalJobIndices[t]);
                                }

                            }
                        }

                    }
                }
            }
        }
        else
        {
            var activeVox = army.grid.GetVoxels().Where(v => v.On)
                   .Where(v => v.Job == null);

           foreach(var a in activeVox)
            {
                if(a.Index == army.hqBestVacant)
                {
                    a.MakeIdle();
                }
            }
        }
    }                                                   

    // Update is called once per frame
    public void Update()
    {
        
        JobHandler();//if job times out, voxel.job set to 'null', and voxels set to 'commute'
        JobCount();
        if (CheckCurrentJobs() == true)
        {

            RedistributeJobs();//every time new job is added or subtracted the army is redistributed to cover all targets
        }
        UpdateJobTarget();
    }

    public void RedistributeJobs()
    {
        IEnumerable <SparseGrid.Voxel> vox;
        if (army.armyVacancyRate < 0.5f)
        {
            vox = army.grid.GetVoxels().Where(v => v.On).Where(v=>v.Job ==null).Where(v=>v.AtWork==false);
        }
        else
            vox = army.grid.GetVoxels().Where(v => v.On).Where(v=>v.AtWork == false);

        if (currentJobs.Count < 1)
        {
            foreach (var v in vox)
            {
                v.Job = null;
                v.MakeCommute();
            }
        }
        else
        {
            foreach (var job in currentJobs)
            {
                var jobSpecVox = vox.Take(job.indices.Count);
                foreach (var j in jobSpecVox)
                {
                    j.Job = job;
                    j.Job.indices = job.indices;
                    j.MakeCommute();

                }
            }
        }
    }

    //determines whether there has been a change in the number of potential/active jobs
    public bool CheckCurrentJobs()
    {
        bool worked = false;
        if (currentJobs.Count != proxyJobCount)
        {
            proxyJobCount = currentJobs.Count;
            worked = true;
        }

        else worked = false;
        return worked;
    }


    public void JobHandler()
    {
   
        //handling potentialJobs
       if(potentialJobs.Count>0)
        {
            for (int i = 0; i < potentialJobs.Count; i++)
            {
                var job = potentialJobs[i];
                var jobStreets = job.streets;
                var jobBuild = job.targetBuilding;
                var indices = job.indices;

                job.Update();

                if (job.timeExpire <= job.timePassed)
                {

                    totalJobIndices.RemoveRange(0, job.indices.Count);
                    potentialJobs.Remove(job);
                    foreach (var js in jobStreets)
                        js.GetComponent<MeshRenderer>().material.color = Color.white;
                    jobBuild.GetComponent<MeshRenderer>().material.color = Color.gray;                         
                }

                if (job.pursue)
                {
                    currentJobs.Add(job);
                    potentialJobs.Remove(job);
                }

                if (job.abort)
                {

                    var activeVox = army.grid.GetVoxels().Where(v => v.On)
                    .Where(v => v.Job == job);

                    foreach (var v in activeVox)
                    {
                        if (v.Job == job)
                        {
                            v.Job = null;
                            v.MakeCommute();
                        }
                    }


                    foreach(var j in job.indices)
                    {
                        totalJobIndices.Remove(j);
                    }
                    potentialJobs.Remove(job);
                    //color streets gray again
                    foreach (var js in jobStreets)
                        js.GetComponent<MeshRenderer>().material.color = Color.white;
                    //color building gray again
                    jobBuild.GetComponent<MeshRenderer>().material.color = Color.gray;
                }
            }
        }

        //handing currentJobs

        if (currentJobs.Count > 0)
        {
            for (int i = 0; i < currentJobs.Count; i++)
            {
                var job = currentJobs[i];
                var jobStreets = job.streets;
                var jobBuild = job.targetBuilding;
                var indices = job.indices;
                job.Update();

                foreach (var js in jobStreets)
                    js.GetComponent<MeshRenderer>().material.color = Color.green;
                jobBuild.GetComponent<MeshRenderer>().material.color = Color.green;


                if (job.completed)
                {

                    var activeVox = army.grid.GetVoxels().Where(v => v.On)
                    .Where(v => v.Job == job);

                    foreach (var v in activeVox)
                    {
                        if (v.Job == job)
                        {
                            v.Job = null;
                            v.MakeCommute();
                        }
                    }

                    _revenue += job.payout;
                    currentJobs.Remove(job);
                    foreach (var js in jobStreets)
                        js.GetComponent<MeshRenderer>().material.color = Color.white;
                    jobBuild.GetComponent<MeshRenderer>().material.color = Color.gray;

                }

                if (job.abort)
                {

                    var activeVox = army.grid.GetVoxels().Where(v => v.On)
                    .Where(v => v.Job == job);

                    foreach (var v in activeVox)
                    {
                        if (v.Job == job)
                        {
                            v.Job = null;
                            v.MakeCommute();
                        }
                    }

                    //totalJobIndices.RemoveRange(0, job.indices.Count);
                    foreach (var j in job.indices)
                    {
                        totalJobIndices.Remove(j);
                    }
                    currentJobs.Remove(job);
                    //color streets gray again
                    foreach (var js in jobStreets)
                        js.GetComponent<MeshRenderer>().material.color = Color.white;
                    //color building gray again
                    jobBuild.GetComponent<MeshRenderer>().material.color = Color.gray;
                }
            }
        }

     }

    public void getVoids() // city voids - used to construct army sparse array
    {
        for (int i = 0; i < mapHeight; i++)
        {
            for (int j = 0; j < mapWidth; j++)
            {
                var go = mapPixels[i, j].DisplayBuilding;
                var childCount = go.transform.childCount;

                if (mapPixels[i, j].Type != 11)
                {
                    if (childCount > 1)
                    {
                        for (int c = 0; c < childCount; c++)
                        {
                            //normal voids
                            if (c != 0)
                            {
                                var bldg = go.transform.GetChild(c).gameObject;
                                //bldg.transform.position = bldg.transform.position + new Vector3(0, voxelSize * 0.5f, 0);
                                var coll = bldg.GetComponent<MeshCollider>();
                                voids.Add(coll);
                            }
                            //big voids (used for optimized sparsgrid)
                            else
                            {
                                  var bldg = go.transform.GetChild(0);
                                 var coll = bldg.GetComponent<MeshCollider>();
                                  voidsOther.Add(coll);
                            }
                        }
                    }
                    else
                    {
                        for (int c = 0; c < childCount; c++)
                        {
                            var bldg = go.transform.GetChild(c).gameObject;
                            var coll = bldg.GetComponent<MeshCollider>();
                            voids.Add(coll);
                        }
                    }
                        
                }
            }
        }
    }

    public void setBounds() //city bounds - used for army sparse array
    {
        var bbox = new Bounds();
        foreach (var v in voids.Select(v => v.bounds))
            bbox.Encapsulate(v);
        var max = bbox.max;
        var min = bbox.min + new Vector3(0, voxelSize, 0);
        bbox.SetMinMax(min, max);
        bbox.Expand(-voxelSize);
        cityBounds = bbox;
    }
}


    public class Job

    {
        public BuildCity.Pixel plot;
         UndirectedGraph<SparseGrid.Voxel, TaggedEdge<SparseGrid.Voxel, SparseGrid.Face>> _graph;

        public float size;
        public float payout;
        public float timeReq;
        public float timePassed;
        public bool completed;
        public bool timeUp;
        public string name;
        public int bldgIndex;
        public List<GameObject> streets;
        public GameObject targetBuilding;
  
        public bool pursue;
        public bool abort;

        public float timeCreate;
        public float timeExpire;

        float reactClock;
 

        public List<Vector3Int> indices;
        public int initIndicesCount;
        public BuildCity city;

    //Generates structures to which fleet needs to attend to.
    //Various instances of this class area consumed in BuildCity.
        public Job(BuildCity city)
        {
            this.city = city;
            _graph = city.army.graph;
            indices = new List<Vector3Int>();
            initIndicesCount = 0;
            plot = new BuildCity.Pixel(city);
            size = new float();
            payout = new float();
            timeReq = new float();
            completed = false;
            timeUp = false;
            pursue = false;
            abort = false;
            timeExpire = new float();
  
            Init(out streets, out targetBuilding);
            InstObj();
            name = plot.Name + string.Format("{0}",  bldgIndex);

            timeCreate = Time.realtimeSinceStartup;
            timeExpire = timeCreate + 150.0f;
            timePassed = timeCreate;

       
        }

        public void Init(out List<GameObject> streetObjs, out GameObject target)
        {
            var bldg = city.PickBuilding(out plot, out bldgIndex);
           
            bldg.GetComponent<MeshRenderer>().material.color = Color.red;

            var streets = city.PickStreets(plot, bldgIndex);
            streetObjs = new List<GameObject>();
            streetObjs.AddRange(streets);
            target = bldg;

            for (int j = 0; j < streets.Count; j++)
            {
                var pos = streets[j].GetComponent<MeshRenderer>().material.color = Color.red;
            }

            var scaleX = bldg.GetComponent<Transform>().transform.localScale.x;
            var scaleZ = bldg.GetComponent<Transform>().transform.localScale.z;
            var scaleY = bldg.GetComponent<Transform>().transform.localScale.y;
            size = scaleX + scaleZ + scaleY;
            payout = size * 20.0f;
            timeReq = size * 2.0f;
            timePassed = timeReq;

        }

        public void InstObj()
        {

            List< SparseGrid.Voxel> start = new List<SparseGrid.Voxel>();
            List<SparseGrid.Voxel> output = new List<SparseGrid.Voxel>();

            var tarCenter = targetBuilding.transform.position;
             Vector3 motion = getFaceMotion(plot, targetBuilding, city);

            var randFactor = Random.Range(2, 5);
            tarCenter += new Vector3(0, targetBuilding.transform.localScale.y/randFactor, 0);
            tarCenter += motion;

            var voxelEnd = city.army.grid.GetVoxels()
            .Where(c => c.IsActive).OrderBy(c => Vector3.Distance(c.Center, tarCenter))
            .ToList();
            var endVox = voxelEnd[0];

            _graph = city.army.graphEdges.ToUndirectedGraph<SparseGrid.Voxel, TaggedEdge<SparseGrid.Voxel, SparseGrid.Face>>();

       
            int startPop = (int)(size *0.07f);
            var modPop = startPop < 1 ? 1 : startPop;
            for (int s = 0; s < streets.Count; s++)
            {
            var voxels = city.army.grid.GetVoxels()
            .Where(c => c.IsActive).OrderBy(c => Vector3.Distance(c.Center, streets[s].transform.position)).ToList();

                for (int c = 0; c < modPop; c++)
                {
                    var startVox = voxels[c];
                    var shortest = _graph.ShortestPathsDijkstra(e => 1, startVox);

                    if(shortest(endVox, out var path))
                    {
                        var current = startVox;
                        indices.Add(current.Index);

                        foreach(var edge in path)
                        {
                            current = edge.GetOtherVertex(current);
                            indices.Add(current.Index);
                        }
                    }
                }
            }

            indices = indices.OrderBy(i => i.y).ToList();
            initIndicesCount = indices.Count;
         }

        public void Update()
        {
         timePassed += Time.deltaTime; // takes care of countdown towards project time out

        if(indices.Count <=0)
            this.completed = true;
        }

    public Vector3 getFaceMotion(BuildCity.Pixel targetPix, GameObject target, BuildCity _city)
    {
        Vector3 delta = new Vector3();
        Vector3 output = new Vector3(0, 0, 0);
        var closest = _city.FindClosestStreetPixel(targetPix);

        delta = closest.DisplayBuilding.transform.position - target.transform.position;

        var x = delta.x;
        var z = delta.z;

        var normalizedX = x / Mathf.Abs(x);
        var normalizedZ = z / Mathf.Abs(z);

        if (Mathf.Abs(x) > Mathf.Abs(z))
        {
            var trans = target.transform.localScale.x * normalizedX;
            output = new Vector3(trans, 0, 0);
        }
        else
        {
            var trans = target.transform.localScale.z * normalizedZ;
            output = new Vector3(0, 0, trans);
        }

        return output;
    }

}

//This class is designed to have one instance that navigates the city gamecourse. 
//It is basically consumed in BuildCity. 
public class Army : MonoBehaviour
{
    BuildCity _city;
    MeshCollider[] _voids; 
    public  Bounds _bounds; //city XYZ bounds
    float _voxelSize;
    MeshCollider[] hq;

    Bounds[] hqBounds;
    public int initVoxCount;

    public GameObject _A;
    public GameObject _B;

    public SparseGrid.Grid3d grid; // main sparsegrid used for navigation of the fleet
    public SparseGrid.Grid3d gridHQ; // a provisional frid used to tell the main grid where to activate HQ voxels
    public float armyVacancyRate;// 0.0f to 1.0f. 1.0f being the entire army is at HQ
    public Vector3Int hqBestVacant; // the index of the first HQ voxel that needs to be filled in upon return. This index is update in a coroutine and of course is constantly changing.


    //navigation graph and graph edges created and stored once
    public IEnumerable<TaggedEdge<SparseGrid.Voxel, SparseGrid.Face>> graphEdges;
    public UndirectedGraph<SparseGrid.Voxel, TaggedEdge<SparseGrid.Voxel, SparseGrid.Face>> graph;

    public Army(BuildCity city)
    {
        _city = city;
        _voids = city.voidsOther.ToArray();
        armyVacancyRate = new float();
        hqBestVacant = new Vector3Int();
      
        _bounds = city.cityBounds;
        _voxelSize = city.voxelSize;
        hq = new MeshCollider[1];
        hq[0] = city.globalHQ;
        hqBounds = new Bounds[1];
        hqBounds[0] = city.globalHQ.bounds;

        _A = Resources.Load("_A", typeof(GameObject)) as GameObject;
        _B = Resources.Load("_B", typeof(GameObject)) as GameObject;

        grid = SparseGrid.Grid3d.MakeWithCity(_bounds, _voids, _voxelSize);
        gridHQ = SparseGrid.Grid3d.MakeGridWithBounds(hqBounds, _voxelSize);

        InitGraph();

        AttachGameObjects();
        PopulateWithHQ();
    }

    public IEnumerable<SparseGrid.Face> GetFaces()
    {
        var faces = grid.GetFaces().Where(f => f.IsActive);
        foreach(var f in faces)
        {
            yield return f;
        }
    }

    public void InitGraph()
    {
       var faces= GetFaces();
       graphEdges = faces.Select(f => new TaggedEdge<SparseGrid.Voxel, SparseGrid.Face>(f.Voxels[0], f.Voxels[1], f));
       graph = graphEdges.ToUndirectedGraph<SparseGrid.Voxel, TaggedEdge<SparseGrid.Voxel, SparseGrid.Face>>();
    }

    public void PopulateAll()
    {
        var genVox = grid.Voxels.ToList();
        
        foreach(var v in genVox)
        {
            v.Value.SwitchOn();
        }
    }

    public void PopulateWithHQ()
    {
        int count = 0;
        var voxHQ = gridHQ.Voxels.ToList();
        var genVox = grid.Voxels.ToList();
 

        for (int k = 0; k < genVox.Count; k++)
        {
            var center = genVox[k].Value.Center;
            for (int v = 0; v < voxHQ.Count; v++)
            {
                if(voxHQ[v].Value.Center == center)
                {
                    genVox[k].Value.Index = grid.IndexFromPoint(center);
                    genVox[k].Value.SwitchOn();
                    genVox[k].Value.HQ = true;
                    count++;
                }
            }
        }

    }


    public IEnumerator MoveDudes()
    {
        for (int c = 0; c < 100000; c++)
        {
            UpdateLandlocked();
            if(!((grid.GetVoxels().Where(v => v.On).Where(v => v.IsMovable).Where(v => v.Commute) == null ||
                grid.GetVoxels().Where(v => v.On).Where(v => v.IsMovable).Where(v => v.Commute).Count() ==0)))
            {

                var opVox = grid.GetVoxels().Where(v => v.On).Where(v => v.IsMovable).Where(v => v.Commute);
                var opVoxCount = opVox.Count();

                var index = Random.Range(0, opVoxCount - 1);
                var start = opVox.ElementAt(index);
 

                Vector3Int endIndex = new Vector3Int();

                if (start.Job == null || start.Job.indices.Count < 1)
                {
                    endIndex = hqBestVacant;
                }
                else
                {
                    endIndex = start.Job.indices[0];
                }

                if (grid.Voxels.TryGetValue(endIndex, out var end))
                {

                    var shortest = graph.ShortestPathsDijkstra(_ => 1, start);
                    SparseGrid.Voxel next = null;
                    List<SparseGrid.Voxel> pathList = new List<SparseGrid.Voxel>();

                    if (shortest(end, out var path))
                    {
                        var current = start;
                        foreach (var edge in path)
                        {
                            current = edge.GetOtherVertex(current);
                            pathList.Add(current);
                        }

                        var indNex = pathList.Count() <= 10 ? pathList.Count() - 1 : (int)(pathList.Count() * 0.5f);

                        if (indNex <= 10) next = end;
                        else next = pathList.ElementAt(indNex);
                    }
                    else
                    {
                        Debug.Log("shortest path not found");
                        continue;
                    }

                    if (next.On == false)
                    {
                        next.SwitchOn();
                        next.Job = start.Job;
                        next.AtWork = start.AtWork;
                        next.Idle = start.Idle;
                        next.Commute = start.Commute;

                        start.SwitchOff();
                        start.Job = null;
                        start.AtWork = false;
                        start.Idle = false;
                        start.Commute = false;
                    }

                    UpdateEmptyHQIndex();
                    UpdateBottoms();
                    UpdateArmyVacancyRate();

                }
            }
                yield return new WaitForSeconds(0.0001f);
        }
    }

    private void UpdateArmyVacancyRate()
    {
        var hqVacant = grid.GetVoxels().Where(v => v.On == false)
          .Where(v => v.HQ == true)
          .OrderBy(v => v.Center.y).Count();
        var hqTotal = grid.GetVoxels().Where(v => v.HQ == true).Count();
        armyVacancyRate = (float) hqVacant/ hqTotal;
    }

    public void  UpdateEmptyHQIndex()
    {
       var hqVacant =  grid.GetVoxels().Where(v => v.On == false)
            .Where(v => v.HQ == true)
            .OrderBy(v => v.Center.y);

        if (hqVacant.Count() == 0 || hqVacant == null)
            return;

           hqBestVacant = hqVacant.First().Index;
      
    }

    //gets the index of the best neighbour
    public bool SecondBestMove(SparseGrid.Voxel voxel, out Vector3Int outIndex)
    {
        var neighbours = voxel.GetFaceNeighbours().Where(n=>n.On == false);

        Vector3Int target = new Vector3Int(0, 0, 0);
        if (voxel.Job != null)
            if(voxel.Job.indices.Count> 0 || voxel.Job.indices != null)
                target = voxel.Job.indices[0];
        else
            target = hqBestVacant;

        outIndex = new Vector3Int();
        bool worked = false;

        if (grid.Voxels.TryGetValue(target, out var end))
        {
            var neighRanking = new List<VoxelScore>();

            foreach (var n in neighbours)
            {
                neighRanking.Add(new VoxelScore(n.Index, getManhattan(n, end)));
            }

            var sortedRank = neighRanking.OrderBy(s => s.score);
            var secondBest = sortedRank.First().index;
            outIndex = secondBest;
            worked = true;
        }

        return worked;
    }

    //checks to see if there is an empty space below, if so, it moves there
    public void CheckBottom(SparseGrid.Voxel voxel)
    {
        if (voxel.AtWork == false)
        {
            var neighbourLow = voxel.GetBottomNeighbour();
            if (neighbourLow != null)
                if (neighbourLow.On == false)
                {
                    neighbourLow.SwitchOn();
                    neighbourLow.Job = voxel.Job;
                    neighbourLow.AtWork = voxel.AtWork;
                    neighbourLow.Idle = voxel.Idle;
                    neighbourLow.Commute = voxel.Commute;

                    voxel.SwitchOff();
                    voxel.Job = null;
                    voxel.AtWork = false;
                    voxel.Idle = false;
                    voxel.Commute = false;
                }
        }
    }

    //makes sure all voxels drop to the lowest Y point in city
    public void UpdateBottoms()
    {
        var voxelsOn = grid.GetVoxels().Where(v => v.On);
        foreach(var vox in voxelsOn)
            CheckBottom(vox);
    }

    //keeps army updated on which members can move
    public void UpdateLandlocked()
    {
        var voxelsOn = grid.GetVoxels().Where(v => v.On);
        foreach (var v in voxelsOn)
        {
            int vacant = 0;
            var neighbor = v.GetFaceNeighbours();
            foreach (var n in neighbor)
                if (n.On == false)
                    vacant++;

            if (vacant > 1) v.IsMovable = true;
            else v.IsMovable = false;
        }
    }

    //apply once upon instantiation
public void AppendGO(SparseGrid.Voxel voxel)
    {
        if ((voxel.Index.x + voxel.Index.y + voxel.Index.z) % 2 == 0)
        {
            GameObject go = Instantiate(_A, voxel.Center, Quaternion.identity);
            voxel.DisplayCell = go;
            voxel.SwitchOff();
        }
        else
        {
            GameObject go = Instantiate(_B, voxel.Center, Quaternion.identity);
            voxel.DisplayCell = go;
            voxel.SwitchOff();
        }
    }
   
    //apply once upon intantiation
    public void AttachGameObjects()
    {
        List<Vector3Int> keys = grid.Voxels.Keys.ToList();

        for (int i = 0; i < keys.Count; i++)
        {
            var key = keys[i];

            SparseGrid.Voxel voxel;
            if (grid.Voxels.TryGetValue(key, out voxel))
                AppendGO(voxel);
        }
    }

    public int getManhattan(SparseGrid.Voxel source, SparseGrid.Voxel target)
    {
        int manhattan = 0;

        int X = (int)(target.Center.x - source.Center.x);
        int Y = (int)(target.Center.y - source.Center.y);
        int Z = (int)(target.Center.z - source.Center.z);

        manhattan = X + Y + Z;
        return manhattan;
    }

}

//Classes that I am using for the purposes of scoring and sorting instances of value pairs.
//Using them like custom dictionary classes.

public class JobScore
{
    public Job job { get; set; }
    public float score { get; set; }

    public JobScore(Job job, float score)
    {
        this.job = job;
        this.score = score;
    }

}
public struct VoxelScore
{
    public Vector3Int index { get; set; }
    public int score { get; set; }

    public VoxelScore(Vector3Int index, int score)
    {
        this.index = index;
        this.score = score;
    }
}

public class PixelScore
{
    BuildCity.Pixel pixel;
    float score;

    public BuildCity.Pixel getPixel
    {
        get { return pixel; }
    }

    public float getScore
    {
        get { return score; }
    }

    public PixelScore(BuildCity.Pixel pixel, float score)
    {
        this.pixel = pixel;
        this.score = score;
    }
}

public class GOScore
{
    public GameObject GO { get; set; }
    public float score { get; set; }
    public int index { get; set; }

    public GOScore(GameObject GO, float score, int index)
    {
        this.GO = GO;
        this.score = score;
        this.index = index;
    }

}






