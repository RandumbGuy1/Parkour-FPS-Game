﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamagable
{
    float MaxHealth { get; }
    float CurrentHealth { get; }

    void OnDamage(float damage, PlayerManager player = null);
    void OnDeath();
}
