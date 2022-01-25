using UnityEngine;

public class Hitbox : MonoBehaviour, IDamagable
{
    [Header("Hitbox Settings")]
    [SerializeField] private HitboxZone hitboxType;
    [SerializeField] private float zoneMultiplier;

    [Header("Assignables")]
    [SerializeField] private PlayerHealth health;
     
    public float MaxHealth => 0f;
    public float CurrentHealth => 0f;

    public void OnDamage(float damage, ScriptManager player = null)
    {
        health.OnDamage(damage * EvaluateMultiplier() * zoneMultiplier, player);
    }

    private float EvaluateMultiplier()
    {
        switch (hitboxType)
        {
            case HitboxZone.body: return 0.9f;
            case HitboxZone.head: return 1.2f;
            case HitboxZone.limbs:return 0.7f;
            default: return 0f;
        }
    }

    public void OnDeath() { }
}

public enum HitboxZone
{
    head,
    body,
    limbs
};
