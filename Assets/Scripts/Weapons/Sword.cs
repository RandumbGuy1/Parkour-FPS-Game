using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour, IItem, IWeapon
{
    public WeaponClass weaponType { get { return type; } }
    public Sprite itemSprite { get { return weaponSprite; } }

    public bool automatic { get { return weaponAutomatic; } }

    public Vector3 defaultPos { get { return weaponDefaultPos; } }
    public Vector3 defaultRot { get { return weaponDefaultRot; } }

    public Vector3 aimPos { get { return weaponAimPos; } }
    public Vector3 aimRot { get { return weaponAimRot; } }

    public float weight { get { return weaponWeight; } }

    public float recoilSmoothTime { get { return swingSmoothing; } }
    public Vector3 recoilPosOffset { get { return swingPosOffset; } }
    public Vector3 recoilRotOffset { get { return swingRotOffset; } }

    public float recoilForce { get; }
    public float reloadSmoothTime { get; }
    public Vector3 reloadRotOffset { get; }

    [Header("Weapon Class")]
    [SerializeField] private WeaponClass type;
    [SerializeField] private bool weaponAutomatic;

    [Header("Weapon Artwork")]
    [SerializeField] private Sprite weaponSprite;

    [Header("Weapon Holding Settings")]
    [SerializeField] private Vector3 weaponDefaultPos;
    [SerializeField] private Vector3 weaponDefaultRot;
    [Space(10)]
    [SerializeField] private Vector3 weaponAimPos;
    [SerializeField] private Vector3 weaponAimRot;
    [Space(10)]
    [SerializeField] private float weaponWeight;

    [Header("Attack Settings")]
    [SerializeField] private float attackRadius;
    [SerializeField] private float swingForce;
    [SerializeField] private float hitboxActive;
    [SerializeField] private LayerMask AttackLayer;

    [Header("Swing Settings")]
    [SerializeField] private float swingSmoothing;
    [SerializeField] private Vector3 swingPosOffset;
    [SerializeField] private Vector3 swingRotOffset;

    [Header("Assignables")]
    [SerializeField] private Transform attackPoint;

    public bool OnAttack(Transform cam)
    {
        StopAllCoroutines();
        StartCoroutine(AttackHitBox(cam));

        return true;
    }

    private IEnumerator AttackHitBox(Transform cam)
    {
        float elapsed = 0f;

        while (elapsed < hitboxActive)
        {
            Collider[] colliders = Physics.OverlapCapsule(transform.position + transform.up, transform.position - transform.up, attackRadius, AttackLayer);

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody rb = colliders[i].GetComponent<Rigidbody>();

                if (rb != null)
                {
                    Vector3 dirToLaunch = ((colliders[i].transform.position - transform.position) + cam.forward * 2f).normalized;

                    rb.AddForce(dirToLaunch * swingForce, ForceMode.Impulse);
                    rb.AddTorque(dirToLaunch * swingForce, ForceMode.Impulse);
                }
            }

            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }
    }

    public bool SecondaryAction()
    {
        return true;
    }

    public string ReadData() => "holding a sword (no way)";

    public bool OnUse() => true;

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position + transform.up, attackRadius);
        Gizmos.DrawWireSphere(transform.position - transform.up, attackRadius);
    }
}
