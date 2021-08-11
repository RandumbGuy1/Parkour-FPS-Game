using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float lifeTime;

    private Rigidbody rb;
    private TrailRenderer tr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tr = GetComponent<TrailRenderer>();
    }

    void OnEnable()
    {
        tr.Clear();

        rb.detectCollisions = true;

        CancelInvoke("Explode");
        Invoke("Explode", lifeTime);
    }

    void OnCollisionEnter(Collision col)
    {
        CancelInvoke("Explode");
        Invoke("Explode", 0.005f);

        ContactPoint contact = col.GetContact(0);
        ObjectPooler.Instance.SpawnParticle("LaserImpactFX", transform.position, Quaternion.LookRotation(contact.normal));
    }

    void Explode()
    {
        if (!gameObject.activeSelf) return;

        rb.detectCollisions = false;
        gameObject.SetActive(false);
    }
}
