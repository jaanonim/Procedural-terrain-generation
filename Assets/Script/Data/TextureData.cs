using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdateData
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public Layer[] layers;

    float saveMinH;
    float saveMaxH;

    

    public void ApplyToMaterial(Material material, float scale)
    {

        material.SetInt("Count", layers.Length);
        
        material.SetColorArray("color", layers.Select(x=>x.tint).ToArray());
        material.SetFloatArray("startHeight", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("blendStrength", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("colorStrength", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("textureScale", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texture2DArray = GenerateTextutreArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTexture", texture2DArray);
        

        UpdateMeshH(material, saveMinH, saveMaxH,scale);
    }

    public void UpdateMeshH(Material material, float min, float max,float scale)
    {
        saveMinH = min;
        saveMaxH = max;

        material.SetFloat("worldScale", scale);
        material.SetFloat("minH",min);
        material.SetFloat("maxH",max);
    }

    Texture2DArray GenerateTextutreArray(Texture2D[] t)
    {
        Texture2DArray tArray = new Texture2DArray(textureSize, textureSize, t.Length, textureFormat, true);
        for(int i = 0;i<t.Length;i++)
        {
            tArray.SetPixels(t[i].GetPixels(), i);
        }
        tArray.Apply();
        return tArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0,1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;
    }
}
