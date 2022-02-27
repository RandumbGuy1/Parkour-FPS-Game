using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float boostForce;
    [SerializeField] private LayerMask Obstacle;

    private void OnCollisionEnter(Collision col)
    {
        PlayerManager s = col.gameObject.GetComponent<PlayerManager>();
        Rigidbody rb;

        if (s == null) rb = col.gameObject.GetComponent<Rigidbody>();
        else rb = s.rb;

        ContactPoint contact = col.GetContact(0);
        if (Vector3.Dot(-contact.normal, transform.up) < 0.9f) return;

        Vector3 boostDir = -contact.normal * boostForce * 4.5f;
        rb.velocity = new Vector3(rb.velocity.x * 1.25f, 0, rb.velocity.z * 1.25f);
        rb.AddForce(boostDir * 0.5f, ForceMode.VelocityChange);

        if (s == null) return;

        s.CameraShaker.ShakeOnce(new PerlinShake(ShakeData.Create(Mathf.Clamp(col.relativeVelocity.magnitude * 0.35f, 3f, 20f), 3f, 1.5f, 3.5f)));
    }
}
