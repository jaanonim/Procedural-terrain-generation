using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class display : MonoBehaviour
{
    public enum DraweMode { NoiseMap, Mesh, FallMap, MeshAndEnvironment, EndlesTerrain};
    public bool autoUpdate;
    [Space]
    public DraweMode draweMode;

    [Header("Data")]
    public MeshSettings meshSettings;
    public HightMapSettings hightMapSettings;
    public TextureData textureData;
    public EnvironmentData environmentData;

    [Header("Other Setting")]
    public Material terrainMateial;
    public MeshCollider meshColider;

    [Range(0, MeshSettings.numberOfSuportedLODs - 1)]
    public int levelOfDetail;

    [Header("Targets")]
    public GameObject Mesh;
    public GameObject Plane;

   

    [HideInInspector]
    public bool IsEndlessMode;

    PanelProcess panelProcess;
    EnvironmentGenerator eGen;

    private void Start()
    {
        panelProcess = gameObject.GetComponent<PanelProcess>() as PanelProcess;
        eGen = gameObject.GetComponent<EnvironmentGenerator>() as EnvironmentGenerator;

        textureData.ApplyToMaterial(terrainMateial, meshSettings.uniformScale);
        textureData.UpdateMeshH(terrainMateial, hightMapSettings.minH, hightMapSettings.maxH, meshSettings.uniformScale);

        if (draweMode == DraweMode.MeshAndEnvironment)
        {
            panelProcess.setActive(true);
        }
        StartCoroutine(startGenIn());
        panelProcess.setNumer("1/1");
    }

    IEnumerator startGenIn()
    {
        yield return new WaitForSeconds(0.1f);
        panelProcess.setValue(1);
        IniGenerate();
        
    }

    void OnValuesUpdate()
    {
        if (!Application.isPlaying)
        { 
            IniGenerate();
        }
    }

    void OnTextureValuesUpdate()
    {
        textureData.ApplyToMaterial(terrainMateial, meshSettings.uniformScale);
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdate -= OnValuesUpdate;
            meshSettings.OnValuesUpdate += OnValuesUpdate;
        }
        if (hightMapSettings != null)
        {
            hightMapSettings.OnValuesUpdate -= OnValuesUpdate;
            hightMapSettings.OnValuesUpdate += OnValuesUpdate;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdate -= OnTextureValuesUpdate;
            textureData.OnValuesUpdate += OnTextureValuesUpdate;
        }
        if (environmentData != null)
        {
            environmentData.OnValuesUpdate -= OnValuesUpdate;
            environmentData.OnValuesUpdate += OnValuesUpdate;
        }
    }

    public void IniGenerate()
    {
        try
        {
            IsEndlessMode = false;
            textureData.ApplyToMaterial(terrainMateial, meshSettings.uniformScale);
            textureData.UpdateMeshH(terrainMateial, hightMapSettings.minH, hightMapSettings.maxH, meshSettings.uniformScale);

            HightMap heightMap = hightMapGenerator.GenHightMap(meshSettings, hightMapSettings, Vector2.zero);


            draw draw = FindObjectOfType<draw>() as draw;

            if (draweMode == DraweMode.NoiseMap)
            {
                Mesh.SetActive(false);
                Plane.SetActive(true);
                draw.Draw(generator.HeightMapToTexture(heightMap.value, meshSettings.numberVerticisPerLine, meshSettings.numberVerticisPerLine));
                panelProcess.setActive(false);
            }
            else if (draweMode == DraweMode.FallMap)
            {
                Mesh.SetActive(false);
                Plane.SetActive(true);
                draw.Draw(generator.HeightMapToTexture(fallofGenerator.GenerateFalloff(meshSettings.numberVerticisPerLine, hightMapSettings.FalloffBlur, hightMapSettings.FalloffSize), meshSettings.numberVerticisPerLine, meshSettings.numberVerticisPerLine));
                panelProcess.setActive(false);

            }
            else if (draweMode == DraweMode.Mesh)
            {
                Mesh.SetActive(true);
                Plane.SetActive(false);
                draw.DrawMesh(mechDraw.GenerateMech(heightMap.value, meshSettings, levelOfDetail), meshColider);
                panelProcess.setActive(false);
            }
            else if (draweMode == DraweMode.MeshAndEnvironment)
            {
                panelProcess.setLabel("Terrain");
                panelProcess.setValue(0);
                Mesh.SetActive(true);
                Plane.SetActive(false);
                MechData newMesh = mechDraw.GenerateMech(heightMap.value, meshSettings, levelOfDetail);
                draw.DrawMesh(newMesh, meshColider);

                if (!Application.isPlaying)
                {
                    //draweMode = DraweMode.Mesh;
                    Debug.LogWarning("Generate Environment don't work in Editor!");
                    panelProcess.setActive(false);
                }
                else
                {
                    StartCoroutine(eGen.GenerateEnvironment(heightMap, newMesh, meshSettings,hightMapSettings,environmentData, meshSettings.numberVerticisPerLine, meshSettings.uniformScale, hightMapSettings.minH, hightMapSettings.maxH));
                }

            }
            else if (draweMode == DraweMode.EndlesTerrain)
            {
                IsEndlessMode = true;
                Mesh.SetActive(false);
                Plane.SetActive(false);
                if (!Application.isPlaying)
                {
                    //draweMode = DraweMode.Mesh;
                    Debug.LogWarning("Generate endles terrain don't work in Editor!");
                }
                panelProcess.setActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Exeption: "+ e.GetType().ToString());
        }
    }    
}
