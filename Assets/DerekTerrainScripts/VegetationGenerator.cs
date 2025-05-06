using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class VegetationSettings
{
    public string name;
    public GameObject prefab;
    [Range(0f, 1f)]
    public float minDensity = 0.1f;  // Per 100x100 units
    [Range(0f, 1f)]
    public float maxDensity = 0.3f;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    [Range(0f, 1f)]
    public float minTemp = 0f;     // Temperature range where this vegetation can grow
    [Range(0f, 1f)]
    public float maxTemp = 1f;
    [Range(0f, 1f)]
    public float minMoisture = 0f; // Moisture range where this vegetation can grow
    [Range(0f, 1f)]
    public float maxMoisture = 1f;
    public bool usePhysics = true;   // Whether this vegetation needs colliders/rigidbody
}

[System.Serializable]
public class BiomeVegetation
{
    public string biomeName;
    [Header("Vegetation Lists")]
    public List<VegetationSettings> trees = new List<VegetationSettings>();
    public List<VegetationSettings> grains = new List<VegetationSettings>();
    public List<VegetationSettings> legumes = new List<VegetationSettings>();
    public List<VegetationSettings> wildPlants = new List<VegetationSettings>();

    [Header("Grass Settings")]
    public GameObject[] grassPrefabs;
    [Range(0f, 1f)]
    public float grassDensity = 0.5f;
}

public class VegetationGenerator : MonoBehaviour
{
    [Header("Biome Settings")]
    public BiomeSettings biomeSettings;  // Reference to your existing BiomeSettings asset
    public List<BiomeVegetation> biomeVegetation = new List<BiomeVegetation>();

    [Header("Generation Settings")]
    public float chunkSize = 100f;
    public int maxObjectsPerChunk = 1000;
    public Transform vegetationParent;

    [Header("Pool Settings")]
    public int poolSize = 5000;

    private Dictionary<Vector2Int, List<GameObject>> activeChunks = new Dictionary<Vector2Int, List<GameObject>>();
    private Queue<GameObject> objectPool;

    void OnValidate()
    {
        // Ensure biomeVegetation list matches BiomeSettings
        if (biomeSettings != null)
        {
            // Add any missing biomes
            while (biomeVegetation.Count < biomeSettings.biomeLayers.Length)
            {
                biomeVegetation.Add(new BiomeVegetation
                {
                    biomeName = biomeSettings.biomeLayers[biomeVegetation.Count].name
                });
            }
            // Remove extra biomes
            while (biomeVegetation.Count > biomeSettings.biomeLayers.Length)
            {
                biomeVegetation.RemoveAt(biomeVegetation.Count - 1);
            }
            // Update biome names
            for (int i = 0; i < biomeVegetation.Count; i++)
            {
                biomeVegetation[i].biomeName = biomeSettings.biomeLayers[i].name;
            }
        }
    }

    void Start()
    {
        InitializeObjectPool();
    }

