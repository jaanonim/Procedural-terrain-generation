using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class draw : MonoBehaviour {

    [Header("Render data")]
    public Renderer render;
    public MeshFilter meshFilter;
    //public MeshRenderer meshRenderer;

    public void DrawMesh(MechData m,MeshCollider meshCollider)
    {
        Mesh mesh = m.CreateMesh();
        meshFilter.sharedMesh = mesh;
        meshFilter.transform.localScale = Vector3.one * FindObjectOfType<display>().meshSettings.uniformScale;

        //meshCollider.sharedMesh = mesh;

    }

    public void Draw(Texture2D t)
    {
        render.sharedMaterial.mainTexture = t;
        render.transform.localScale = new Vector3(t.width, 1, t.height);
    }
}
