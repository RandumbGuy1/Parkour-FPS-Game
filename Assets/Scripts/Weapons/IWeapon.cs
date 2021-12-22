using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    WeaponClass WeaponType { get; }

    bool Automatic { get; }
    float RecoilSmoothTime { get; }
    float ReloadSmoothTime { get; }
    ShakeData RecoilShakeData { get; }

    bool OnAttack();
    bool SecondaryAction();
}

public enum WeaponClass
{
    Ranged,
    Melee,
}
