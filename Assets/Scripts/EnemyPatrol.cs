using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class EnemyPatrol : MonoBehaviour
{
    public float patrolRange = 10f;
    public float waitTime = 2f;
    private NavMeshAgent navMeshAgent;
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool hasValidNavMesh;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogWarning($"{name} has EnemyPatrol but no NavMeshAgent.", this);
            enabled = false;
            return;
        }

        if (!ShouldRunServerSimulation())
        {
            navMeshAgent.isStopped = true;
            return;
        }

        hasValidNavMesh = EnsureAgentIsOnNavMesh();
        if (!hasValidNavMesh)
        {
            Debug.LogWarning($"{name} could not find a nearby NavMesh for its agent type.", this);
            enabled = false;
            return;
        }

        initialPosition = transform.position;
        SetNewPatrolPoint();
    }

    void Update()
    {
        if (!ShouldRunServerSimulation())
        {
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                navMeshAgent.isStopped = true;
            return;
        }

        if (!hasValidNavMesh || navMeshAgent == null || !navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh)
            return;

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !isWaiting)
        {
            isWaiting = true;
            waitTimer = 0f;
        }

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                SetNewPatrolPoint();
            }
        }
    }

    private bool EnsureAgentIsOnNavMesh()
    {
        if (navMeshAgent.isOnNavMesh)
            return true;

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, patrolRange, NavMesh.AllAreas))
            return false;

        return navMeshAgent.Warp(hit.position);
    }

    void SetNewPatrolPoint()
    {
        if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh)
            return;

        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += initialPosition;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            navMeshAgent.SetDestination(targetPosition);
        }
    }

    private static bool ShouldRunServerSimulation()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager == null || !networkManager.IsListening || networkManager.IsServer;
    }
}
