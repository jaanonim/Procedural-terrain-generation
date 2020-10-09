using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class hightMapGenerator
{
    public static HightMap GenHightMap(MeshSettings meshSettings ,HightMapSettings settings, Vector2 center)
    {
        AnimationCurve animationCurve_t = new AnimationCurve(settings.animationCurve.keys);

        float minV = float.MaxValue;
        float maxV = float.MinValue;

        float[,] falloffMap = null;

        if (settings.useFalloff)
        {
            if (falloffMap == null)
            {
                falloffMap = fallofGenerator.GenerateFalloff(meshSettings.numberVerticisPerLine + 2, settings.FalloffBlur, settings.FalloffSize);
            }
        }

        float[,] valuse = PerlinNoise.GeneratorNoise(meshSettings.numberVerticisPerLine + 2, meshSettings.numberVerticisPerLine + 2, settings.noiseSettings,center);
        
        //Erode
        valuse = HightMap.ConwertTabBack(Erosion.Erode(HightMap.ConwertTab(valuse, meshSettings.numberVerticisPerLine + 2), settings, meshSettings, settings.erosionShader, 1), meshSettings.numberVerticisPerLine + 2);



        for (int i = 0; i < meshSettings.numberVerticisPerLine + 2; i++)
        {
            for (int j = 0; j < meshSettings.numberVerticisPerLine + 2; j++)
            {
                if (settings.useFalloff)
                {
                    valuse[i, j] = Mathf.Clamp(valuse[i, j] - falloffMap[i, j], 0, 1) * animationCurve_t.Evaluate(valuse[i, j]) * settings.heightMultiplayer;
                }
                else
                {
                    valuse[i, j] *= animationCurve_t.Evaluate(valuse[i, j]) * settings.heightMultiplayer;
                }
                if (valuse[i, j] > maxV)
                {
                    maxV = valuse[i, j];
                }
                if (valuse[i, j] < minV)
                {
                    minV = valuse[i, j];
                }
            }
        }



        return new HightMap(valuse,minV,maxV);
    }
}

public class HightMap
{
    public readonly float[,] value;
    public readonly float minV;
    public readonly float maxV;

    public HightMap(float[,] value,float minV,float maxV)
    {
        this.value = value;
        this.minV = minV;
        this.maxV = maxV;
    }

    public static float[] ConwertTab(float[,] tab, int len)
    {
        float[] tablica = new float[len * len];
        int k = 0;
        for (int i = 0; i < len; i++)
        {
            for (int j = 0; j < len; j++)
            {
                tablica[k] = tab[i, j];
                k++;
            }
        }
        return tablica;
    }

    public static float[,] ConwertTabBack(float[] tab, int len)
    {
        float[,] tablica = new float[len, len];
        int k = 0;
        for (int i = 0; i < len; i++)
        {
            for (int j = 0; j < len; j++)
            {
                tablica[i, j] = tab[k];
                k++;
            }
        }
        return tablica;
    }
}