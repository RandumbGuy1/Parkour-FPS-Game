using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Assignables")]
    [SerializeField] private PlayerHealth enemyHealth;
    [SerializeField] private Transform shatteredEnemy;

    void Awake() => enemyHealth.OnPlayerStateChanged += OnPlayerStateChanged;
    void OnDestroy() => enemyHealth.OnPlayerStateChanged -= OnPlayerStateChanged;

    private void OnPlayerStateChanged(PlayerState newState)
    {
        if (newState != PlayerState.Dead) return;

        enabled = false;

        shatteredEnemy.position = enemyHealth.transform.position;
        shatteredEnemy.gameObject.SetActive(true);

        for (int i = 0; i < shatteredEnemy.childCount; i++)
        {
            Rigidbody rb = shatteredEnemy.GetChild(i).GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.AddExplosionForce(12f, enemyHealth.transform.position, 10f, 0.3f, ForceMode.Impulse);
        }
    }
}
