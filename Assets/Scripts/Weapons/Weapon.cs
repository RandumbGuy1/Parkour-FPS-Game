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

    public enum AttackType
    {
        Automatic,
        NonAutomatic,
    }

    public WeaponClass weaponType;
    public AttackType attackType;

    public abstract bool OnAttack(Transform cam);
    public abstract void SecondaryAction();
}
