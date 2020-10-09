using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class generator  {


    public static Texture2D ColorMapToTexture(Color[] colMap,int width, int height)
    {
        Texture2D t = new Texture2D(width, height);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.SetPixels(colMap);
        t.Apply();
        return t;
    }

    public static Texture2D HeightMapToTexture(float[,] n, int width, int height)
    {

        Color[] colMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colMap[y * width + x] = Color.Lerp(Color.black, Color.white, n[x, y]);
            }
        }
        
        return ColorMapToTexture(colMap,width,height);
    }
}
