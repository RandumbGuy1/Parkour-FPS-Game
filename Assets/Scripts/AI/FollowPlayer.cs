using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour, IPathFinding
{
    [Header("Follow Settings")]
    [SerializeField] private float speed;
    [SerializeField] private float standingDistance;
    [SerializeField] private float shootingDistance;
    [SerializeField] private float gravityForce;

    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask Ground;
    [SerializeField] private Vector3 groundCheckHalfExtents;
    [SerializeField] private Vector3 groundCheckOffset;
    [SerializeField] private float groundCheckInterval;
    private bool grounded;

    [Header("Assignables")]
    [SerializeField] private EnemyShoot shooting;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody enemyRb;

    public Transform Target { get { return target; } }
    public NavMeshAgent Agent { get { return agent; } }

    void Awake()
    {
        agent.speed = 0;
        CancelInvoke("SetGroundCheckInterval");
        SetGroundCheckInterval();
    }

    void OnEnable()
    {
        CancelInvoke("SetGroundCheckInterval");
        SetGroundCheckInterval();
    }

    public void MoveToTarget()
    {
        HandleRotation();

        if (!grounded)
        {
            enemyRb.AddForce(gravityForce * enemyRb.drag * enemyRb.mass * Vector3.down, ForceMode.Acceleration);

            if (!agent.isOnOffMeshLink)
            {
                agent.enabled = false;
                enemyRb.AddForce(0.5f * speed * (target.position - enemyRb.transform.position).normalized, ForceMode.Acceleration);
            }

            return;
        }

        Vector3 enemyToPlayer = target.position - enemyRb.transform.position;
        enemyToPlayer.y *= 0.5f;

        float sqrEnemyToPlayer = enemyToPlayer.sqrMagnitude;
        Vector3 pathDir = GetNextPathPos() - enemyRb.transform.position;

        if (sqrEnemyToPlayer < shootingDistance * shootingDistance && shooting != null) shooting.OnAttack(Target);
        if (sqrEnemyToPlayer < standingDistance * standingDistance && Vector3.Dot(pathDir.normalized, (target.position - enemyRb.transform.position).normalized) > 0.1f && !Physics.Linecast(enemyRb.transform.position, target.position, Ground))         
            return;

        enemyRb.AddForce(5f * speed * pathDir.normalized, ForceMode.Acceleration);
    }

    private Vector3 GetNextPathPos()
    {
        agent.enabled = true;

        bool offNavmesh = agent.isOnOffMeshLink;

        agent.nextPosition = offNavmesh ? agent.currentOffMeshLinkData.endPos : enemyRb.position;
        agent.SetDestination(target.position);
        Vector3 pos = agent.steeringTarget;

        if (offNavmesh) agent.enabled = false;

        return pos;
    }

    private void HandleRotation()
    {
        float angle = Vector3.Angle(enemyRb.transform.up, Vector3.up);

        if (angle > 15f)
        {
            Quaternion fromTo = Quaternion.FromToRotation(enemyRb.transform.up, Vector3.up);
            enemyRb.AddTorque(new Vector3(fromTo.x, fromTo.y, fromTo.z) * 100f, ForceMode.Acceleration);
        }
    }

    private void SetGroundCheckInterval()
    {
        Quaternion orientation = Quaternion.LookRotation(enemyRb.transform.up);
        grounded = Physics.CheckBox(enemyRb.position + enemyRb.transform.TransformDirection(groundCheckOffset), groundCheckHalfExtents, orientation, Ground);

        Invoke(nameof(SetGroundCheckInterval), groundCheckInterval);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(enemyRb.position + enemyRb.transform.TransformDirection(groundCheckOffset), groundCheckHalfExtents * 2f);
    }
}
