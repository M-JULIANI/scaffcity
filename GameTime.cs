using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTime : MonoBehaviour
{

    //The sun rotation logic is largely taken from a tutorial : https://www.youtube.com/watch?v=zxIyZzieFqE
    //The logic of turning off the sun light as well as activating/ deactivating street lights is mine.
    public Transform[] sun;
    public float dayCycleInMinutes = 1;
    private const float second = 1;
    private const float minute = 60 * second;
    private const float hour = 60 * minute;
    private const float day = 24 * hour;
    private const float degreesPerSecond = 360 / day;
    private float _degreeRotation;
    private float _timeOfDay;
    private float _tod;

    GameObject mainController;

    GameObject[] nightLights;
    BuildCity _city = null;
    List<BuildCity.Pixel> cityPix;



    private void Awake()
    {
       mainController = GameObject.Find("MainController");
        cityPix = new List<BuildCity.Pixel>();
    }

    void Start()
    {  
        _timeOfDay = 0.0f;
        _tod = 0.0f;
        _degreeRotation = degreesPerSecond * day / (dayCycleInMinutes * minute);
        
        StartCoroutine(startNightLights()); 
    }

  
    void Update()
    {
        sun[0].Rotate(new Vector3(_degreeRotation, 0, 0) * Time.deltaTime);
        _timeOfDay += Time.deltaTime;


        if (sun[0].transform.rotation.eulerAngles.x >= 180.0f || sun[0].transform.rotation.eulerAngles.x <= 0.0f)
        {
            sun[0].GetComponent<LensFlare>().enabled = false;
            sun[0].GetComponent<Light>().enabled = false;
            if (nightLights != null)
                LightsOn();
        }
        else
        {
            sun[0].GetComponent<LensFlare>().enabled = true;
            sun[0].GetComponent<Light>().enabled = true;
            if(nightLights != null)
                LightsOff();
        }

        var remapped = Mathf.Cos(2 * Mathf.PI * _degreeRotation/360.0f *  _timeOfDay);
        RenderSettings.ambientLight = new Color(remapped, remapped, remapped);
    }

    
    public IEnumerator startNightLights() //takes care of turning lights on/off with the sun cycle
    {
        yield return new WaitForSeconds(1.0f);
        _city = mainController.GetComponent<UIControl>().city;
        cityPix.AddRange(_city.GetIntersectionPixels());
        InstantiateLights(cityPix);
    }

    public void LightsOn() //turns lights on
    {
        foreach (var n in nightLights)
            n.GetComponent<Light>().enabled = true;
    }
    public void LightsOff() //turns lights off
    {
        foreach (var n in nightLights)
            n.GetComponent<Light>().enabled = false;

    }

    public GameObject[] InstantiateLights(List<BuildCity.Pixel> streets) //Instantiates lights over street and intersection 'pixels'
    {
        var intCount = streets.Count;
        nightLights = new GameObject[intCount];

        for (int i = 0; i < nightLights.Length; i++)
        {
            nightLights[i] = new GameObject();
            nightLights[i].name = "nightLight";
            var lightComp = nightLights[i].AddComponent<Light>();

            lightComp.type = LightType.Spot;
            lightComp.spotAngle = 52;
            lightComp.intensity = 3.5f;
            lightComp.range = 16.0f;
            lightComp.color = Color.white;

            lightComp.enabled = false;
            var pos = streets[i].WorldPosition;
            pos += new Vector3(0, 15, 0);
            nightLights[i].transform.position = pos;
            nightLights[i].transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        }

        return nightLights;
    }
    public float getTime()
    {
        return _timeOfDay;
    }

}
