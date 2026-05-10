using UnityEngine;
using UnityEngine.AI;

public class NPCCitySpawner : MonoBehaviour
{
    [Header("NPC Source")]
    [SerializeField] private GameObject npcModelPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int npcCount = 15;
    [SerializeField] private Transform cityCenterPoint;
    [SerializeField] private Vector2 cityHalfSize = new Vector2(120f, 120f);
    [SerializeField] private float spawnHeight = 5f;
    [SerializeField] private int maxSpawnAttemptsPerNpc = 12;

    [Header("Walk Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float angularSpeed = 220f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float patrolRadius = 35f;
    [SerializeField] private float repathDelay = 1.5f;

    [Header("Visuals")]
    [SerializeField] private bool forceSolidBlackMaterial = true;

    [Header("Pool")]
    [Tooltip("Maximum number of NPC instances pooled. If 0, defaults to npcCount.")]
    [SerializeField] private int poolLimit = 0;
    [Tooltip("If true, uses existing child GameObjects under this spawner as the pool (no Instantiate).")]
    [SerializeField] private bool useSceneChildrenAsPool = true;

    private Material sharedBlackMaterial;
    // simple pool (lazily populated)
    private GameObject[] npcPool;
    private int poolIndex = 0;
    private int instantiatedCount = 0;

    private void Start()
    {
        if (npcModelPrefab == null)
        {
            Debug.LogWarning("NPCCitySpawner: npcModelPrefab is not assigned.");
            return;
        }

        if (forceSolidBlackMaterial)
        {
            sharedBlackMaterial = CreateBlackMaterial();
        }

        // prepare pool storage (lazy instantiation)
        int limit = poolLimit > 0 ? poolLimit : npcCount;
        CreatePool(limit);
        // spawn initial NPCs (instances will be created lazily)
        SpawnAllFromPool();
    }

    private void CreatePool(int size)
    {
        npcPool = new GameObject[size];
        poolIndex = 0;
        instantiatedCount = 0;

        if (useSceneChildrenAsPool)
        {
            // Collect suitable child objects (already placed in scene) to serve as pool
            var candidates = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child == null) continue;
                GameObject go = child.gameObject;
                if (go == null) continue;

                // Heuristic: treat child as NPC if it has NavMeshAgent or NPCWanderer
                if (go.GetComponent<NavMeshAgent>() != null || go.GetComponent<NPCWanderer>() != null)
                {
                    candidates.Add(go);
                }
            }

            int fill = Mathf.Min(size, candidates.Count);
            for (int i = 0; i < fill; i++)
            {
                npcPool[i] = candidates[i];
                npcPool[i].SetActive(false);
            }
            instantiatedCount = fill;

            if (fill < size)
            {
                Debug.LogWarning($"NPCCitySpawner: only found {fill} child NPCs for pool, requested {size}. Set poolLimit or add NPC children.");
            }
            return;
        }
    }

    private GameObject GetPooledNpc()
    {
        if (npcPool == null || npcPool.Length == 0) return null;

        // search for an available inactive object starting from poolIndex
        for (int i = 0; i < npcPool.Length; i++)
        {
            int idx = (poolIndex + i) % npcPool.Length;
            GameObject candidate = npcPool[idx];
            if (candidate == null) continue;

            if (!candidate.activeInHierarchy)
            {
                poolIndex = (idx + 1) % npcPool.Length;
                return candidate;
            }
        }

        // nothing available
        return null;
    }

    private void SpawnAllFromPool()
    {
        for (int i = 0; i < npcCount; i++)
        {
            SpawnNpcFromPool();
        }
    }

    private void SpawnNpcFromPool()
    {
        Vector3 center = cityCenterPoint != null ? cityCenterPoint.position : Vector3.zero;

        for (int attempt = 0; attempt < maxSpawnAttemptsPerNpc; attempt++)
        {
            Vector3 randomPoint = center + new Vector3(
                Random.Range(-cityHalfSize.x, cityHalfSize.x),
                spawnHeight,
                Random.Range(-cityHalfSize.y, cityHalfSize.y));

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, 20f, NavMesh.AllAreas))
            {
                continue;
            }

            GameObject npc = GetPooledNpc();
            if (npc == null) return; // pool exhausted

            npc.transform.SetParent(transform, true);
            npc.transform.position = navHit.position;
            npc.transform.rotation = Quaternion.identity;
            npc.SetActive(true);

            EnsureNavMeshAgent(npc, out NavMeshAgent agent);
            if (agent != null)
            {
                agent.Warp(navHit.position);
                ConfigureAgent(agent);
            }

            EnsureWanderComponent(npc);
            ApplyBlackMaterial(npc);
            return;
        }
    }

    private void SpawnSingleNpc()
    {
        Vector3 center = cityCenterPoint != null ? cityCenterPoint.position : Vector3.zero;

        for (int attempt = 0; attempt < maxSpawnAttemptsPerNpc; attempt++)
        {
            Vector3 randomPoint = center + new Vector3(
                Random.Range(-cityHalfSize.x, cityHalfSize.x),
                spawnHeight,
                Random.Range(-cityHalfSize.y, cityHalfSize.y));

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, 20f, NavMesh.AllAreas))
            {
                continue;
            }

            // try to get from pool (lazily instantiate if required)
            GameObject npc = GetPooledNpc();
            if (npc == null) return;

            npc.transform.SetParent(transform, true);
            npc.transform.position = navHit.position;
            npc.transform.rotation = Quaternion.identity;
            npc.SetActive(true);

            EnsureNavMeshAgent(npc, out NavMeshAgent agent);
            if (agent != null)
            {
                agent.Warp(navHit.position);
                ConfigureAgent(agent);
            }

            EnsureWanderComponent(npc);
            ApplyBlackMaterial(npc);
            return;
        }
    }

    // Return NPC back to pool (deactivate and reset). Call this when NPC 'dies' or is removed.
    public void ReleaseNpc(GameObject npc)
    {
        if (npc == null) return;

        // stop agent if present
        if (npc.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.ResetPath();
        }

        npc.SetActive(false);
        npc.transform.SetParent(transform, true);
    }

    private void EnsureNavMeshAgent(GameObject npc, out NavMeshAgent agent)
    {
        agent = npc.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = npc.AddComponent<NavMeshAgent>();
        }
    }

    private void EnsureNpcCollider(GameObject npc)
    {
        CapsuleCollider capsule = npc.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = npc.AddComponent<CapsuleCollider>();
        }

        capsule.isTrigger = false;
        capsule.direction = 1;
        capsule.center = Vector3.up * 1f;
        capsule.radius = 0.35f;
        capsule.height = 1.8f;
    }

    private void ConfigureAgent(NavMeshAgent agent)
    {
        agent.speed = moveSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = 0.25f;
        agent.autoBraking = false;
    }

    private void EnsureWanderComponent(GameObject npc)
    {
        NPCWanderer wanderer = npc.GetComponent<NPCWanderer>();
        if (wanderer == null)
        {
            wanderer = npc.AddComponent<NPCWanderer>();
        }

        Vector3 center = cityCenterPoint != null ? cityCenterPoint.position : Vector3.zero;
        wanderer.Initialize(center, cityHalfSize, patrolRadius, repathDelay);
    }

    private void ApplyBlackMaterial(GameObject npc)
    {
        if (!forceSolidBlackMaterial || sharedBlackMaterial == null) return;

        Renderer[] renderers = npc.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = sharedBlackMaterial;
            }
            renderer.materials = mats;
        }
    }

    private Material CreateBlackMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = Color.black;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.black);
        return material;
    }
}
