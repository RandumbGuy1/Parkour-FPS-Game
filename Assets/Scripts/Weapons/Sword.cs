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

    public float recoilForce { get; }
    public float recoilSmoothTime { get; }
    public float reloadSmoothTime { get; }

    public Vector3 recoilPosOffset { get; }
    public Vector3 recoilRotOffset { get; }
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

    public bool OnAttack(Transform cam)
    {
        return true;
    }

    public bool SecondaryAction()
    {
        return true;
    }

    public string ReadData() => "holding a sword (no way)";

    public bool OnUse() => true;
}
