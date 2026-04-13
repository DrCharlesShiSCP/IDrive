using UnityEngine;
using Unity.Netcode;

public class RPGRounds : MonoBehaviour
{
    public GameObject collisionEffect;
    public float destroyDelay = 2f;
    public AudioClip bombSound;
    public AudioSource bombsoundSource;

    private void OnCollisionEnter(Collision collision)
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.NetworkManager != null && !networkObject.NetworkManager.IsServer)
            return;

        if (collisionEffect != null)
        {
            Instantiate(collisionEffect, collision.contacts[0].point, Quaternion.identity);
        }
        if (bombsoundSource != null && bombSound != null)
            bombsoundSource.PlayOneShot(bombSound);

        if (networkObject != null && networkObject.IsSpawned)
            StartCoroutine(DespawnAfterDelay(networkObject));
        else
            Destroy(gameObject, destroyDelay);
    }

    private System.Collections.IEnumerator DespawnAfterDelay(NetworkObject networkObject)
    {
        yield return new WaitForSeconds(destroyDelay);

        if (networkObject != null && networkObject.IsSpawned)
            networkObject.Despawn(true);
    }
}
