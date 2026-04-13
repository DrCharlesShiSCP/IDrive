using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class NetworkTurretController : NetworkBehaviour
{
    [Header("References")]
    public Transform yawPivot;
    public Transform pitchPivot;

    [Header("Aim Speed")]
    public float yawSpeed = 70f;
    public float pitchSpeed = 45f;

    [Header("Yaw Constraints")]
    public float minYaw = -180f;
    public float maxYaw = 180f;

    [Header("Pitch Constraints")]
    public float minPitch = -8f;
    public float maxPitch = 25f;
    public bool invertPitch;

    public NetworkVariable<float> Yaw = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> Pitch = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Quaternion baseYawRotation;
    private Quaternion basePitchRotation;
    private Vector2 aimInput;

    private void Reset()
    {
        yawPivot = transform;
        pitchPivot = transform;
    }

    private void Awake()
    {
        if (yawPivot == null)
            yawPivot = transform;

        if (pitchPivot == null)
            pitchPivot = yawPivot;

        baseYawRotation = yawPivot.localRotation;
        basePitchRotation = pitchPivot.localRotation;
    }

    private void Update()
    {
        if (IsServer)
            UpdateServerAim();

        ApplyAim(Yaw.Value, Pitch.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitAimInputServerRpc(Vector2 input, ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorizedSpotter(rpcParams.Receive.SenderClientId))
            return;

        aimInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void UpdateServerAim()
    {
        float dt = Time.deltaTime;
        float pitchDirection = invertPitch ? -aimInput.y : aimInput.y;

        Yaw.Value = Mathf.Clamp(Yaw.Value + aimInput.x * yawSpeed * dt, minYaw, maxYaw);
        Pitch.Value = Mathf.Clamp(Pitch.Value + pitchDirection * pitchSpeed * dt, minPitch, maxPitch);
    }

    private void ApplyAim(float yaw, float pitch)
    {
        if (yawPivot != null && yawPivot == pitchPivot)
        {
            yawPivot.localRotation = baseYawRotation * Quaternion.Euler(pitch, yaw, 0f);
            return;
        }

        if (yawPivot != null)
            yawPivot.localRotation = baseYawRotation * Quaternion.Euler(0f, yaw, 0f);

        if (pitchPivot != null)
            pitchPivot.localRotation = basePitchRotation * Quaternion.Euler(pitch, 0f, 0f);
    }

    private static bool IsAuthorizedSpotter(ulong senderClientId)
    {
        return TankCrewRoleManager.Instance != null &&
               TankCrewRoleManager.Instance.ClientHasRole(senderClientId, TankCrewRole.Spotter);
    }
}
