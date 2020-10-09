using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Analytics;

public class EnvironmentGenerator : MonoBehaviour
{
    public enum PositioningMode {Calculation, Colision,BothMode};
    [Header("Environment")]
    public PositioningMode positioningMode;

    public Transform parent;
    [Range(1,50)]
    public int skip;

    PanelProcess panelProcess;

    
    private void Start()
    {
        panelProcess = gameObject.GetComponent<PanelProcess>() as PanelProcess;
    }

    public IEnumerator GenerateEnvironment(HightMap heightMap ,MechData meshData, MeshSettings meshSettings,HightMapSettings hightMapSettings,EnvironmentData environmentData, int mapChunkSize, float scale, float min , float max)
    {
        int seed;
        if(environmentData.usingHeightMapSeed)
        {
            seed = hightMapSettings.noiseSettings.seed;
        }
        else
        {
            seed = environmentData.seed;
        }

        System.Random prng = new System.Random(seed);


        bool useFlatSheadedB = false;
        int flatSheadedChunkSizeIndexB=0;
        if (meshSettings.useFlatShader)
        {
            flatSheadedChunkSizeIndexB = meshSettings.chunkSizeIndex;
            useFlatSheadedB = true;
            meshSettings.chunkSizeIndex = meshSettings.flatSheadedChunkSizeIndex;
            meshSettings.useFlatShader = false;
        }

        for (int i = 0; i < environmentData.types.Length; i++)
        {
            panelProcess.setNumer( i+1+"/"+ environmentData.types.Length);
            panelProcess.setLabel("Environment ");

            EnvironmentData.Types now = environmentData.types[i];
            float[,] noise = PerlinNoise.GeneratorNoise(mapChunkSize + 2, mapChunkSize + 2, now.noiseData.noiseSettings,Vector2.zero);
           
            float[] noisMapEnd = HightMap.ConwertTab(noise, mapChunkSize + 2);
            int msi = (now.LOD == 0) ? 1 : now.LOD * 2;
            Vector3 lastPos = Vector3.zero;
            int len = ((int)(mapChunkSize + 2) / msi) + 1;
            len = len * len;
           
            Vector3[] points = mechDraw.GenerateMech(heightMap.value, meshSettings, now.LOD).vertices;
            Vector3[] orginalVerticis = meshData.vertices;
            
            for (int j = 0; j < len; j++)
            {
                Vector3 nowPos = Vector3.zero;
                if((positioningMode==PositioningMode.Calculation)||(positioningMode==PositioningMode.BothMode))
                {
                    nowPos = orginalVerticis[CalculationPos(points,orginalVerticis,j,len)];
                }
                else
                {
                    nowPos = points[j];
                }
                 

                panelProcess.setValue((float)j / len);

                float wynik = map(min, max, 0, 1, nowPos.y);

               
                //if (true)
                if (noisMapEnd[j] < now.curve.Evaluate(wynik))
                {
                    if (lastPos != nowPos)
                    {
                        Vector3 randPos = new Vector3(prng.Next(-now.LOD*environmentData.prngPrecision, now.LOD * environmentData.prngPrecision) / environmentData.prngPrecision, 0, prng.Next(-now.LOD * environmentData.prngPrecision, now.LOD * environmentData.prngPrecision) / environmentData.prngPrecision) * now.randPos;

                        float x = 0, y = 0, z = 0;
                        if (now.randRotX)
                        {
                            x = prng.Next(0, 359);
                        }
                        if (now.randRotY)
                        {
                            y = prng.Next(0, 359);
                        }
                        if (now.randRotZ)
                        {
                            z = prng.Next(0, 359);
                        }
                        if((positioningMode == PositioningMode.Colision)|| (positioningMode == PositioningMode.BothMode))
                        {
                            nowPos.y = ColidePos(nowPos.x + randPos.x, nowPos.z + randPos.z, heightMap.minV, heightMap.maxV) - randPos.y;
                        }
                        Vector3 randRot = new Vector3(x, y, z);
                        lastPos = nowPos;

                        GameObject o = Instantiate(now.Object, (nowPos + randPos) * scale, Quaternion.Euler(now.rotation + randRot));

                        Transform tObject = o.GetComponent<Transform>() as Transform;
                        tObject.SetParent(parent, true);
                        tObject.localScale = now.scale;

                        if(j%skip == 0)
                        {
                            yield return null;
                        }
                    }

                }
                
            }
        }
        yield return null;
        if (useFlatSheadedB)
        {
            meshSettings.chunkSizeIndex = flatSheadedChunkSizeIndexB;
            meshSettings.useFlatShader = true;
        }
        panelProcess.setActive(false);
    }

    float map(float in_min, float in_max, float out_min, float out_max, float x)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    int CalculationPos(Vector3[] points, Vector3[] orginalVerticis, int nr, int len)
    {
        float nowMinDis = Mathf.Abs(points[nr].z - orginalVerticis[0].z);
        int zMinIndex = 0;

        for (int a = 0; a < orginalVerticis.Length; a += len)
        {
            float d = Mathf.Abs(points[nr].z - orginalVerticis[a].z);
            if (d < nowMinDis)
            {
                nowMinDis = d;
                zMinIndex = a;
            }
        }


        nowMinDis = Mathf.Abs(points[nr].x - orginalVerticis[zMinIndex].x);
        int xMinIndex = zMinIndex;
        for (int a = zMinIndex; a < zMinIndex + len; a++)
        {
            if (a > orginalVerticis.Length - 1)
            {
                break;
            }
            float d = Mathf.Abs(points[nr].x - orginalVerticis[a].x);
            if (d < nowMinDis)
            {
                nowMinDis = d;
                xMinIndex = a;
            }
        }

        return xMinIndex;
    }

    float ColidePos(float x, float z, float minH, float maxH)
    {
        float lenght = Mathf.Abs(maxH - minH) + 5;

        RaycastHit hit;
        Physics.Raycast(new Vector3(x,lenght+minH,z), Vector3.down, out hit, lenght);
        
        return hit.point.y;
    }
}




