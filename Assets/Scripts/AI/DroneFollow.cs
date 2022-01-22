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
    [SerializeField] private LayerMask Ground;
    [SerializeField] private float hoverForce;
    [SerializeField] private float hoverRadius;
    [SerializeField] private float hoverDistance;
    private float idleSwayTick = 0f;

    [Header("Assignables")]
    [SerializeField] private EnemyShoot shooting;
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody enemyRb;

    public Transform Target { get { return target; } }
    public NavMeshAgent Agent { get { return null; } }

    public void MoveToTarget()
    {
        HandleRotation();
        idleSwayTick += Time.fixedDeltaTime * 4f;

        Vector3 hoverDir = GetHoverPos() - enemyRb.transform.position;
        if (hoverDir.y < 0f) hoverDir.y *= 0.6f;

        enemyRb.AddForce(hoverForce * hoverDir + Vector3.down * 10f, ForceMode.Acceleration);

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

    private Vector3 GetHoverPos()
    {
        Physics.SphereCast(enemyRb.transform.position, hoverRadius, Vector3.down, out var hit, Ground);

        return hit.point += Vector3.up * hoverDistance + (0.3f * Mathf.Sin(idleSwayTick) * Vector3.up);
    }

    private Vector3 GetNextPathPos()
    {
        return target.position;
    }

    private void HandleRotation()
    {
        float angle = Vector3.Angle(enemyRb.transform.up, Vector3.up);
        if (angle < 5f) return;

        Quaternion fromTo = Quaternion.FromToRotation(enemyRb.transform.up, Vector3.up);
        Vector3 upRight = new Vector3(fromTo.x, fromTo.y, fromTo.z);

        enemyRb.AddTorque(upRight * angularSpeed, ForceMode.Impulse);            
    }
}
