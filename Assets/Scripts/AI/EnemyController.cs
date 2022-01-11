using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Assignables")]
    [SerializeField] private PlayerHealth enemyHealth;
    [SerializeField] private FollowPlayer enemyPathFinding;

    void Awake() => enemyHealth.OnPlayerStateChanged += OnPlayerStateChanged;
    void OnDestroy() => enemyHealth.OnPlayerStateChanged -= OnPlayerStateChanged;

    private void OnPlayerStateChanged(PlayerState newState)
    {
        if (newState != PlayerState.Dead) return;

        enabled = false;
        enemyPathFinding.Agent.enabled = false;
        enemyPathFinding.enabled = false;

        ObjectPooler.Instance.Spawn("ShatteredBarry", enemyHealth.transform.position, enemyHealth.transform.rotation);
    }
}
