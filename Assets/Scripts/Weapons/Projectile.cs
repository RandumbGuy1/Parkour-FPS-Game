using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float lifeTime;

    private Rigidbody rb;

    void OnEnable()
    {
        rb = GetComponent<Rigidbody>();

        rb.detectCollisions = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Invoke("Explode", lifeTime);
    }

    void OnCollisionEnter(Collision col)
    {
        CancelInvoke("Explode");
        Invoke("Explode", 0.01f);

        ContactPoint contact = col.GetContact(0);
        ObjectPooler.Instance.SpawnParticle("LaserImpactFX", transform.position, Quaternion.LookRotation(contact.normal));
    }

    void Explode()
    {
        if (!gameObject.activeSelf) return;

        gameObject.SetActive(false);

        rb.detectCollisions = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;
    }
}
