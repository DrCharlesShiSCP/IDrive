using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class EnemyPatrol : MonoBehaviour
{
    public float patrolRange = 10f;
    public float waitTime = 2f;
    [SerializeField] private bool makeRigidbodyKinematicWhilePatrolling = true;

    private NavMeshAgent navMeshAgent;
    private Rigidbody patrolRigidbody;
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool hasValidNavMesh;
    private bool cachedRigidbodyState;
    private bool originalRigidbodyIsKinematic;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        patrolRigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (navMeshAgent == null)
        {
            Debug.LogWarning($"{name} has EnemyPatrol but no NavMeshAgent.", this);
            enabled = false;
            return;
        }

        if (!ShouldRunServerSimulation())
        {
            PrepareRigidbodyForNavigation();
            DisableLocalAgentSimulation();
            return;
        }

        hasValidNavMesh = EnsureAgentIsOnNavMesh();
        if (!hasValidNavMesh)
        {
            Debug.LogWarning($"{name} could not find a nearby NavMesh for its agent type.", this);
            enabled = false;
            return;
        }

        PrepareRigidbodyForNavigation();
        navMeshAgent.isStopped = false;
        initialPosition = transform.position;
        SetNewPatrolPoint();
    }

    void Update()
    {
        if (!ShouldRunServerSimulation())
        {
            DisableLocalAgentSimulation();
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

    public void ReleaseRigidbodyControl()
    {
        if (patrolRigidbody == null || !cachedRigidbodyState)
            return;

        patrolRigidbody.isKinematic = originalRigidbodyIsKinematic;
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

    private void PrepareRigidbodyForNavigation()
    {
        if (!makeRigidbodyKinematicWhilePatrolling || patrolRigidbody == null)
            return;

        if (!cachedRigidbodyState)
        {
            originalRigidbodyIsKinematic = patrolRigidbody.isKinematic;
            cachedRigidbodyState = true;
        }

        if (!patrolRigidbody.isKinematic)
        {
            patrolRigidbody.linearVelocity = Vector3.zero;
            patrolRigidbody.angularVelocity = Vector3.zero;
        }

        patrolRigidbody.isKinematic = true;
    }

    private void DisableLocalAgentSimulation()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled)
            return;

        if (navMeshAgent.isOnNavMesh)
            navMeshAgent.isStopped = true;

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
        navMeshAgent.enabled = false;
        enabled = false;
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
