using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    WeaponClass weaponType { get; }

    bool automatic { get; }
    float recoilSmoothTime { get; }
    float reloadSmoothTime { get; }
    ShakeData recoilShakeData { get; }

    bool OnAttack(ScriptManager s);
    bool SecondaryAction(ScriptManager s);
}

public enum WeaponClass
{
    Ranged,
    Melee,
}
