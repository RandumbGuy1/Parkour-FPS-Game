using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public enum WeaponClass
    {
        Ranged,
        Melee,
    }

    public WeaponClass weaponType;
    public bool automatic;

    public float recoilForce;

    public Vector3 defaultPos;
    public Vector3 defaultRot;

    public float bobAmountHoriz;
    public float bobAmountVert;
    public float swayAmount;

    public abstract bool OnAttack(Transform cam);
    public abstract bool SecondaryAction();
}
