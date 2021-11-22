using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float boostForce;
    [SerializeField] private LayerMask Obstacle;
    Vector3 idk;
    private void OnCollisionEnter(Collision col)
    {
        ScriptManager s = col.gameObject.GetComponent<ScriptManager>();
        Rigidbody rb;

        if (s == null) rb = col.gameObject.GetComponent<Rigidbody>();
        else rb = s.rb;

        ContactPoint contact = col.GetContact(0);
        Vector3 dir = (col.transform.position - transform.position).normalized;
        dir.y = 0f;

        Vector3 normal = (Physics.Raycast(contact.point + Vector3.up * 0.5f + dir * 0.2f, Vector3.down, out var hit, 3f, Obstacle) ? hit.normal : Vector3.zero);

        if (normal == Vector3.zero) return;

        Vector3 boostDir = normal * boostForce * 4f;
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(boostDir * 0.5f, ForceMode.VelocityChange);

        if (s == null) return;

        s.CameraShaker.ShakeOnce(col.relativeVelocity.magnitude * 0.6f, 4f, 1.5f, 4f, ShakeData.ShakeType.Perlin);
        idk = contact.point + Vector3.up * 0.5f + dir * 0.2f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(idk, 0.1f);
    }
}
