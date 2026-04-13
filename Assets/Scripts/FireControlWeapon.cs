using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FireControlWeapon : MonoBehaviour
{
    [Header("References")]
    public Transform barrel;
    public GameObject projectilePrefab;
    public Image reloadProgressImage;

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

    [Header("Input")]
    public Key fireKey = Key.Space;
    public Key reloadKey = Key.R;

    private bool shellLoaded;
    private float reloadTimer;

    public bool IsLoaded => shellLoaded;
    public float ReloadProgress => shellLoaded ? 1f : Mathf.Clamp01(reloadTimer / Mathf.Max(0.01f, reloadSeconds));

    private void Awake()
    {
        shellLoaded = shellLoadedOnStart;

        if (reloadProgressImage == null)
        {
            GameObject reloadProgressObject = GameObject.Find("ReloadProgress");
            if (reloadProgressObject != null)
                reloadProgressImage = reloadProgressObject.GetComponent<Image>();
        }

        UpdateReloadUi();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[fireKey].wasPressedThisFrame)
            TryFire();

        if (!shellLoaded)
            UpdateReload();
    }

    public bool TryFire()
    {
        if (!shellLoaded || barrel == null || projectilePrefab == null)
            return false;

        Vector3 spawnPosition = barrel.position + barrel.forward * muzzleOffset;
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, barrel.rotation);
        TryAssignProjectileTag(projectile);
        LaunchProjectile(projectile);

        if (projectileLifetime > 0f)
            Destroy(projectile, projectileLifetime);

        shellLoaded = false;
        reloadTimer = 0f;
        UpdateReloadUi();
        return true;
    }

    public void LoadShell()
    {
        shellLoaded = true;
        reloadTimer = reloadSeconds;
        UpdateReloadUi();
    }

    public void UnloadShell()
    {
        shellLoaded = false;
        reloadTimer = 0f;
        UpdateReloadUi();
    }

    private void UpdateReload()
    {
        if (Keyboard.current[reloadKey].isPressed)
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

        if (reloadTimer >= reloadSeconds)
            LoadShell();
        else
            UpdateReloadUi();
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

    private void UpdateReloadUi()
    {
        if (reloadProgressImage == null)
            return;

        reloadProgressImage.fillAmount = ReloadProgress;
    }
}
