using System.Collections;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class NetworkTankWeapon : NetworkBehaviour
{
    [Header("References")]
    public Transform barrel;
    public GameObject projectilePrefab;

    [Header("Projectile")]
    public float projectileSpeed = 55f;
    public float projectileLifetime = 8f;
    public float muzzleOffset = 0.75f;
    public string projectileTag = "Projectiles";

    [Header("Reload")]
    public float reloadSeconds = 3f;
    public bool shellLoadedOnStart = true;
    public bool resetProgressWhenReloadReleased;
    public float reloadDecaySpeed = 0f;

    public NetworkVariable<bool> ShellLoaded = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> ReloadProgress = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool reloadHeld;
    private float reloadTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        ShellLoaded.Value = shellLoadedOnStart;
        reloadTimer = shellLoadedOnStart ? reloadSeconds : 0f;
        ReloadProgress.Value = shellLoadedOnStart ? 1f : 0f;
    }

    private void Update()
    {
        if (!IsServer || ShellLoaded.Value)
            return;

        UpdateReload();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestFireServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorizedFireControl(rpcParams.Receive.SenderClientId))
            return;

        TryFireFromServer();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReloadHeldServerRpc(bool held, ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorizedFireControl(rpcParams.Receive.SenderClientId))
            return;

        reloadHeld = held;
    }

    private void TryFireFromServer()
    {
        if (!ShellLoaded.Value || barrel == null || projectilePrefab == null)
            return;

        Vector3 spawnPosition = barrel.position + barrel.forward * muzzleOffset;
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, barrel.rotation);
        TryAssignProjectileTag(projectile);
        LaunchProjectile(projectile);

        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        if (networkObject != null)
            networkObject.Spawn(true);

        if (projectileLifetime > 0f)
            StartCoroutine(DestroyProjectileAfterLifetime(projectile, networkObject));

        ShellLoaded.Value = false;
        reloadTimer = 0f;
        ReloadProgress.Value = 0f;
    }

    private void UpdateReload()
    {
        if (reloadHeld)
        {
            reloadTimer += Time.deltaTime;
        }
        else if (resetProgressWhenReloadReleased)
        {
            reloadTimer = 0f;
        }
        else if (reloadDecaySpeed > 0f)
        {
            reloadTimer = Mathf.Max(0f, reloadTimer - reloadDecaySpeed * Time.deltaTime);
        }

        ReloadProgress.Value = Mathf.Clamp01(reloadTimer / Mathf.Max(0.01f, reloadSeconds));

        if (reloadTimer < reloadSeconds)
            return;

        ShellLoaded.Value = true;
        ReloadProgress.Value = 1f;
    }

    private void LaunchProjectile(GameObject projectile)
    {
        Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();
        if (projectileRigidbody == null)
            projectileRigidbody = projectile.AddComponent<Rigidbody>();

        projectileRigidbody.useGravity = false;
        projectileRigidbody.linearVelocity = barrel.forward * projectileSpeed;
    }

    private void TryAssignProjectileTag(GameObject projectile)
    {
        if (string.IsNullOrWhiteSpace(projectileTag))
            return;

        try
        {
            projectile.tag = projectileTag;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Projectile tag '{projectileTag}' is not defined in Project Settings > Tags and Layers.", this);
        }
    }

    private IEnumerator DestroyProjectileAfterLifetime(GameObject projectile, NetworkObject networkObject)
    {
        yield return new WaitForSeconds(projectileLifetime);

        if (networkObject != null && networkObject.IsSpawned)
            networkObject.Despawn(true);
        else if (projectile != null)
            Destroy(projectile);
    }

    private static bool IsAuthorizedFireControl(ulong senderClientId)
    {
        return TankCrewRoleManager.Instance != null &&
               TankCrewRoleManager.Instance.ClientHasRole(senderClientId, TankCrewRole.FireControl);
    }
}
