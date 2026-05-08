using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCWanderer : MonoBehaviour
{
    private NavMeshAgent agent;
    private Vector3 cityCenter;
    private Vector2 cityHalfSize;
    private float patrolRadius;
    private float repathDelay;
    private float repathTimer;
    private bool isInitialized;
    private NavMeshPath sharedPath;

    public void Initialize(Vector3 center, Vector2 halfSize, float radius, float delay)
    {
        cityCenter = center;
        cityHalfSize = halfSize;
        patrolRadius = Mathf.Max(2f, radius);
        repathDelay = Mathf.Max(0.2f, delay);
        isInitialized = true;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sharedPath = new NavMeshPath();
    }

    private void OnEnable()
    {
        repathTimer = 0f;
    }

    private void Update()
    {
        if (!isInitialized || agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        repathTimer -= Time.deltaTime;

        bool reachedTarget = !agent.pathPending && agent.hasPath &&
                             agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        bool pathInvalid = !agent.pathPending && agent.hasPath &&
                           (agent.pathStatus != NavMeshPathStatus.PathComplete || agent.isPathStale);

        if ((reachedTarget || !agent.hasPath || pathInvalid) && repathTimer <= 0f)
        {
            TrySetNewDestination();
            repathTimer = repathDelay;
        }

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private void TrySetNewDestination()
    {
        float minTravelDistance = Mathf.Max(4f, patrolRadius * 0.35f);

        // 1) Try global city targets first, so NPCs move across districts and cross roads.
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 cityCandidate = GetRandomPointInCityBounds();
            if (TryBuildReachableDestination(cityCandidate, 12f, minTravelDistance, out Vector3 destination))
            {
                agent.SetDestination(destination);
                return;
            }
        }

        // 2) Fallback: local search near current position.
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector3 localOffset = Random.insideUnitSphere * patrolRadius;
            localOffset.y = 0f;
            Vector3 localCandidate = transform.position + localOffset;

            if (TryBuildReachableDestination(localCandidate, 10f, 0f, out Vector3 destination))
            {
                agent.SetDestination(destination);
                return;
            }
        }
    }

    private Vector3 GetRandomPointInCityBounds()
    {
        return new Vector3(
            Random.Range(cityCenter.x - cityHalfSize.x, cityCenter.x + cityHalfSize.x),
            cityCenter.y,
            Random.Range(cityCenter.z - cityHalfSize.y, cityCenter.z + cityHalfSize.y));
    }

    private bool TryBuildReachableDestination(Vector3 candidate, float sampleRadius, float minDistance, out Vector3 destination)
    {
        destination = Vector3.zero;
        if (agent == null || sharedPath == null)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            return false;
        }

        if (minDistance > 0f && Vector3.Distance(transform.position, hit.position) < minDistance)
        {
            return false;
        }

        if (!agent.CalculatePath(hit.position, sharedPath))
        {
            return false;
        }

        if (sharedPath.status != NavMeshPathStatus.PathComplete || sharedPath.corners.Length < 2)
        {
            return false;
        }

        destination = hit.position;
        return true;
    }
}
