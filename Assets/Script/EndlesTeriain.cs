using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlesTeriain : MonoBehaviour
{
   
    public static float scale = 2f;

    const float playerMoveTresholdForChunkUpdate = 25f;
    const float sqrPlayerMoveTresholdForChunkUpdate = playerMoveTresholdForChunkUpdate * playerMoveTresholdForChunkUpdate;
    const float colliderGenerationThreshold = 2;


    public int colliderMeshIndex;
    public static float maxViveDst;
    public static Vector2 playerPos;

    public Transform player;
    public LODInfo[] detail;

    MeshSettings mSettings;
    HightMapSettings hSettings;
    Material material;

    Vector2 oldPlayerPos;
    static display display;

    float meshWorldSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> TerrainChunksVible = new List<TerrainChunk>();

    private void Start()
    {
        display = FindObjectOfType<display>() as display;

        mSettings = display.meshSettings;
        hSettings = display.hightMapSettings;
        material = display.terrainMateial;


        maxViveDst = detail[detail.Length - 1].visibleDistance;
        meshWorldSize = mSettings.mechWorldSize;
        chunksVisible = Mathf.RoundToInt(maxViveDst / meshWorldSize);
        if (display.IsEndlessMode)
        {
            UpdateVisbleChunk();
        }
    }

    private void Update()
    {
        if (display.IsEndlessMode)
        {
            playerPos = new Vector2(player.position.x, player.position.z);

            if(playerPos != oldPlayerPos)
            {
                foreach(TerrainChunk chunck in TerrainChunksVible)
                {
                    chunck.UpdateColissionMesh();
                }
            }

            if ((oldPlayerPos - playerPos).sqrMagnitude > sqrPlayerMoveTresholdForChunkUpdate)
            {
                oldPlayerPos = playerPos;
                UpdateVisbleChunk();
            }
        }
    }

    void UpdateVisbleChunk()
    {
        HashSet<Vector2> updatedChunkCoord = new HashSet<Vector2>();
        for (int i = TerrainChunksVible.Count-1; i >=0 ; i--)
        {
            updatedChunkCoord.Add(TerrainChunksVible[i].coord);
            TerrainChunksVible[i].UpdateChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(playerPos.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(playerPos.y / meshWorldSize);

        for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
        {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
            {
                Vector2 pChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!updatedChunkCoord.Contains(pChunkCoord))
                {
                    if (chunkDictionary.ContainsKey(pChunkCoord))
                    {
                        chunkDictionary[pChunkCoord].UpdateChunk();
                    }
                    else
                    {
                        chunkDictionary.Add(pChunkCoord, new TerrainChunk(pChunkCoord, hSettings, mSettings, meshWorldSize, detail, colliderMeshIndex, transform, material));
                    }
                }
            }
        }
    }
   
    public class TerrainChunk
    {
        public Vector2 coord;

        GameObject meshObj;
        Vector2 position;
        Bounds bounds;

        HightMap mapH;
        bool hmapRecive;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        HightMapSettings hightMapSettings;
        MeshSettings meshSettings;
        LODInfo[] lodInfo;
        LODMesh[] lodMeshes;
        int colliderMeshIndex;
        bool hasSetCollider=false;

        int prevLODindex = -1;


        public TerrainChunk(Vector2 coord, HightMapSettings hightMapSettings, MeshSettings meshSettings ,float size, LODInfo[] lodInfo,int colliderMeshIndex, Transform parent, Material material)
        {
            this.coord = coord;
            this.hightMapSettings = hightMapSettings;
            this.meshSettings = meshSettings;
            this.lodInfo = lodInfo;
            this.colliderMeshIndex = colliderMeshIndex;


            
            position = coord *size/meshSettings.uniformScale;
            Vector2 center = coord * size;
            bounds = new Bounds(position, Vector2.one * size);

            Vector3 position3D = new Vector3(position.x, 0, position.y);

            meshObj = new GameObject("Chunk");
            meshRenderer = meshObj.AddComponent<MeshRenderer>();
            meshFilter = meshObj.AddComponent<MeshFilter>();
            meshCollider = meshObj.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObj.transform.position = position3D*scale;
            meshObj.transform.parent = parent;
            meshObj.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            lodMeshes = new LODMesh[lodInfo.Length];
            for (int i = 0; i < lodInfo.Length; i++)
            {
                lodMeshes[i] = new LODMesh(lodInfo[i].lod);
                lodMeshes[i].updateCalback += UpdateChunk;
                if(i==colliderMeshIndex)
                {
                    lodMeshes[i].updateCalback += UpdateColissionMesh;
                }
            }

            TreadedDataRequest.RequestData(()=>hightMapGenerator.GenHightMap(meshSettings, hightMapSettings,center),OnMapDataRecive);
        }


        void OnMapDataRecive(object mapO)
        {
            mapH = (HightMap)mapO;
            hmapRecive = true;

            UpdateChunk();
        }

        public void UpdateChunk()
        {

            if (hmapRecive)
            {
                float playerDstToEdge = Mathf.Sqrt(bounds.SqrDistance(playerPos));
                bool visibe = playerDstToEdge <= maxViveDst;
                bool wasVisible = isVisble();

                if (visibe)
                {
                    int LODIndex = 0;
                    for (int i = 0; i < lodInfo.Length - 1; i++)
                    {
                        if (playerDstToEdge > lodInfo[i].visibleDistance)
                        {
                            LODIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }

                    }

                    if (LODIndex != prevLODindex)
                    {
                        LODMesh lodMesh = lodMeshes[LODIndex];
                        if (lodMesh.hasMesh)
                        {
                            prevLODindex = LODIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequsetMesh)
                        {
                            lodMesh.RequsetMesh(mapH, meshSettings);
                        }
                    }
                    
                    TerrainChunksVible.Add(this);
                    
                }

                if(wasVisible!=visibe)
                {
                    if(visibe)
                    {
                        TerrainChunksVible.Add(this);
                    }
                    else
                    {
                        TerrainChunksVible.Remove(this);
                    }
                    SetVisible(visibe);
                }
            }
        }

        public void UpdateColissionMesh()
        {
            if (!hasSetCollider)
            {
                float sqrDistansFromPlayerToEdge = bounds.SqrDistance(playerPos);

                if (sqrDistansFromPlayerToEdge < lodInfo[colliderMeshIndex].sqrVibeleDistanceTreshold)
                {
                    if (!lodMeshes[colliderMeshIndex].hasRequsetMesh)
                    {
                        lodMeshes[colliderMeshIndex].RequsetMesh(mapH, meshSettings);
                    }
                }

                if (sqrDistansFromPlayerToEdge < colliderGenerationThreshold * colliderGenerationThreshold)
                {
                    if (lodMeshes[colliderMeshIndex].hasMesh)
                    {
                        meshCollider.sharedMesh = lodMeshes[colliderMeshIndex].mesh;
                        hasSetCollider = true;
                    }
                }
            }
        }

        public void SetVisible(bool visible)
        {
            meshObj.SetActive(visible);
        }

        public bool isVisble()
        {
            return meshObj.activeSelf;
        }

        
    }

    
    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequsetMesh;
        public bool hasMesh;
        int lod;
        public event System.Action updateCalback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        void OnMeshDataRecive(object mechDataO)
        {
            mesh = ((MechData)mechDataO).CreateMesh();
            hasMesh = true;

            updateCalback();
        }

        public void RequsetMesh(HightMap map, MeshSettings mSettings)
        {
            hasRequsetMesh = true;
            
            TreadedDataRequest.RequestData(()=>mechDraw.GenerateMech(map.value, mSettings, lod), OnMeshDataRecive);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        
        [Range(0, MeshSettings.numberOfSuportedLODs - 1)]
        public int lod;
        public float visibleDistance;

        public float sqrVibeleDistanceTreshold
        {
            get
            {
                return visibleDistance*visibleDistance;
            }
        }
    }

}
