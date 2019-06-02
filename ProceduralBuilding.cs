using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static UnityEngine.Mathf;


//This class is all authored by me as you might be able to tell. It started out being fairly flexible, and slowly moved 
//towards 'hard coded' to work with my module. Changing it back to be fully parametric would only require changing not more than handful of lines.

public class ProceduralBuilding
{
    int boundingXZ;
    float multiplierSpacing;
    BuildCity city;
    float _voxelSize;

    List<GameObject> buildings;


        public void Start()
    {
        buildings = new List<GameObject>();

    }
    public ProceduralBuilding(int boundingXZ, float multiplierSpacing, BuildCity city)
    {
        this.boundingXZ = boundingXZ;
        this.multiplierSpacing = multiplierSpacing;
        this.city = city;
        _voxelSize = city.voxelSize;

    }


    public GameObject BuildingA(int lowerRange, int upperRange, Vector3 position, Material outline, Color color)
    {
        GameObject _parent = new GameObject();
        _parent.transform.position = position;
        _parent.AddComponent<MeshRenderer>();
       _parent.AddComponent<MeshCollider>();
        _parent.isStatic = true;

        float module = this.boundingXZ / 6.0f;
        float doubleMod = module * 2.0f;

        for (double i = 0; i < 3; i++)
        {
            for (double j = 0; j < 3; j++)
            {
                var rand = Random.Range(lowerRange, upperRange);
                var randMod = rand % 2 == 0 ? rand : rand + 1;

                var point = new Vector3((float)(i * doubleMod + module) - (this.boundingXZ / 2.0f),0.0f,(float) (j * doubleMod + module) - this.boundingXZ / 2.0f);

                var pos = point + new Vector3(0.0f, (randMod * multiplierSpacing / 2) + _voxelSize * 1.0f, 0.0f);

                var geo = (GameObject.CreatePrimitive(PrimitiveType.Cube));
                geo.AddComponent<MeshCollider>();
                geo.GetComponent<BoxCollider>().enabled = false;
                geo.GetComponent<MeshRenderer>().material = outline;
                geo.GetComponent<MeshRenderer>().material.color = color;
                geo.transform.position = pos;
                var scale = geo.transform.localScale;
                var newScale = scale + new Vector3((doubleMod - 1.0f) * multiplierSpacing, randMod, (doubleMod - 1.0f) * multiplierSpacing);

                geo.transform.localScale = newScale;
                geo.isStatic = true;
                geo.transform.SetParent(_parent.transform, false);

            }
        }

        var geoBig = (GameObject.CreatePrimitive(PrimitiveType.Cube));
        geoBig.transform.position = position;
        var scaleB = geoBig.transform.localScale;
        var newScaleB = scaleB + new Vector3(12, 100, 12);
        geoBig.transform.localScale = newScaleB;
        geoBig.AddComponent<MeshCollider>();
        geoBig.GetComponent<MeshRenderer>().material.color = Color.magenta;
        geoBig.GetComponent<MeshRenderer>().enabled = false;
        geoBig.isStatic = true;

        geoBig.transform.SetParent(_parent.transform, true);
        geoBig.transform.SetAsFirstSibling();

        return _parent;
    }

