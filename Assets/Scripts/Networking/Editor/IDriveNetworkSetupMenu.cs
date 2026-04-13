using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IDriveNetworkSetupMenu
{
    private const string PlayerPrefabPath = "Assets/Prefabs/NetworkTankCrewPlayer.prefab";

    [MenuItem("IDrive/Networking/Setup Server-Side Multiplayer")]
    public static void SetupServerSideMultiplayer()
    {
        NetworkManager networkManager = EnsureNetworkManager();
        EnsureRoleManager();
        EnsureLocalRoleCameraManager();
        EnsureNetworkedTank();
        EnsurePlayerPrefab(networkManager);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[IDriveNetworkSetup] Server-side multiplayer objects and components are set up. Assign the RPG projectile prefab on NetworkTankWeapon, then register it in NetworkManager Network Prefabs.");
    }

    private static NetworkManager EnsureNetworkManager()
    {
        NetworkManager networkManager = Object.FindFirstObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            GameObject networkObject = new GameObject("NetworkManager");
            Undo.RegisterCreatedObjectUndo(networkObject, "Create NetworkManager");
            networkManager = Undo.AddComponent<NetworkManager>(networkObject);
        }

        if (networkManager.GetComponent<UnityTransport>() == null)
            Undo.AddComponent<UnityTransport>(networkManager.gameObject);

        if (networkManager.GetComponent<NetworkConnectionAndRoleUI>() == null)
            Undo.AddComponent<NetworkConnectionAndRoleUI>(networkManager.gameObject);

        return networkManager;
    }

    private static void EnsureRoleManager()
    {
        GameObject roleManagerObject = FindSceneObject("Tank Crew Role Manager");
        if (roleManagerObject == null)
        {
            roleManagerObject = new GameObject("Tank Crew Role Manager");
            Undo.RegisterCreatedObjectUndo(roleManagerObject, "Create Tank Crew Role Manager");
        }

        EnsureComponent<NetworkObject>(roleManagerObject);
        EnsureComponent<TankCrewRoleManager>(roleManagerObject);
    }

    private static void EnsureLocalRoleCameraManager()
    {
        GameObject cameraManagerObject = FindSceneObject("Local Role Camera Manager");
        if (cameraManagerObject == null)
        {
            cameraManagerObject = new GameObject("Local Role Camera Manager");
            Undo.RegisterCreatedObjectUndo(cameraManagerObject, "Create Local Role Camera Manager");
        }

        LocalRoleCameraManager cameraManager = EnsureComponent<LocalRoleCameraManager>(cameraManagerObject);
        cameraManager.commanderCamera = FindCamera("Commander Camera");
        cameraManager.driverCamera = FindCamera("Driver Camera");
        cameraManager.spotterCamera = FindCamera("Spotter Camera");
        cameraManager.fireControlCamera = FindCamera("Fire Control Camera");
        EditorUtility.SetDirty(cameraManager);
    }

    private static void EnsureNetworkedTank()
    {
        GameObject playersTank = FindSceneObject("PlayersTank");
        if (playersTank == null)
        {
            Debug.LogWarning("[IDriveNetworkSetup] Could not find PlayersTank in the active scene.");
            return;
        }

        EnsureComponent<NetworkObject>(playersTank);
        EnsureComponent<NetworkTransform>(playersTank);

        NetworkTankController tankController = EnsureComponent<NetworkTankController>(playersTank);
        tankController.driveRoot = playersTank.transform;
        tankController.tankRigidbody = playersTank.GetComponent<Rigidbody>();

        NetworkTurretController turretController = EnsureComponent<NetworkTurretController>(playersTank);
        Transform turret = FindChildRecursive(playersTank.transform, "Turret");
        turretController.yawPivot = turret != null ? turret : playersTank.transform;
        turretController.pitchPivot = turretController.yawPivot;

        NetworkTankWeapon tankWeapon = EnsureComponent<NetworkTankWeapon>(playersTank);
        Transform firePoint = FindChildRecursive(playersTank.transform, "FirePoint");
        tankWeapon.barrel = firePoint != null ? firePoint : turretController.pitchPivot;

        EditorUtility.SetDirty(tankController);
        EditorUtility.SetDirty(turretController);
        EditorUtility.SetDirty(tankWeapon);
    }

    private static void EnsurePlayerPrefab(NetworkManager networkManager)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            GameObject instance = new GameObject("NetworkTankCrewPlayer");
            instance.AddComponent<NetworkObject>();
            instance.AddComponent<TankCrewPlayer>();
            instance.AddComponent<TankCrewInputClient>();

            prefab = PrefabUtility.SaveAsPrefabAsset(instance, PlayerPrefabPath);
            Object.DestroyImmediate(instance);
        }

        if (networkManager != null && networkManager.NetworkConfig != null)
        {
            networkManager.NetworkConfig.PlayerPrefab = prefab;
            EditorUtility.SetDirty(networkManager);
        }
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static Camera FindCamera(string cameraName)
    {
        GameObject cameraObject = FindSceneObject(cameraName);
        return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindChildRecursive(root.transform, objectName);
            if (result != null)
                return result.gameObject;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        foreach (Transform child in root)
        {
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
