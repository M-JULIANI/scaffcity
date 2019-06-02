using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


//This class is, for all intents and purposes authored by me. The 'draw mesh' of the ghost targets was taken from 
//'Util' code authored by Vicente Soler.
public class UIControl : MonoBehaviour
{
    [SerializeField]
    GUISkin _skin;

    public BuildCity city;
    int maxW = 3;
    int maxH = 3;
    BuildWindow[,] windowArray;
    BusinessLogic gameStats;
    GameObject mainController;
    GameObject gameTimeObj;
    GameTime gameTimeInst;
    float elapsedGameTime;
    float startTime;

    List<Job> _allJobs;
    int jobCount;

    //game stats
    int _fleetSize;
    float _unitCostIdle;
    float _unitCostActive;
    float _fleetCost;
    float _revenue;
    float _profit;

    float _globalPayout;

    List<Rect> rectangles = new List<Rect>();
    Rect _windowRect = new Rect(20, 20, 150, 160);
    int windowCount;
    bool gameStarted;
    bool gameWon;

    public void Awake()
    {
        mainController = GameObject.Find("MainController");
        gameTimeObj = GameObject.Find("GameTime");
        gameStats = mainController.GetComponent<BusinessLogic>();
        gameTimeInst = gameTimeObj.GetComponent<GameTime>();
        InitWindowArray();
        _allJobs = new List<Job>();
        _globalPayout = new float();
        gameStarted = false;
        startTime = 0.0f;
        gameWon = false;
        _fleetCost = 10.0f;
        _revenue = 1.0f;

        
    }

    // Start is called before the first frame update
    public void Start()
    {
        city = new BuildCity(7, 7);
        city.Start();
        StartCoroutine(city.WakeCity());
        jobCount = -1;
    }

    public BuildCity getCity()
    {
        return city;
    }

    public void InitWindowArray()
    {
        windowArray = new BuildWindow[maxW, maxH];
        int count = 0;
        for (int h = 0; h < maxH; h++)
        {
            for (int w = 0; w < maxW; w++)
            {
                windowArray[w, h] = new BuildWindow();
                windowArray[w, h].ActiveJob = false;
                windowArray[w, h].On = false;
                windowArray[w, h].Position = new Vector3Int(w, h, 0);
                windowArray[w, h].ID = count;
                windowArray[w, h].Name = count.ToString();
                windowArray[w, h].Rectangle = new Rect(windowArray[w, h].Position.x * 180 + 50, windowArray[w, h].Position.y *200 + 50, 180 , 180 );
                count++;    
            }
        }
    }

