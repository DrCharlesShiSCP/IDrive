using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NetworkReloadProgressUI : MonoBehaviour
{
    public NetworkTankWeapon tankWeapon;
    public Image reloadProgressImage;

    private void Awake()
    {
        if (reloadProgressImage == null)
            reloadProgressImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (tankWeapon == null)
            tankWeapon = FindFirstObjectByType<NetworkTankWeapon>();

        if (tankWeapon == null || reloadProgressImage == null)
            return;

        reloadProgressImage.fillAmount = tankWeapon.ReloadProgress.Value;
    }
}
