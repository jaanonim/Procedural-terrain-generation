using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PerlinNoise
{
    public enum NormalizeMode {Local,Global};

    public static float[,]GeneratorNoise(int w,int h,NoiseSettings settings,Vector2 center)
    {
        System.Random prng =new System.Random(settings.seed);

        Vector2[] octavesOffset = new Vector2[settings.octaves];

        float maxPosiblieH = 0;
        float amp = 1;
        float fre = 1;

        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000)+ settings.offset.x + center.x;
            float offsetY = prng.Next(-100000, 100000)- settings.offset.y - center.y;
            octavesOffset[i] = new Vector2(offsetX, offsetY);
            maxPosiblieH += amp;
            amp *= settings.persistance;
        }

        float[,] noiseMap = new float[w,h];

        float maxN = float.MinValue;
        float minN = float.MaxValue;

        float halfW = w / 2f;
        float halfH = h / 2f;

        for(int y=0; y<h;y++)
        {
            for (int x = 0; x < w; x++)
            {

                amp = 1;
                fre = 1;
                float noiseH = 0;


                for (int i = 0; i < settings.octaves; i++)
                {
                    float sX = (x - halfW + octavesOffset[i].x) * settings.scale * fre;
                    float sY = (y - halfH + octavesOffset[i].y) * settings.scale * fre;

                    float perlinValue = Mathf.PerlinNoise(sX, sY)*2-1;
                    noiseH += perlinValue * amp;

                    amp *= settings.persistance;
                    fre *= settings.lacunarity;
                }

                if (noiseH > maxN)
                {
                    maxN = noiseH;
                }

                if (noiseH < minN)
                {
                    minN = noiseH;
                }

                noiseMap[x, y] = noiseH;
            }
        }
        for(int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if(settings.normalizeMode==NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minN, maxN, noiseMap[x, y]);
                }
                else
                {
                    float normalizeH = (noiseMap[x, y] + 1) / (2*maxPosiblieH/settings.normalizeValue);
                    noiseMap[x, y] = Mathf.Clamp(normalizeH,0,int.MaxValue);
                }
            }
        }
        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings
{
    public PerlinNoise.NormalizeMode normalizeMode;
    public float scale;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;

    public Vector2 offset;
    public float normalizeValue=5;

    public void ValidateV()
    {
        scale = Mathf.Max(scale, 0.00001f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity,1);
        persistance = Mathf.Clamp(persistance,0, 1);
    }
}
