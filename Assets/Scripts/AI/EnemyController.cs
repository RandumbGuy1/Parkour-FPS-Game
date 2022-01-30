using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour, IEnemy
{
    [Header("Assignables")]
    [SerializeField] private string spawnedPieces = "ShatteredBarry";
    [SerializeField] private PlayerHealth enemyHealth;
    private IPathFinding enemyPathFinding;
    private PlayerHealth targetHealth;

    public PlayerHealth EnemyHealth { get { return enemyHealth; } }

    void Awake()
    {
        enemyHealth.OnPlayerStateChanged += OnPlayerStateChanged;

        enemyPathFinding = GetComponent<IPathFinding>();
        if (enemyPathFinding == null) return;

        targetHealth = enemyPathFinding.Target.GetComponent<PlayerHealth>();
        if (targetHealth == null) return;
        targetHealth.OnPlayerStateChanged += DisableEnemyOnTargetDeath;
    }

    void OnDestroy()
    {
        enemyHealth.OnPlayerStateChanged -= OnPlayerStateChanged;

        if (targetHealth == null) return;
        targetHealth.OnPlayerStateChanged -= DisableEnemyOnTargetDeath;
    }

    void FixedUpdate()
    {
        if (enemyPathFinding == null) return;
        enemyPathFinding.MoveToTarget();
    }

    private void DisableEnemyOnTargetDeath(UnitState newState) => enabled = (newState == UnitState.Alive);

    private void OnPlayerStateChanged(UnitState newState)
    {
        if (newState != UnitState.Dead) return;

        enabled = false;
        if (enemyPathFinding != null && enemyPathFinding.Agent != null) enemyPathFinding.Agent.enabled = false;

        ObjectPooler.Instance.Spawn(spawnedPieces, enemyHealth.transform.position, enemyHealth.transform.rotation);
    }

    public void EnemyUpdate() { }
}
