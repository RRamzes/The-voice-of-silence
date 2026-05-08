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

    private Material sharedBlackMaterial;

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

        for (int i = 0; i < npcCount; i++)
        {
            SpawnSingleNpc();
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

            // Создаем NPC
            GameObject npc = Instantiate(npcModelPrefab, navHit.position, Quaternion.identity, transform);

            // Настраиваем агента
            EnsureNavMeshAgent(npc, out NavMeshAgent agent);
            EnsureNpcCollider(npc);

            // КРИТИЧЕСКИЙ МОМЕНТ: Принудительно ставим агента на NavMesh
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
