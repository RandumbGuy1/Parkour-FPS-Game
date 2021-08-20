using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float boostForce;

    private void OnCollisionEnter(Collision col)
    {
        Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 dir = (transform.position - col.transform.position).normalized;
        dir.y = 0f;

        Vector3 boostDir = (dir + Vector3.up * 5f) * boostForce;

        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(boostDir * 0.5f, ForceMode.VelocityChange);
    }
}
