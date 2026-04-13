using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class DriverTankController : MonoBehaviour
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
    public bool usePhysicsMovement = true;

    [Header("Input")]
    public Key forwardKey = Key.W;
    public Key reverseKey = Key.S;
    public Key turnLeftKey = Key.A;
    public Key turnRightKey = Key.D;
    public Key brakeKey = Key.Space;
    public bool alsoUseArrowKeys = true;

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
    }

    private void FixedUpdate()
    {
        if (Keyboard.current == null)
            return;

        float throttle = GetThrottleInput();
        float steering = GetSteeringInput();
        float dt = Time.fixedDeltaTime;

        if (IsPressed(brakeKey))
        {
            throttle = 0f;
            steering = 0f;
        }

        float targetSpeed = throttle >= 0f ? throttle * forwardSpeed : throttle * reverseSpeed;
        Vector3 targetVelocity = driveRoot.forward * targetSpeed;
        currentMoveVelocity = Vector3.MoveTowards(currentMoveVelocity, targetVelocity, acceleration * dt);
        currentTurnVelocity = Mathf.MoveTowards(currentTurnVelocity, steering * turnSpeed, turnAcceleration * dt);

        if (usePhysicsMovement && tankRigidbody != null && !tankRigidbody.isKinematic)
        {
            tankRigidbody.MovePosition(tankRigidbody.position + currentMoveVelocity * dt);
            tankRigidbody.MoveRotation(tankRigidbody.rotation * Quaternion.Euler(0f, currentTurnVelocity * dt, 0f));
            return;
        }

        driveRoot.position += currentMoveVelocity * dt;
        driveRoot.Rotate(0f, currentTurnVelocity * dt, 0f, Space.World);
    }

    private float GetThrottleInput()
    {
        float input = 0f;
        if (IsPressed(forwardKey) || (alsoUseArrowKeys && IsPressed(Key.UpArrow)))
            input += 1f;
        if (IsPressed(reverseKey) || (alsoUseArrowKeys && IsPressed(Key.DownArrow)))
            input -= 1f;
        return Mathf.Clamp(input, -1f, 1f);
    }

    private float GetSteeringInput()
    {
        float input = 0f;
        if (IsPressed(turnRightKey) || (alsoUseArrowKeys && IsPressed(Key.RightArrow)))
            input += 1f;
        if (IsPressed(turnLeftKey) || (alsoUseArrowKeys && IsPressed(Key.LeftArrow)))
            input -= 1f;
        return Mathf.Clamp(input, -1f, 1f);
    }

    private static bool IsPressed(Key key)
    {
        return Keyboard.current != null && Keyboard.current[key].isPressed;
    }
}