    void InitializeObjectPool()
    {
        objectPool = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = new GameObject("PooledVegetation");
            obj.SetActive(false);
            obj.transform.parent = vegetationParent;
            objectPool.Enqueue(obj);
        }
    }

    public void GenerateVegetationForChunk(Vector2Int chunkCoord, float[,] heightMap, float[,] temperatureMap, float[,] moistureMap)
    {
        if (activeChunks.ContainsKey(chunkCoord))
            return;

        List<GameObject> chunkVegetation = new List<GameObject>();
        Vector2 worldPos = new Vector2(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize);

        // Sample points for vegetation placement
        for (int x = 0; x < heightMap.GetLength(0); x++)
        {
            for (int y = 0; y < heightMap.GetLength(1); y++)
            {
                float temp = temperatureMap[x, y];
                float moisture = moistureMap[x, y];

                BiomeVegetation biomePlants = GetBiomeVegetation(temp, moisture);
                if (biomePlants == null)
                    continue;

                TryPlaceVegetation(worldPos, x, y, heightMap[x, y], temp, moisture, biomePlants, chunkVegetation);
            }
        }

        activeChunks.Add(chunkCoord, chunkVegetation);
    }

    private void TryPlaceVegetation(Vector2 worldPos, int x, int y, float height, float temp, float moisture,
        BiomeVegetation biomePlants, List<GameObject> chunkVegetation)
    {
        ProcessVegetationType(biomePlants.trees, worldPos, x, y, height, temp, moisture, chunkVegetation, true);
        ProcessVegetationType(biomePlants.grains, worldPos, x, y, height, temp, moisture, chunkVegetation, false);
        ProcessVegetationType(biomePlants.legumes, worldPos, x, y, height, temp, moisture, chunkVegetation, false);
        ProcessVegetationType(biomePlants.wildPlants, worldPos, x, y, height, temp, moisture, chunkVegetation, false);

        if (biomePlants.grassPrefabs.Length > 0)
        {
            // Implementation for grass using Unity's terrain system or GPU instancing
        }
    }

    private void ProcessVegetationType(List<VegetationSettings> vegetationList, Vector2 worldPos, int x, int y,
        float height, float temp, float moisture, List<GameObject> chunkVegetation, bool isTree)
    {
        foreach (var veg in vegetationList)
        {
            if (!veg.prefab || temp < veg.minTemp || temp > veg.maxTemp ||
                moisture < veg.minMoisture || moisture > veg.maxMoisture)
                continue;

            float density = Mathf.Lerp(veg.minDensity, veg.maxDensity,
                GetEnvironmentalFactor(temp, moisture, veg));

            if (Random.value > density)
                continue;

            GameObject vegObject = GetPooledObject();
            if (vegObject == null)
                return;

            float randomX = Random.Range(-0.5f, 0.5f);
            float randomY = Random.Range(-0.5f, 0.5f);
            Vector3 position = new Vector3(
                worldPos.x + x + randomX,
                height,
                worldPos.y + y + randomY
            );

            SetupVegetationObject(vegObject, veg, position);
            chunkVegetation.Add(vegObject);
        }
    }

    private float GetEnvironmentalFactor(float temp, float moisture, VegetationSettings veg)
    {
        float tempFactor = 1f - Mathf.Abs((temp - (veg.minTemp + veg.maxTemp) * 0.5f) / (veg.maxTemp - veg.minTemp));
        float moistureFactor = 1f - Mathf.Abs((moisture - (veg.minMoisture + veg.maxMoisture) * 0.5f) /
            (veg.maxMoisture - veg.minMoisture));

        return tempFactor * moistureFactor;
    }

    private void SetupVegetationObject(GameObject obj, VegetationSettings settings, Vector3 position)
    {
        // Store original prefab reference
        var prefabInstance = obj.AddComponent<VegetationInstance>();
        prefabInstance.originalPrefab = settings.prefab;

        // Set transform
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        float scale = Random.Range(settings.scaleRange.x, settings.scaleRange.y);
        obj.transform.localScale = Vector3.one * scale;

        // Set up mesh and materials from prefab
        var meshFilter = obj.AddComponent<MeshFilter>();
        var meshRenderer = obj.AddComponent<MeshRenderer>();
        var prefabMeshFilter = settings.prefab.GetComponent<MeshFilter>();
        var prefabMeshRenderer = settings.prefab.GetComponent<MeshRenderer>();

        if (prefabMeshFilter && prefabMeshRenderer)
        {
            meshFilter.sharedMesh = prefabMeshFilter.sharedMesh;
            meshRenderer.sharedMaterials = prefabMeshRenderer.sharedMaterials;
        }

        if (settings.usePhysics)
        {
            if (!obj.GetComponent<Collider>())
                obj.AddComponent<MeshCollider>();
            if (!obj.GetComponent<Rigidbody>())
            {
                var rb = obj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
        }

        obj.SetActive(true);
    }

    private GameObject GetPooledObject()
    {
        if (objectPool.Count > 0)
            return objectPool.Dequeue();
        return null;
    }

    private BiomeVegetation GetBiomeVegetation(float temperature, float moisture)
    {
        if (biomeSettings == null)
            return null;

        // Find matching biome layer
        for (int i = 0; i < biomeSettings.biomeLayers.Length; i++)
        {
            var layer = biomeSettings.biomeLayers[i];
            if (temperature >= layer.minTemperature && temperature <= layer.maxTemperature &&
                moisture >= layer.minMoisture && moisture <= layer.maxMoisture)
            {
                return biomeVegetation[i];
            }
        }
        return null;
    }

    public void RemoveChunkVegetation(Vector2Int chunkCoord)
    {
        if (!activeChunks.ContainsKey(chunkCoord))
            return;

        foreach (var obj in activeChunks[chunkCoord])
        {
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        activeChunks.Remove(chunkCoord);
    }
}

// Helper component to store prefab reference
public class VegetationInstance : MonoBehaviour
{
    public GameObject originalPrefab;
}