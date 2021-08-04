using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float boostForce;

    private bool recharged = true;

    private void OnCollisionEnter(Collision col)
    {
        Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();
        if (rb == null || !recharged) return;

        recharged = false;
        Invoke("ResetCharge", 0.5f);

        Vector3 vel = rb.velocity;
        vel.y = 0f;

        Vector3 boostDir = (vel.normalized + Vector3.up * 5f) * boostForce;

        rb.velocity = vel;
        rb.AddForce(boostDir, ForceMode.Impulse);
    }

    private void ResetCharge() => recharged = true;
}
