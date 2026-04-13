using UnityEngine;

[DisallowMultipleComponent]
public class LocalRoleCameraManager : MonoBehaviour
{
    public static LocalRoleCameraManager Instance { get; private set; }

    [Header("Role Cameras")]
    public Camera commanderCamera;
    public Camera driverCamera;
    public Camera spotterCamera;
    public Camera fireControlCamera;

    [Header("Role-Specific Local Objects")]
    public Behaviour[] commanderOnlyBehaviours;
    public Behaviour[] driverOnlyBehaviours;
    public Behaviour[] spotterOnlyBehaviours;
    public Behaviour[] fireControlOnlyBehaviours;
    public GameObject[] commanderOnlyObjects;
    public GameObject[] driverOnlyObjects;
    public GameObject[] spotterOnlyObjects;
    public GameObject[] fireControlOnlyObjects;

    public TankCrewRole CurrentLocalRole { get; private set; } = TankCrewRole.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyRole(TankCrewRole.None);
    }

    public void ApplyRole(TankCrewRole role)
    {
        CurrentLocalRole = role;
        bool useDriverAsLobbyCamera = role == TankCrewRole.None && driverCamera != null;

        SetCameraActive(commanderCamera, role == TankCrewRole.Commander);
        SetCameraActive(driverCamera, role == TankCrewRole.Driver || useDriverAsLobbyCamera);
        SetCameraActive(spotterCamera, role == TankCrewRole.Spotter);
        SetCameraActive(fireControlCamera, role == TankCrewRole.FireControl);

        SetBehaviours(commanderOnlyBehaviours, role == TankCrewRole.Commander);
        SetBehaviours(driverOnlyBehaviours, role == TankCrewRole.Driver);
        SetBehaviours(spotterOnlyBehaviours, role == TankCrewRole.Spotter);
        SetBehaviours(fireControlOnlyBehaviours, role == TankCrewRole.FireControl);

        SetObjects(commanderOnlyObjects, role == TankCrewRole.Commander);
        SetObjects(driverOnlyObjects, role == TankCrewRole.Driver);
        SetObjects(spotterOnlyObjects, role == TankCrewRole.Spotter);
        SetObjects(fireControlOnlyObjects, role == TankCrewRole.FireControl);
    }

    private static void SetCameraActive(Camera targetCamera, bool active)
    {
        if (targetCamera == null)
            return;

        targetCamera.gameObject.SetActive(active);
        targetCamera.enabled = active;

        AudioListener listener = targetCamera.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = active;
    }

    private static void SetBehaviours(Behaviour[] behaviours, bool active)
    {
        if (behaviours == null)
            return;

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null)
                behaviour.enabled = active;
        }
    }

    private static void SetObjects(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject targetObject in objects)
        {
            if (targetObject != null)
                targetObject.SetActive(active);
        }
    }
}
