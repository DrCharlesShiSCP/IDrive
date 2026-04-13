using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SpotterTurretAim : MonoBehaviour
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

    [Header("Input")]
    public Key yawLeftKey = Key.A;
    public Key yawRightKey = Key.D;
    public Key pitchUpKey = Key.W;
    public Key pitchDownKey = Key.S;
    public bool alsoUseArrowKeys = true;

    private Quaternion baseYawRotation;
    private Quaternion basePitchRotation;
    private float currentYaw;
    private float currentPitch;

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
        if (Keyboard.current == null)
            return;

        float dt = Time.deltaTime;
        currentYaw = Mathf.Clamp(currentYaw + GetYawInput() * yawSpeed * dt, minYaw, maxYaw);
        currentPitch = Mathf.Clamp(currentPitch + GetPitchInput() * pitchSpeed * dt, minPitch, maxPitch);

        if (yawPivot == pitchPivot)
        {
            yawPivot.localRotation = baseYawRotation * Quaternion.Euler(currentPitch, currentYaw, 0f);
            return;
        }

        yawPivot.localRotation = baseYawRotation * Quaternion.Euler(0f, currentYaw, 0f);
        pitchPivot.localRotation = basePitchRotation * Quaternion.Euler(currentPitch, 0f, 0f);
    }

    public void SetAim(float yaw, float pitch)
    {
        currentYaw = Mathf.Clamp(yaw, minYaw, maxYaw);
        currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private float GetYawInput()
    {
        float input = 0f;
        if (IsPressed(yawRightKey) || (alsoUseArrowKeys && IsPressed(Key.RightArrow)))
            input += 1f;
        if (IsPressed(yawLeftKey) || (alsoUseArrowKeys && IsPressed(Key.LeftArrow)))
            input -= 1f;
        return Mathf.Clamp(input, -1f, 1f);
    }

    private float GetPitchInput()
    {
        float input = 0f;
        if (IsPressed(pitchUpKey) || (alsoUseArrowKeys && IsPressed(Key.UpArrow)))
            input += invertPitch ? -1f : 1f;
        if (IsPressed(pitchDownKey) || (alsoUseArrowKeys && IsPressed(Key.DownArrow)))
            input += invertPitch ? 1f : -1f;
        return Mathf.Clamp(input, -1f, 1f);
    }

    private static bool IsPressed(Key key)
    {
        return Keyboard.current != null && Keyboard.current[key].isPressed;
    }
}
