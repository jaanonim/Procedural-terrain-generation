using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class mechDraw
{
    public static MechData GenerateMech(float[,] n, MeshSettings settings, int levelOfDetail)
    { 
        //Mesh Simplification Increment
        int msi = (levelOfDetail == 0)?1:levelOfDetail * 2;

        int borderSize = n.GetLength(0);
        int meshSize = borderSize - 2*msi;
        int meshSizeNoS = borderSize - 2;

        float topLeftX = (meshSizeNoS - 1) / -2f;
        float topLeftZ = (meshSizeNoS - 1) / 2f;
       
        int verticesPerLine = (meshSize - 1) / msi + 1;

        MechData mechD = new MechData(borderSize, settings.useFlatShader);

        int[,] vIndexMap = new int[borderSize,borderSize];
        int vMeshIndex = 0;
        int vBorderIndex =-1;

        for (int y = 0; y < borderSize; y += msi)
        {
            for (int x = 0; x < borderSize; x += msi)
            {
                bool isBorderV = y == 0 || y == borderSize - 1 || x == 0 || x == borderSize - 1;
                if(isBorderV)
                {
                    vIndexMap[x, y] = vBorderIndex;
                    vBorderIndex--;
                }
                else
                {
                    vIndexMap[x, y] = vMeshIndex;
                    vMeshIndex++;
                }
            }
        }

        for (int y = 0; y < borderSize; y+=msi)
        {
            for (int x = 0; x < borderSize; x+=msi)
            {
                int vIndex = vIndexMap[x, y];
                Vector2 percent = new Vector2((x-msi) / (float)meshSize, (y-msi) / (float)meshSize);
                float height = n[x, y];
                Vector3 vPosition = new Vector3((topLeftX+percent.x* meshSizeNoS)* settings.uniformScale , height , (topLeftZ- percent.y * meshSizeNoS) * settings.uniformScale);

                mechD.AddVertex(vPosition, percent, vIndex);

                if(x<borderSize-1&&y<borderSize-1)
                {
                    int a = vIndexMap[x, y];
                    int b = vIndexMap[x+msi, y];
                    int c = vIndexMap[x, y+msi];
                    int d = vIndexMap[x+msi, y+msi];
                    mechD.AddTringele(a,d,c);
                    mechD.AddTringele(d,a,b);
                    
                }

                vIndex++;
            }
        }

        mechD.ProcessMesh();

        return mechD;
    }
}

public class MechData
{
    public Vector3[] vertices;
    int[] triangels;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] borderV;
    int[] borderT;

    int tIndex;
    int tBorderIndex;

    bool useFlatShader;

    public MechData(int verticesesPerLine, bool useFlatShader)
    {
        this.useFlatShader = useFlatShader;
        vertices = new Vector3[verticesesPerLine * verticesesPerLine];
        uvs = new Vector2[verticesesPerLine * verticesesPerLine];
        triangels = new int[(verticesesPerLine - 1) * (verticesesPerLine - 1) * 6];

        borderV = new Vector3[verticesesPerLine * 4 + 4];
        borderT = new int[24 * verticesesPerLine];
    }

    public void AddVertex(Vector3 vPosition, Vector2 uv,int vIndex)
    {
        if(vIndex<0)
        {
            borderV[-vIndex - 1] = vPosition;
        }
        else
        {
            vertices[vIndex] = vPosition;
            uvs[vIndex] = uv;
        }
    }

    public void AddTringele(int a, int b, int c)
    {
        if(a<0||b<0||c<0)
        {
            borderT[tBorderIndex] = a;
            borderT[tBorderIndex + 1] = b;
            borderT[tBorderIndex + 2] = c;
            tBorderIndex += 3;
        }
        else
        {
            triangels[tIndex] = a;
            triangels[tIndex + 1] = b;
            triangels[tIndex + 2] = c;
            tIndex += 3;
        }
        
    }

    Vector3 [] CalculateNormals()
    {
        Vector3[] normals = new Vector3[vertices.Length];
        int triangleC = triangels.Length/3;
        for(int i = 0;i<triangleC;i++)
        {
            //normal triangele index
            int NTI = i * 3;
            int vertexIndrxA = triangels[NTI];
            int vertexIndrxB = triangels[NTI+1];
            int vertexIndrxC = triangels[NTI+2];

            Vector3 traingleN = NormalFromIndeces(vertexIndrxA,vertexIndrxB,vertexIndrxC);
            normals[vertexIndrxA] += traingleN;
            normals[vertexIndrxB] += traingleN;
            normals[vertexIndrxC] += traingleN;

        }

        int triangleBorderC = borderT.Length/3;
        for(int i = 0;i<triangleBorderC;i++)
        {
            //normal triangele index
            int NTI = i * 3;
            int vertexIndrxA = borderT[NTI];
            int vertexIndrxB = borderT[NTI+1];
            int vertexIndrxC = borderT[NTI+2];

            Vector3 traingleN = NormalFromIndeces(vertexIndrxA,vertexIndrxB,vertexIndrxC);
            if(vertexIndrxA>=0)
            {
                normals[vertexIndrxA] += traingleN;
            }
            if (vertexIndrxB>=0)
            {
                normals[vertexIndrxB] += traingleN;
            }
            if (vertexIndrxC>=0)
            {
                normals[vertexIndrxC] += traingleN;
            }

        }

        for(int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }

        return normals;
    }

    Vector3 NormalFromIndeces(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderV[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderV[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderV[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB-pointA;
        Vector3 sideAC = pointC-pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;

    }

    public void ProcessMesh()
    {
        if(useFlatShader)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }

    void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedV = new Vector3[triangels.Length];
        Vector2[] flatShadedUvs = new Vector2[triangels.Length];

        for(int i=0;i<triangels.Length;i++)
        {
            flatShadedV[i] = vertices[triangels[i]];
            flatShadedUvs[i] = uvs[triangels[i]];
            triangels[i] = i;
        }

        vertices = flatShadedV;
        uvs = flatShadedUvs;

    }

    public Mesh CreateMesh()
    {
        Mesh m = new Mesh();

        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        m.vertices = vertices;
        m.triangles = triangels;

        m.uv = uvs;

        if(useFlatShader)
        {
            m.RecalculateNormals();
        }
        else
        {
            m.normals = bakedNormals;
        }

        return m;
    }
}
