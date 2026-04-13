using UnityEngine;

[ExecuteInEditMode]
public class ThermalEffect : MonoBehaviour
{
    public Material thermalMat;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (thermalMat == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.Blit(source, destination, thermalMat);
    }
}
