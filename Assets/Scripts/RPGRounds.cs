using UnityEngine;
using Unity.Netcode;

public class RPGRounds : NetworkBehaviour
{
    public GameObject collisionEffect;
    public float destroyDelay = 2f;
    public AudioClip bombSound;
    public AudioSource bombsoundSource;

    private bool hasCollided;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided)
            return;

        NetworkObject networkObject = NetworkObject;
        if (networkObject != null && networkObject.IsSpawned && !IsServer)
            return;

        hasCollided = true;
        Vector3 contactPoint = GetContactPoint(collision);

        if (networkObject != null && networkObject.IsSpawned)
            SpawnCollisionEffectClientRpc(contactPoint);
        else
            SpawnCollisionEffect(contactPoint);

        if (networkObject != null && networkObject.IsSpawned)
            StartCoroutine(DespawnAfterDelay(networkObject));
        else
            Destroy(gameObject, destroyDelay);
    }

    [ClientRpc]
    private void SpawnCollisionEffectClientRpc(Vector3 contactPoint)
    {
        SpawnCollisionEffect(contactPoint);
    }

    private void SpawnCollisionEffect(Vector3 contactPoint)
    {
        if (collisionEffect != null)
            Instantiate(collisionEffect, contactPoint, Quaternion.identity);

        if (bombsoundSource != null && bombSound != null)
            bombsoundSource.PlayOneShot(bombSound);
        else if (bombSound != null)
            AudioSource.PlayClipAtPoint(bombSound, contactPoint);
    }

    private static Vector3 GetContactPoint(Collision collision)
    {
        return collision.contactCount > 0 ? collision.GetContact(0).point : collision.transform.position;
    }

    private System.Collections.IEnumerator DespawnAfterDelay(NetworkObject networkObject)
    {
        yield return new WaitForSeconds(destroyDelay);

        if (networkObject != null && networkObject.IsSpawned)
            networkObject.Despawn(true);
    }
}