 public GameObject BuildingB(int lowerRange, int upperRange, Vector3 position, Material outline, Color color)
    {
        GameObject _parent = new GameObject();
        _parent.transform.position = position;
        _parent.AddComponent<MeshRenderer>();
        _parent.AddComponent<MeshCollider>();
        _parent.isStatic = true;
        float module = boundingXZ / 4.0f;
        float doubleMod = module * 2.0f;

        for (double i = 0; i < 2; i++)
        {
            for (double j = 0; j < 2; j++)
            {
                var rand = Random.Range(lowerRange, upperRange);
                var randMod = rand % 2 == 0 ? rand : rand + 1;

                var point = new Vector3((float) (i * doubleMod + module) - (boundingXZ / 2.0f),0.0f, (float)(j * doubleMod + module) - (boundingXZ / 2.0f));
                var pos = point + new Vector3(0.0f, (randMod * multiplierSpacing / 2)+ _voxelSize * 1.0f, 0.0f);
                var geo = (GameObject.CreatePrimitive(PrimitiveType.Cube));
                geo.AddComponent<MeshCollider>();
                geo.GetComponent<BoxCollider>().enabled = false;
                geo.GetComponent<MeshRenderer>().material = outline;
                geo.GetComponent<MeshRenderer>().material.color = color;
                geo.transform.position = pos;

                var scale = geo.transform.localScale;
                var newScale = scale + new Vector3((doubleMod - 1.0f) * multiplierSpacing, randMod, (doubleMod - 1.0f) * multiplierSpacing);

                geo.transform.localScale = newScale;     
                geo.transform.SetParent(_parent.transform, false);
                geo.isStatic = true;
            }
        }

        //big mesh collider to subtract from overall sparse mesh volume
        var geoBig = (GameObject.CreatePrimitive(PrimitiveType.Cube));
        geoBig.transform.position = position;
        var scaleB = geoBig.transform.localScale;
        var newScaleB = scaleB + new Vector3(12, 100, 12);
        geoBig.transform.localScale = newScaleB;
        geoBig.AddComponent<MeshCollider>();
        geoBig.isStatic = true;
        geoBig.GetComponent<MeshRenderer>().material.color = Color.magenta;
        geoBig.GetComponent<MeshRenderer>().enabled = false;

        geoBig.transform.SetParent(_parent.transform, true);
        geoBig.transform.SetAsFirstSibling();

        return _parent;
    }

    public GameObject BuildingC(int lowerRange, int upperRange, Vector3 position, Material outline, Color color)
    {
        GameObject _parent = new GameObject();
        _parent.transform.position = position;
        _parent.AddComponent<MeshRenderer>();
        _parent.AddComponent<MeshCollider>();
        _parent.isStatic = true;
        GameObject[] gos = new GameObject[4];

        var rand1 = Random.Range(lowerRange, upperRange);
        var randMod1 = rand1 % 2 == 0 ? rand1 : rand1 + 1;
        var rand2 = Random.Range(lowerRange, upperRange);
        var randMod2 = rand2 % 2 == 0 ? rand2 : rand2 + 1;
        var rand3 = Random.Range(lowerRange, upperRange);
        var randMod3 = rand3 % 2 == 0 ? rand3 : rand3 + 1;
        var rand4 = Random.Range(lowerRange, upperRange);
        var randMod4 = rand4 % 2 == 0 ? rand4 : rand4+ 1;
        float module = boundingXZ / 6.0f;
        float doubleMod = module * 2.0f;

        var block1 = BldgCRoutine(1, 2, randMod1, module, module, doubleMod, position, outline, color);
        var block2 = BldgCRoutine(4, 1, randMod2, module, doubleMod, module, position, outline, color);
        var block3 = BldgCRoutine(5, 4, randMod3, module, module, doubleMod, position, outline, color);
        var block4 = BldgCRoutine(2,5, randMod4, module, doubleMod, module, position, outline, color);

        gos[0] = block1;
        gos[1] = block2;
        gos[2] = block3;
        gos[3] = block4;


        for (int i = 0; i < gos.Length; i++)
        {
            gos[i].transform.SetParent(_parent.transform, false);
        }

        //big mesh collider to subtract from overall sparse mesh volume
        var geoBig = (GameObject.CreatePrimitive(PrimitiveType.Cube));
        geoBig.transform.position = position;
        var scaleB = geoBig.transform.localScale;
        var newScaleB = scaleB + new Vector3(12, 100, 12);
        geoBig.transform.localScale = newScaleB;
        geoBig.AddComponent<MeshCollider>();
        geoBig.isStatic = true;
        geoBig.GetComponent<MeshRenderer>().material.color = Color.magenta;
        geoBig.GetComponent<MeshRenderer>().enabled = false;

        geoBig.transform.SetParent(_parent.transform, true);
        geoBig.transform.SetAsFirstSibling();

        return _parent;
    }

