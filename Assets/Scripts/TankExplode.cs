using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class TankExplode : MonoBehaviour
{
    public GameObject Head;
    public float upwardForce = 5f;
    public AudioClip ExplosionClip;
    public GameObject explosion1;
    public GameObject explosion2;
    public AudioSource explosionSnd;
    private NavMeshAgent navMeshAgent;

    [Header("Force Settings")]
    [SerializeField] private float minForce = 10f;
    [SerializeField] private float maxForce = 30f;
    [SerializeField] private bool affectChildren = true;
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

    [Header("Direction Settings")]
    [SerializeField] private float minVerticalAngle = -45f;
    [SerializeField] private float maxVerticalAngle = 45f;
    [SerializeField] private bool restrictHorizontalAngle = false;
    [SerializeField] private float minHorizontalAngle = 0f;
    [SerializeField] private float maxHorizontalAngle = 360f;

    private bool exploded;
    public bool HasExploded => exploded;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (explosion1 != null)
            explosion1.SetActive(false);
        if (explosion2 != null)
            explosion2.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!ShouldRunServerSimulation())
            return;

        if (!exploded && IsProjectile(collision.gameObject))
        {
            exploded = true;
            ApplyUpwardForce();
            ApplyRandomForces();
            if (explosion1 != null)
                explosion1.SetActive(true);
            if (explosion2 != null)
                explosion2.SetActive(true);
            if (explosionSnd != null && ExplosionClip != null)
                explosionSnd.PlayOneShot(ExplosionClip);
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
                StartCoroutine(DespawnAfterDelay(networkObject, 3f));
            else
                Destroy(gameObject, 3f);
            StopTankMovement();
        }
    }

    private System.Collections.IEnumerator DespawnAfterDelay(NetworkObject networkObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (networkObject != null && networkObject.IsSpawned)
            networkObject.Despawn(true);
    }

    private static bool ShouldRunServerSimulation()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager == null || !networkManager.IsListening || networkManager.IsServer;
    }

    private static bool IsProjectile(GameObject collisionObject)
    {
        return collisionObject.CompareTag("Projectiles") || collisionObject.GetComponent<RPGRounds>() != null;
    }

    private void ApplyUpwardForce()
    {
        if (Head != null)
        {
            Rigidbody rb = Head.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = Head.AddComponent<Rigidbody>();
            }

            rb.useGravity = true;
            rb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse);
        }
    }
    private void StopTankMovement()
    {
        if (navMeshAgent != null)
            navMeshAgent.isStopped = true;

        EnemyPatrol enemyPatrol = GetComponent<EnemyPatrol>();
        if (enemyPatrol != null)
            enemyPatrol.enabled = false;
    }
    public void ApplyRandomForces()
    {
        if (!affectChildren)
        {
            SetupAndApplyForce(gameObject);
            return;
        }

        foreach (Transform child in transform)
        {
            SetupAndApplyForce(child.gameObject);
        }
    }

    private void SetupAndApplyForce(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }

        BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = obj.AddComponent<BoxCollider>();
        }

        Vector3 randomDirection = GetRandomDirection();
        float forceMagnitude = Random.Range(minForce, maxForce);

        rb.AddForce(randomDirection * forceMagnitude, forceMode);
    }

    private Vector3 GetRandomDirection()
    {
        float verticalAngle = Random.Range(minVerticalAngle, maxVerticalAngle);
        float horizontalAngle;
        if (restrictHorizontalAngle)
        {
            horizontalAngle = Random.Range(minHorizontalAngle, maxHorizontalAngle);
        }
        else
        {
            horizontalAngle = Random.Range(0f, 360f);
        }

        Vector3 direction = Quaternion.Euler(verticalAngle, horizontalAngle, 0f) * Vector3.forward;
        return direction.normalized;
    }
}
