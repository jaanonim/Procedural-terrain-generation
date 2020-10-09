using UnityEngine;
using System.Collections.Generic;

public static class Erosion
{
    public static float[] Erode(float[] values, HightMapSettings hightMapSettings, MeshSettings meshSettings, ComputeShader erosionShader, int seed)
    {
        if(hightMapSettings.erossionSettings.numberOfIterationsMode == ErossionSettings.NumberOfIterationsMode.perSurface)
        {
            hightMapSettings.erossionSettings.numErosionIterations = Mathf.RoundToInt(hightMapSettings.erossionSettings.numberOfIterations * meshSettings.numberVerticisPerLine * meshSettings.numberVerticisPerLine);
        }
        else
        {
            hightMapSettings.erossionSettings.numErosionIterations = Mathf.RoundToInt(hightMapSettings.erossionSettings.numberOfIterations);
        }

        ErossionSettings.ErosionMode erosionMode = hightMapSettings.erossionSettings.erosionMode;
        if (erosionMode == ErossionSettings.ErosionMode.CPU)
        {
            return ErodeCPU(values, meshSettings.numberVerticisPerLine, hightMapSettings.erossionSettings, seed);
        }
        else if (erosionMode == ErossionSettings.ErosionMode.GPU)
        {
            return ErodeGPU(values, meshSettings.numberVerticisPerLine, hightMapSettings.erossionSettings, erosionShader);
        }
        else
        {
            return values;
        }
       
    }

