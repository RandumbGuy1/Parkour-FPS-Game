using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DroneFollow : MonoBehaviour, IPathFinding
{
    [Header("Follow Settings")]
    [SerializeField] private float speed;
    [SerializeField] private float standingDistance;
    [SerializeField] private float shootingDistance;

    [Header("Drone Settings")]
    [SerializeField] private List<Vector3> hoverDirections = new List<Vector3>();
    [SerializeField] private LayerMask Ground;
    [SerializeField] private float hoverMaxSpring;
    [SerializeField] private float hoverForce;
    [SerializeField] private float hoverDampening;
    [SerializeField] private float hoverDistance;
    private float[] lastHitDistances;
    private Vector3[] hoverOffsets;
    private float idleSwayTick = 0f;

    [Header("Assignables")]
    [SerializeField] private PlayerHealth enemyHealth;
    [SerializeField] private EnemyShoot shooting;
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody enemyRb;
    [SerializeField] private Collider enemyCol;

    public Transform Target { get { return target; } }
    public NavMeshAgent Agent { get { return null; } }

    void Awake()
    {
        hoverOffsets = new Vector3[hoverDirections.Count];
        lastHitDistances = new float[hoverOffsets.Length];

        if (enemyHealth != null) enemyHealth.OnPlayerDamage += WeakenDrone;
    }

    private void WeakenDrone(float damage)
    {
        if (damage < 0) return;

        hoverForce = Mathf.Clamp(hoverForce - damage * 10f, 0.1f, Mathf.Infinity);
        hoverMaxSpring = Mathf.Clamp(hoverMaxSpring - damage * 20f, 5f, Mathf.Infinity);
    }

    public void MoveToTarget()
    {
        HandleHover();

        Vector3 enemyToPlayer = target.position - enemyRb.transform.position;
        enemyToPlayer.y *= 0.05f;
        float sqrEnemyToPlayer = enemyToPlayer.sqrMagnitude;

        float angle = Vector3.Angle(enemyRb.transform.up, Vector3.up);
        if (angle > 90f) return;

        Vector3 pathDir = GetNextPathPos() - enemyRb.transform.position;
        pathDir.y = 0f;

        if (sqrEnemyToPlayer < shootingDistance * shootingDistance && shooting != null) shooting.OnAttack(Target);
        if (sqrEnemyToPlayer < standingDistance * standingDistance && Vector3.Dot(pathDir.normalized, (target.position - enemyRb.transform.position).normalized) > -0.9f)
            return;

        enemyRb.AddForce(5f * speed * pathDir.normalized, ForceMode.Acceleration);
    }

    private float GetHoverForce(Vector3 point, Vector3 dir, int index)
    {
        if (!Physics.Raycast(point, dir, out var hit, hoverDistance, Ground))
        {
            lastHitDistances[index] = hoverDistance * 1.1f;
            return 0f;
        }

        float forceAmount = hoverForce * (hoverDistance - hit.distance) + (hoverDampening * (lastHitDistances[index] - hit.distance));
        forceAmount = Mathf.Clamp(forceAmount, 0f, hoverMaxSpring);
        lastHitDistances[index] = hit.distance;
        
        float idleWaves = 0.3f * Mathf.Sin(idleSwayTick);

        return forceAmount + idleWaves;
    }

    private Vector3 GetNextPathPos()
    {
        return target.position;
    }

    private void HandleHover()
    {
        idleSwayTick += Time.fixedDeltaTime * 4f;

        for (int i = 0; i < hoverDirections.Count; i++)
        {
            Vector3 hoverPoint = enemyRb.transform.position + enemyRb.transform.TransformDirection(hoverDirections[i]);
            hoverOffsets[i] = hoverPoint;

            enemyRb.AddForceAtPosition(GetHoverForce(hoverPoint, -enemyRb.transform.up, i) * enemyRb.transform.up, hoverPoint, ForceMode.Acceleration);
        }

        Quaternion targetRot = Quaternion.FromToRotation(enemyRb.transform.up, Vector3.up);
        enemyRb.AddTorque(0.2f * hoverForce * new Vector3(targetRot.x, targetRot.y, targetRot.z) - (enemyRb.angularVelocity * 0.1f), ForceMode.VelocityChange);
    }

    void OnDrawGizmosSelected()
    {
        if (hoverOffsets == null) return;
        for (int i = 0; i < hoverOffsets.Length; i++) Gizmos.DrawWireSphere(hoverOffsets[i], 0.1f);
    }
}
