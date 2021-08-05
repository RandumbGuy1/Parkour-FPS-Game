using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float lifeTime;

    void OnEnable() => Invoke("Explode", lifeTime);

    void OnCollisionEnter(Collision col)
    {
        ObjectPooler.Instance.SpawnParticle("LaserImpactFX", transform.position, Quaternion.LookRotation(col.GetContact(0).normal));

        Invoke("Explode", 0.01f);
    }

    void Explode() => gameObject.SetActive(false);
}
