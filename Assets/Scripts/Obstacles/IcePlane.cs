using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePlane : MonoBehaviour
{
    [Header("Ice Settings")]
    [SerializeField] private float iceFrictionMultiplier;
    [SerializeField] private float iceDragMultiplier;

    private void OnCollisionEnter(Collision col)
    {
        PlayerManager s = col.gameObject.GetComponent<PlayerManager>();
        if (s == null) 
        {
            Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.drag *= iceDragMultiplier;
            return;
        }

        ContactPoint contact = col.GetContact(0);
        if (Vector3.Dot(-contact.normal, transform.up) < 0.9f) return;

        s.PlayerMovement.SetFrictionMultiplier(iceFrictionMultiplier);
    }

    private void OnCollisionExit(Collision col)
    {
        PlayerManager s = col.gameObject.GetComponent<PlayerManager>();
        if (s == null)
        {
            Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.drag /= iceDragMultiplier;
            return;
        }

        s.PlayerMovement.SetFrictionMultiplier();
    }
}