    public void OnGUI()
    {
        GUI.skin = _skin;

        var maxHM = (int)(_allJobs.Count / maxH);
        var maxWM = (int)(_allJobs.Count % maxW);

        var modH = 0;
        if (maxWM == 0)
            modH = maxHM;
        else
         modH = maxHM + 1;
        var modW = _allJobs.Count >= 3 ? 3 : _allJobs.Count;

        int count = 0;

        for (int h = 0; h < modH; h++)
        {
            for (int w = 0; w < modW; w++)
            {

                if (count >= _allJobs.Count)
                    break;
                else
                {
                    if(windowArray[w,h].ActiveJob)
                    {
                            GUI.color = Color.green;
                            GUI.Window(windowArray[w, h].ID, windowArray[w, h].Rectangle, WindowFunction, windowArray[w, h].Name);
                    }
                    else
                    {
                            GUI.color = Color.gray;
                            GUI.Window(windowArray[w, h].ID, windowArray[w, h].Rectangle, WindowFunction, windowArray[w, h].Name);
                    }
                    count++;
                }       
            }
        }

        if (gameWon)
            GUI.Label(new Rect(600,300,800,600), "NOW THAT IS A BUSINESS!");

        //gamstats 
        Rect rectangle = new Rect(50, 600, 300, 300);
        GUI.color = Color.gray;
        GUI.Window(800, new Rect(rectangle), statFunc, "Game Stats");

        //reset city
        Rect rectangle1 = new Rect(50, 915, 120, 80);
        GUI.color = Color.gray;
        if (GUI.Button(rectangle1, "RESET CITY"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Time.timeScale = 1.0f;
        }

        //start fleet coroutine
        Rect rectangle2 = new Rect(240, 915, 100, 80);
        GUI.color = Color.gray;
        if (GUI.Button(rectangle2, "START"))
        {
            StartCoroutine(city.army.MoveDudes());
        }
    }

    //game stats
    void statFunc(int ID)
    {
        int i = 1;
        int m = 30;
        int s = 35;

        GUI.TextField(new Rect(m, s * i++, 240, m), "Fleet Size " + string.Format("{0:0}", _fleetSize));
        GUI.TextField(new Rect(m, s * i++, 240, m), "Idle Unit Cost " + string.Format("${0:0.00}", _unitCostIdle));
        GUI.TextField(new Rect(m, s * i++, 240, m), "Active Unit Cost " + string.Format("${0:0.00}", _unitCostActive));
        GUI.TextField(new Rect(m, s * i++, 240, m), "Fleet Cost " + string.Format("${0:0.00}", _fleetCost));
        GUI.TextField(new Rect(m, s * i++, 240, m), "Revenue " + string.Format("${0:0.00}", _revenue));
        GUI.TextField(new Rect(m, s * i++, 240, m), "Profit " + string.Format("${0:0.00}", _profit));
        GUI.TextField(new Rect(m, s * i++, 240, m), "Elapsed Time " + string.Format("{0:0.00}", elapsedGameTime));

    }
    void WindowFunction(int windowID)
    {
        var maxHM = (int)(_allJobs.Count / maxH);
        var maxWM = (int)(_allJobs.Count % maxW);

        var modH = 0;
        if (maxWM == 0)
            modH = maxHM;
        else
            modH = maxHM + 1;
        var modW = _allJobs.Count >= 3 ? 3 : _allJobs.Count;

        int count = 0;
        for (int h = 0; h < modH; h++)
        {
            for (int w = 0; w < modW; w++)
            {
                int i = 1;
                int s = 25;

                if (windowArray[w, h].ID == windowID)
                {
                    if (GUI.Button(new Rect(s, s * i++, 125, 20), "Pursue"))
                    {
                        {
                            windowArray[w, h].Job.pursue = true;
                            windowArray[w, h].ActiveJob = true;
                        }
                    }
                    if (GUI.Button(new Rect(s, s * i++, 125, 20), "Abort"))
                    {
                        {
                            windowArray[w, h].Job.abort = true;
                            windowArray[w, h].ActiveJob = false;
                        }
                    }

                            var payout = windowArray[w, h].Payout;
                            var timeReq = windowArray[w, h].TimeReq;
                            var timePassed = windowArray[w, h].TimeElapsed;
                            var fleetReq = windowArray[w, h].FleetReq;
                            var fleetInit = windowArray[w, h].FleetInitCount;

                            GUI.TextArea(new Rect(s, s * i++, 125, 20), "F_REQ " + string.Format("{0:0}/{1:0}", fleetReq, fleetInit));
                            GUI.TextArea(new Rect(s, s * i++, 125, 20), "PAYOUT " + string.Format("${0:0.00}", payout));
                            GUI.TextArea(new Rect(s, s * i++, 125, 20), "TIME REQ " + string.Format("{0:0.00}", timeReq));
                            GUI.TextArea(new Rect(s, s * i++, 125, 20), "TIME LEFT " + string.Format("{0:0.00}", timePassed));

                    count++;
                }
            }
        } 
    }

    void Update()
    {
        city.Update();
 
        //end game if this condition is met
            if (_revenue>= _fleetCost)
            {
                gameWon = true;
                Debug.Log("Your business broke even!");
                Time.timeScale = 0.0f;
            }

        //draw all active targets
            var strTargets = city.totalJobIndices;
            for (int i = 0; i < strTargets.Count; i++)
            {
                city.army.grid.Voxels.TryGetValue(strTargets[i], out var end);
                var center = end.Center;
                Drawing.DrawCube(center, 1.8f, 0.1f);
            }

            findJobsNew();
            RedrawWindows();

            _fleetSize = gameStats.getFleetSize();
            _unitCostIdle = gameStats.getUnitCostIdle();
            _unitCostActive = gameStats.getUnitCostActive();
            _fleetCost = gameStats.getFleetCost();
            _revenue = gameStats.getRevenue();
            _profit = gameStats.getProfit();


        elapsedGameTime = gameTimeInst.getTime();
      }


    public void findJobsNew()
    {
        var jobs = city.UpdateAllJobs();
        _allJobs = jobs;

    }

    public void RedrawWindows()
    {
        int count = 0;

        var maxHM = (int)(_allJobs.Count / maxH);
        var maxWM = (int)(_allJobs.Count % maxW);

        var modH = 0;
        if (maxWM == 0)
            modH = maxHM;
        else
            modH = maxHM + 1;
        var modW = _allJobs.Count >= 3 ? 3 : _allJobs.Count;

        for (int h = 0; h < modH; h++)
        {
            for (int w = 0; w < modW; w++)
            {
                windowArray[w, h].ID = count;
                for (int i = 0; i < _allJobs.Count; i++)
                {
                    if (windowArray[w, h].ID == i)
                    {
                        {
                            windowArray[w, h].Job = _allJobs[i];
                            windowArray[w, h].Name = windowArray[w, h].Job.name;
                            windowArray[w, h].Payout = windowArray[w, h].Job.payout;
                            windowArray[w, h].TimeReq = windowArray[w, h].Job.timeReq;
                            windowArray[w, h].TimeElapsed = windowArray[w, h].Job.timePassed;
                            windowArray[w, h].FleetReq = windowArray[w, h].Job.indices.Count;
                            windowArray[w, h].FleetInitCount = windowArray[w, h].Job.initIndicesCount;
                            windowArray[w, h].ActiveJob = windowArray[w, h].Job.pursue;

                        }
                        count++;
                    }
                }
            }
        }
    }

}

//window basec class
public class BuildWindow
{
    BuildCity mapGrid;
    public bool On { get; set; }
    public Vector3Int Position { get; set; }
    public Vector3 ScreenPosition { get; set; }
    public Rect Rectangle { get; set; }
    public string Name { get; set; }
    public int ID { get; set; }
    public Job Job{ get; set; }
    public int FleetReq { get; set; }
    public int FleetInitCount { get; set; }
    public float Payout { get; set; }
    public float TimeReq { get; set; }
    public float TimeElapsed { get; set; }
    public bool ActiveJob { get; set; }

    public BuildWindow()
    {
        
    }


}