    public GameObject BuildingD(int lowerRange, int upperRange, Vector3 position, Material outline, Color color)
    {
        GameObject _parent = new GameObject();
        _parent.transform.position = position;
        _parent.AddComponent<MeshRenderer>();
        _parent.AddComponent<MeshCollider>();
        _parent.isStatic = true;
        float module = boundingXZ / 4.0f;
        var rand1 = Random.Range(lowerRange, upperRange);
        var randMod1 = rand1 % 2 == 0 ? rand1 : rand1 + 1;
        var rand2 = Random.Range(lowerRange, upperRange);
        var randMod2 = rand2 % 2 == 0 ? rand2 : rand2 + 1;

        var pointA = new Vector3(module * 2.0f - (boundingXZ / 2.0f), 0.0f, 3.0f * module - (boundingXZ / 2.0f));
        var posA = pointA + new Vector3(0.0f, (randMod1 * multiplierSpacing / 2)+ _voxelSize * 1.0f, 0.0f);
        var geoA = (GameObject.CreatePrimitive(PrimitiveType.Cube));
        geoA.AddComponent<MeshCollider>();
        geoA.GetComponent<BoxCollider>().enabled = false;
        geoA.GetComponent<MeshRenderer>().material = outline;
        geoA.GetComponent<MeshRenderer>().material.color = color;
        geoA.transform.position = posA;
        var scaleA = geoA.transform.localScale;
        var newScaleA = scaleA + new Vector3(((module*4)-1.0f) * multiplierSpacing, randMod1, ((module*2)-1.0f) * multiplierSpacing);
        geoA.transform.localScale = newScaleA;
        geoA.isStatic = true;
        geoA.transform.SetParent(_parent.transform, false);

        var pointB = new Vector3(module * 2 - (boundingXZ / 2.0f), 0.0f, module - (boundingXZ / 2.0f));
        var posB = pointB + new Vector3(0.0f, (randMod2 * multiplierSpacing / 2) + _voxelSize*1.0f, 0.0f);
        var geoB = (GameObject.CreatePrimitive(PrimitiveType.Cube));
        geoB.AddComponent<MeshCollider>();
        geoB.GetComponent<BoxCollider>().enabled = false;
        geoB.GetComponent<MeshRenderer>().material = outline;
        geoB.GetComponent<MeshRenderer>().material.color = color;
        geoB.transform.position = posB;
        var scaleB = geoB.transform.localScale;
        var newScaleB = scaleB + new Vector3(((module*4.0f)-1.0f) * multiplierSpacing, randMod2, ((module*2.0f)-1.0f) * multiplierSpacing);
        geoB.transform.localScale = newScaleB;
        geoB.isStatic = true;
        geoB.transform.SetParent(_parent.transform, false);

        //big mesh collider to subtract from overall sparse mesh volume
        var geoBig = (GameObject.CreatePrimitive(PrimitiveType.Cube));
        geoBig.transform.position = position;
        var scaleBig = geoBig.transform.localScale;
        var newScaleBig = scaleBig + new Vector3(12, 100, 12);
        geoBig.transform.localScale = newScaleBig;
        geoBig.AddComponent<MeshCollider>();
        geoBig.isStatic = true;
        geoBig.GetComponent<MeshRenderer>().material.color = Color.magenta;
        geoBig.GetComponent<MeshRenderer>().enabled = false;

        geoBig.transform.SetParent(_parent.transform, true);
        geoBig.transform.SetAsFirstSibling();

        return _parent;
    }

    public GameObject BldgCRoutine(int multiplier1, int multiplier2, int multiplier3, float module, float scale1, float scale2, Vector3 position, Material outline, Color color)
    {

        var deltaA = boundingXZ * 4;
        var deltaB = boundingXZ * 8;

        var point = new Vector3((module * multiplier1)- (boundingXZ / 2.0f) , 0.0f, (module * multiplier2) - (boundingXZ / 2.0f));
        var pos = point + new Vector3(0.0f, (multiplier3 * multiplierSpacing / 2) + _voxelSize * 1.0f, 0.0f);
        var geo = (GameObject.CreatePrimitive(PrimitiveType.Cube));
        geo.AddComponent<MeshCollider>();
        geo.GetComponent<BoxCollider>().enabled = false;
        geo.GetComponent<MeshRenderer>().material = outline;
        geo.GetComponent<MeshRenderer>().material.color = color;
        geo.transform.position = pos;

        var scale = geo.transform.localScale;
        var newScale = scale + new Vector3(scale1*2 -1 * multiplierSpacing, multiplier3,  scale2*2 -1 * multiplierSpacing);
        geo.transform.localScale = newScale;
        geo.isStatic = true;        

        return geo;

    }

}