    //----GPU--------------------------
    public static float[] ErodeGPU(float[] map, int mapSize, ErossionSettings erossionSettings, ComputeShader erosion)
    {
        int mapSizeWithBorder = mapSize + erossionSettings.erosionBrushRadius * 2;
        int numThreads = erossionSettings.numErosionIterations / 1024;

        // Create brush
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -erossionSettings.erosionBrushRadius; brushY <= erossionSettings.erosionBrushRadius; brushY++)
        {
            for (int brushX = -erossionSettings.erosionBrushRadius; brushX <= erossionSettings.erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erossionSettings.erosionBrushRadius * erossionSettings.erosionBrushRadius)
                {
                    brushIndexOffsets.Add(brushY * mapSize + brushX);
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erossionSettings.erosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        // Generate random indices for droplet placement
        int[] randomIndices = new int[erossionSettings.numErosionIterations];
        for (int i = 0; i < erossionSettings.numErosionIterations; i++)
        {
            int randomX = Random.Range(erossionSettings.erosionBrushRadius, mapSize + erossionSettings.erosionBrushRadius);
            int randomY = Random.Range(erossionSettings.erosionBrushRadius, mapSize + erossionSettings.erosionBrushRadius);
            randomIndices[i] = randomY * mapSize + randomX;
        }

        // Send random indices to compute shader
        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

        // Heightmap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
        mapBuffer.SetData(map);
        erosion.SetBuffer(0, "map", mapBuffer);

        // Settings
        erosion.SetInt("borderSize", erossionSettings.erosionBrushRadius);
        erosion.SetInt("mapSize", mapSizeWithBorder);
        erosion.SetInt("brushLength", brushIndexOffsets.Count);
        erosion.SetInt("maxLifetime", erossionSettings.maxLifetime);
        erosion.SetFloat("inertia", erossionSettings.inertia);
        erosion.SetFloat("sedimentCapacityFactor", erossionSettings.sedimentCapacityFactor);
        erosion.SetFloat("minSedimentCapacity", erossionSettings.minSedimentCapacity);
        erosion.SetFloat("depositSpeed", erossionSettings.depositSpeed);
        erosion.SetFloat("erodeSpeed", erossionSettings.erodeSpeed);
        erosion.SetFloat("evaporateSpeed", erossionSettings.evaporateSpeed);
        erosion.SetFloat("gravity", erossionSettings.gravity);
        erosion.SetFloat("startSpeed", erossionSettings.startSpeed);
        erosion.SetFloat("startWater", erossionSettings.startWater);

        // Run compute shader
        erosion.Dispatch(0, numThreads, 1, 1);
        mapBuffer.GetData(map);

        // Release buffers
        mapBuffer.Release();
        randomIndexBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();

        return map;
    }

    //----CPU--------------------------

    static int[][] erosionBrushIndices;
    static float[][] erosionBrushWeights;
    static System.Random prng;

    static int currentSeed;
    static int currentErosionRadius;
    static int currentMapSize;

    public static float[] ErodeCPU (float[] map, int mapSize, ErossionSettings erossionSettings, int seed)
    {
        bool resetSeed = false;

        Initialize (mapSize, resetSeed, erossionSettings, seed);

        for (int iteration = 0; iteration < erossionSettings.numErosionIterations; iteration++)
        {
            // Create water droplet at random point on map
            float posX = prng.Next (0, mapSize - 1);
            float posY = prng.Next (0, mapSize - 1);
            float dirX = 0;
            float dirY = 0;
            float speed = erossionSettings.startSpeed;
            float water = erossionSettings.startWater;
            float sediment = 0;

            for (int lifetime = 0; lifetime < erossionSettings.maxLifetime; lifetime++)
            {
                int nodeX = (int) posX;
                int nodeY = (int) posY;
                int dropletIndex = nodeY * mapSize + nodeX;
                // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                HeightAndGradient heightAndGradient = CalculateHeightAndGradient (map, mapSize, posX, posY);

                // Update the droplet's direction and position (move position 1 unit regardless of speed)
                dirX = (dirX * erossionSettings.inertia - heightAndGradient.gradientX * (1 - erossionSettings.inertia));
                dirY = (dirY * erossionSettings.inertia - heightAndGradient.gradientY * (1 - erossionSettings.inertia));
                // Normalize direction
                float len = Mathf.Sqrt (dirX * dirX + dirY * dirY);
                if (len != 0) {
                    dirX /= len;
                    dirY /= len;
                }
                posX += dirX;
                posY += dirY;

                // Stop simulating droplet if it's not moving or has flowed over edge of map
                if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= mapSize - 1 || posY < 0 || posY >= mapSize - 1) {
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                float newHeight = CalculateHeightAndGradient (map, mapSize, posX, posY).height;
                float deltaHeight = newHeight - heightAndGradient.height;

                // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                float sedimentCapacity = Mathf.Max (-deltaHeight * speed * water * erossionSettings.sedimentCapacityFactor, erossionSettings.minSedimentCapacity);

                // If carrying more sediment than capacity, or if flowing uphill:
                if (sediment > sedimentCapacity || deltaHeight > 0) {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 0) ? Mathf.Min (deltaHeight, sediment) : (sediment - sedimentCapacity) * erossionSettings.depositSpeed;
                    sediment -= amountToDeposit;

                    // Add the sediment to the four nodes of the current cell using bilinear interpolation
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    map[dropletIndex] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    map[dropletIndex + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    map[dropletIndex + mapSize] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    map[dropletIndex + mapSize + 1] += amountToDeposit * cellOffsetX * cellOffsetY;

                } else {
                    // Erode a fraction of the droplet's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    float amountToErode = Mathf.Min ((sedimentCapacity - sediment) * erossionSettings.erodeSpeed, -deltaHeight);

                    // Use erosion brush to erode from all nodes inside the droplet's erosion radius
                    for (int brushPointIndex = 0; brushPointIndex < erosionBrushIndices[dropletIndex].Length; brushPointIndex++) {
                        int nodeIndex = erosionBrushIndices[dropletIndex][brushPointIndex];
                        float weighedErodeAmount = amountToErode * erosionBrushWeights[dropletIndex][brushPointIndex];
                        float deltaSediment = (map[nodeIndex] < weighedErodeAmount) ? map[nodeIndex] : weighedErodeAmount;
                        map[nodeIndex] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }

                // Update droplet's speed and water content
                speed = Mathf.Sqrt (speed * speed + deltaHeight * erossionSettings.gravity);
                water *= (1 - erossionSettings.evaporateSpeed);
            }
        }
        return map;
    }

    // Initialization creates a System.Random object and precomputes indices and weights of erosion brush
    static void Initialize(int mapSize, bool resetSeed, ErossionSettings erossionSettings, int seed)
    {
        if (resetSeed || prng == null || currentSeed != seed)
        {
            prng = new System.Random(seed);
            currentSeed = seed;
        }

        if (erosionBrushIndices == null || currentErosionRadius != erossionSettings.erosionBrushRadius || currentMapSize != mapSize)
        {
            InitializeBrushIndices(mapSize, erossionSettings.erosionBrushRadius);
            currentErosionRadius = erossionSettings.erosionBrushRadius;
            currentMapSize = mapSize;
        }
    }

    static HeightAndGradient CalculateHeightAndGradient (float[] nodes, int mapSize, float posX, float posY)
    {
        int coordX = (int) posX;
        int coordY = (int) posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = nodes[nodeIndexNW];
        float heightNE = nodes[nodeIndexNW + 1];
        float heightSW = nodes[nodeIndexNW + mapSize];
        float heightSE = nodes[nodeIndexNW + mapSize + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient () { height = height, gradientX = gradientX, gradientY = gradientY };
    }

    static void InitializeBrushIndices (int mapSize, int radius)
    {
        erosionBrushIndices = new int[mapSize * mapSize][];
        erosionBrushWeights = new float[mapSize * mapSize][];

        int[] xOffsets = new int[radius * radius * 4];
        int[] yOffsets = new int[radius * radius * 4];
        float[] weights = new float[radius * radius * 4];
        float weightSum = 0;
        int addIndex = 0;

        for (int i = 0; i < erosionBrushIndices.GetLength (0); i++) {
            int centreX = i % mapSize;
            int centreY = i / mapSize;

            if (centreY <= radius || centreY >= mapSize - radius || centreX <= radius + 1 || centreX >= mapSize - radius) {
                weightSum = 0;
                addIndex = 0;
                for (int y = -radius; y <= radius; y++) {
                    for (int x = -radius; x <= radius; x++) {
                        float sqrDst = x * x + y * y;
                        if (sqrDst < radius * radius) {
                            int coordX = centreX + x;
                            int coordY = centreY + y;

                            if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize) {
                                float weight = 1 - Mathf.Sqrt (sqrDst) / radius;
                                weightSum += weight;
                                weights[addIndex] = weight;
                                xOffsets[addIndex] = x;
                                yOffsets[addIndex] = y;
                                addIndex++;
                            }
                        }
                    }
                }
            }

            int numEntries = addIndex;
            erosionBrushIndices[i] = new int[numEntries];
            erosionBrushWeights[i] = new float[numEntries];

            for (int j = 0; j < numEntries; j++) {
                erosionBrushIndices[i][j] = (yOffsets[j] + centreY) * mapSize + xOffsets[j] + centreX;
                erosionBrushWeights[i][j] = weights[j] / weightSum;
            }
        }
    }

    struct HeightAndGradient {
        public float height;
        public float gradientX;
        public float gradientY;
    }
}

[System.Serializable]
public class ErossionSettings 
{
    public enum ErosionMode { none, CPU, GPU }
    public enum NumberOfIterationsMode { perSurface, constant }

    public ErosionMode erosionMode;
    public NumberOfIterationsMode numberOfIterationsMode;
    public float numberOfIterations;
    [HideInInspector]
    public int numErosionIterations = 50000;
    public int erosionBrushRadius = 3;

    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;

    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    [Range(0, 1)]
    public float inertia = 0.3f;
}
