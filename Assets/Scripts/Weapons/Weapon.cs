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

    public abstract bool OnAttack(Transform cam);
    public abstract void SecondaryAction();
}
