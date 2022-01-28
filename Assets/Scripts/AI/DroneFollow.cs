using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DroneFollow : MonoBehaviour, IPathFinding
{
    [Header("Follow Settings")]
    [SerializeField] private float speed;
    [SerializeField] private float angularSpeed;
    [SerializeField] private float standingDistance;
    [SerializeField] private float shootingDistance;

    [Header("Drone Settings")]
    [SerializeField] private List<Vector3> hoverDirections = new List<Vector3>();
    [SerializeField] private LayerMask Ground;
    [SerializeField] private float hoverForce;
    [SerializeField] private float hoverRadius;
    [SerializeField] private float hoverDistance;
    private float idleSwayTick = 0f;
    private Vector3[] hoverOffsets;

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
        //for (int i = 0; i < hoverDirections.Count; i++) hoverOffsets[i] = 

        //if (enemyHealth != null) enemyHealth.OnPlayerDamage += WeakenDrone;
    }

    private void WeakenDrone(float damage)
    {
        if (damage < 0) return;

        hoverForce = Mathf.Clamp(hoverForce - damage * 10f, 0.1f, Mathf.Infinity);
        angularSpeed = Mathf.Clamp(angularSpeed - damage, 0.5f, Mathf.Infinity);
    }

    public void MoveToTarget()
    {
        HandleHover();

        Vector3 enemyToPlayer = target.position - enemyRb.transform.position;
        enemyToPlayer.y *= 0.05f;
        float sqrEnemyToPlayer = enemyToPlayer.sqrMagnitude;

        Vector3 pathDir = GetNextPathPos() - enemyRb.transform.position;
        pathDir.y = 0f;

        if (sqrEnemyToPlayer < shootingDistance * shootingDistance && shooting != null) shooting.OnAttack(Target);
        if (sqrEnemyToPlayer < standingDistance * standingDistance && Vector3.Dot(pathDir.normalized, (target.position - enemyRb.transform.position).normalized) > 0.1f)
            return;

        enemyRb.AddForce(5f * speed * pathDir.normalized, ForceMode.Acceleration);
    }

    private float GetHoverForce(Vector3 point, Vector3 dir)
    {
        Physics.Raycast(point, dir, out var hit, Ground);

        //return hit.point += Vector3.up * hoverDistance + (0.3f * Mathf.Sin(idleSwayTick) * Vector3.up);

        float hoverElevation = hit.point.y + hoverDistance;
        return Mathf.Clamp(hoverElevation - point.y/*+ (0.3f * Mathf.Sin(idleSwayTick))*/, 0f, 1.2f);
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
            Vector3 hoverPoint = enemyCol.ClosestPoint(enemyRb.transform.position + enemyRb.transform.TransformDirection(hoverDirections[i]));
            hoverOffsets[i] = hoverPoint;

            float upRight = enemyRb.transform.position.y - hoverPoint.y;

            enemyRb.AddForceAtPosition(hoverForce * 2f * GetHoverForce(hoverPoint, -enemyRb.transform.up) * (enemyRb.transform.up + Vector3.up * upRight).normalized
                + Vector3.down * 10f, hoverPoint, ForceMode.Acceleration);
        }
    }

    void OnDrawGizmos()
    {
        if (hoverOffsets == null) return;
        for (int i = 0; i < hoverOffsets.Length; i++) Gizmos.DrawWireSphere(hoverOffsets[i], 0.1f);
    }

    private void HandleRotation(Vector3 headingDir)
    {
        float angle = Vector3.Angle(enemyRb.transform.up, Vector3.up);
        if (angle < 5f) return;

        //Quaternion targetRot = Quaternion.LookRotation(headingDir);
        //enemyRb.MoveRotation(Quaternion.Slerp(enemyRb.rotation, targetRot, angularSpeed * Time.fixedDeltaTime * 1f));
    }
}
