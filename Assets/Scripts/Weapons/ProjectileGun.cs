using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileGun : Weapon
{
    public override void OnAttack()
    {
        print("shot something");
    }

    public override void SecondaryAction()
    {
        print("reloading");
    }
}
