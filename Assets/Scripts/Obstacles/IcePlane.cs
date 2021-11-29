using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePlane : MonoBehaviour
{
    [Header("Ice Settings")]
    [SerializeField] private float iceFrictionMultiplier;

    private void OnCollisionEnter(Collision col)
    {
        ScriptManager s = col.gameObject.GetComponent<ScriptManager>();
        if (s == null) return;

        ContactPoint contact = col.GetContact(0);
        if (Vector3.Dot(-contact.normal, transform.up) < 0.9f) return;

        s.PlayerMovement.SetFrictionMultiplier(iceFrictionMultiplier);
    }

    private void OnCollisionExit(Collision col)
    {
        ScriptManager s = col.gameObject.GetComponent<ScriptManager>();
        if (s == null) return;

        s.PlayerMovement.SetFrictionMultiplier();
    }
}
