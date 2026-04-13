using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(TankCrewPlayer))]
public class TankCrewInputClient : NetworkBehaviour
{
    [Header("Network Targets")]
    public NetworkTankController tankController;
    public NetworkTurretController turretController;
    public NetworkTankWeapon tankWeapon;

    [Header("Input Send Rate")]
    public float inputSendInterval = 0.05f;

    [Header("Driver Keys")]
    public Key forwardKey = Key.W;
    public Key reverseKey = Key.S;
    public Key turnLeftKey = Key.A;
    public Key turnRightKey = Key.D;
    public Key brakeKey = Key.Space;
    public bool driverUsesArrowKeys = true;

    [Header("Spotter Keys")]
    public Key yawLeftKey = Key.A;
    public Key yawRightKey = Key.D;
    public Key pitchUpKey = Key.W;
    public Key pitchDownKey = Key.S;
    public bool spotterUsesArrowKeys = true;

    [Header("Fire Control Keys")]
    public Key fireKey = Key.Space;
    public Key reloadKey = Key.R;

    private TankCrewPlayer crewPlayer;
    private float nextInputSendTime;
    private bool lastReloadHeld;

    private void Awake()
    {
        crewPlayer = GetComponent<TankCrewPlayer>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            enabled = false;
    }

    private void Update()
    {
        if (!IsOwner || Keyboard.current == null)
            return;

        CacheTargetsIfNeeded();

        switch (crewPlayer.AssignedRole)
        {
            case TankCrewRole.Driver:
                SendDriverInput();
                break;
            case TankCrewRole.Spotter:
                SendSpotterInput();
                break;
            case TankCrewRole.FireControl:
                SendFireControlInput();
                break;
        }
    }

    private void SendDriverInput()
    {
        if (tankController == null || !CanSendContinuousInput())
            return;

        float throttle = Axis(forwardKey, reverseKey, driverUsesArrowKeys ? Key.UpArrow : Key.None, driverUsesArrowKeys ? Key.DownArrow : Key.None);
        float steering = Axis(turnRightKey, turnLeftKey, driverUsesArrowKeys ? Key.RightArrow : Key.None, driverUsesArrowKeys ? Key.LeftArrow : Key.None);
        tankController.SubmitDriverInputServerRpc(throttle, steering, IsPressed(brakeKey));
    }

    private void SendSpotterInput()
    {
        if (turretController == null || !CanSendContinuousInput())
            return;

        float yaw = Axis(yawRightKey, yawLeftKey, spotterUsesArrowKeys ? Key.RightArrow : Key.None, spotterUsesArrowKeys ? Key.LeftArrow : Key.None);
        float pitch = Axis(pitchUpKey, pitchDownKey, spotterUsesArrowKeys ? Key.UpArrow : Key.None, spotterUsesArrowKeys ? Key.DownArrow : Key.None);
        turretController.SubmitAimInputServerRpc(new Vector2(yaw, pitch));
    }

    private void SendFireControlInput()
    {
        if (tankWeapon == null)
            return;

        if (Keyboard.current[fireKey].wasPressedThisFrame)
            tankWeapon.RequestFireServerRpc();

        bool reloadHeld = IsPressed(reloadKey);
        if (reloadHeld != lastReloadHeld || CanSendContinuousInput())
        {
            lastReloadHeld = reloadHeld;
            tankWeapon.SetReloadHeldServerRpc(reloadHeld);
        }
    }

    private bool CanSendContinuousInput()
    {
        if (Time.unscaledTime < nextInputSendTime)
            return false;

        nextInputSendTime = Time.unscaledTime + inputSendInterval;
        return true;
    }

    private void CacheTargetsIfNeeded()
    {
        if (tankController == null)
            tankController = FindFirstObjectByType<NetworkTankController>();
        if (turretController == null)
            turretController = FindFirstObjectByType<NetworkTurretController>();
        if (tankWeapon == null)
            tankWeapon = FindFirstObjectByType<NetworkTankWeapon>();
    }

    private static float Axis(Key positive, Key negative, Key alternatePositive, Key alternateNegative)
    {
        float value = 0f;
        if (IsPressed(positive) || IsPressed(alternatePositive))
            value += 1f;
        if (IsPressed(negative) || IsPressed(alternateNegative))
            value -= 1f;
        return Mathf.Clamp(value, -1f, 1f);
    }

    private static bool IsPressed(Key key)
    {
        return key != Key.None && Keyboard.current != null && Keyboard.current[key].isPressed;
    }
}
