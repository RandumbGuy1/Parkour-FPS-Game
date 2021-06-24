using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float lifeTime;

    void OnEnable()
    {
        Invoke("Explode", lifeTime);
    }

    void OnCollisionEnter()
    {
        Invoke("Explode", 0.01f);
    }

    void Explode()
    {
        gameObject.SetActive(false);
    }
}
