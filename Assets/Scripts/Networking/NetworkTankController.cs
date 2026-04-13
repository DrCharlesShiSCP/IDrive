using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class NetworkTankController : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody tankRigidbody;
    public Transform driveRoot;

    [Header("Movement")]
    public float forwardSpeed = 7f;
    public float reverseSpeed = 4f;
    public float turnSpeed = 80f;
    public float acceleration = 18f;
    public float turnAcceleration = 180f;

    private float throttleInput;
    private float steeringInput;
    private bool brakeInput;
    private Vector3 currentMoveVelocity;
    private float currentTurnVelocity;

    private void Reset()
    {
        tankRigidbody = GetComponent<Rigidbody>();
        driveRoot = transform;
    }

    private void Awake()
    {
        if (driveRoot == null)
            driveRoot = transform;

        if (tankRigidbody == null)
            tankRigidbody = GetComponent<Rigidbody>();

        NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
            navMeshAgent.enabled = false;
    }

    private void FixedUpdate()
    {
        if (!IsServer)
            return;

        float dt = Time.fixedDeltaTime;
        float throttle = brakeInput ? 0f : throttleInput;
        float steering = brakeInput ? 0f : steeringInput;
        float targetSpeed = throttle >= 0f ? throttle * forwardSpeed : throttle * reverseSpeed;

        Vector3 targetVelocity = driveRoot.forward * targetSpeed;
        currentMoveVelocity = Vector3.MoveTowards(currentMoveVelocity, targetVelocity, acceleration * dt);
        currentTurnVelocity = Mathf.MoveTowards(currentTurnVelocity, steering * turnSpeed, turnAcceleration * dt);

        if (tankRigidbody != null && !tankRigidbody.isKinematic)
        {
            tankRigidbody.MovePosition(tankRigidbody.position + currentMoveVelocity * dt);
            tankRigidbody.MoveRotation(tankRigidbody.rotation * Quaternion.Euler(0f, currentTurnVelocity * dt, 0f));
            return;
        }

        driveRoot.position += currentMoveVelocity * dt;
        driveRoot.Rotate(0f, currentTurnVelocity * dt, 0f, Space.World);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitDriverInputServerRpc(float throttle, float steering, bool brake, ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorizedDriver(rpcParams.Receive.SenderClientId))
            return;

        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steeringInput = Mathf.Clamp(steering, -1f, 1f);
        brakeInput = brake;
    }

    private static bool IsAuthorizedDriver(ulong senderClientId)
    {
        return TankCrewRoleManager.Instance != null &&
               TankCrewRoleManager.Instance.ClientHasRole(senderClientId, TankCrewRole.Driver);
    }
}
