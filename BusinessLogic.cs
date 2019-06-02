using UnityEngine;

//This class is all mine. A bit convoluted and perhaps slightly unneccesary to keep it as a separate class
// but I did it for the clarity of understanding what numbers are being shunted from where to where.

public class BusinessLogic:MonoBehaviour
{
    public int fleetSize;
    public float unitCostIdle;
    public float unitCostActive;
    public float fleetCost;
    public float profit;
    public float revenue;
    public float initFleetCost;

    BuildCity city;
    GameObject mainController;

    public float getRevenue()
    {
        return revenue;
    }
    public float getUnitCostActive()
    {
        return unitCostActive;
    }
    public float getUnitCostIdle()
    {
        return unitCostIdle;
    }
    public float getFleetCost()
    {
        return fleetCost;
    }
    public int getFleetSize()
    {
        return fleetSize;
    }
    public float getProfit()
    {
        return profit;
    }
    public void Awake()
        {
        mainController = GameObject.Find("MainController");
        }

        public void Start()
        {
            city = mainController.GetComponent<UIControl>().getCity();
            fleetSize = 0;
            unitCostActive = 20.0f;
            unitCostIdle = 5.0f;
            initFleetCost = 0.0f;
            fleetCost = 500.0f;
            revenue = 0.0f;
            profit = 0.0f;
        }

        public void Update()
        {
            city.UpdateFleetStatus();
            fleetSize = city.getFleetSize();
            initFleetCost = fleetSize * unitCostIdle;
            fleetCost = initFleetCost + (((unitCostIdle * city.fleetCountIdle) + (unitCostActive * city.fleetCountActive)) * Time.fixedTime*0.001f);
            revenue = city._revenue;
            profit = revenue - fleetCost;

        }




    
}
