using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float speed;
    [SerializeField] private float angularSpeed;
    [SerializeField] private float standingDistance;
    [SerializeField] private LayerMask Environment;
    [Space(10)]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody enemyRb;

    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask Ground;
    [SerializeField] private Vector3 groundCheckHalfExtents;
    [SerializeField] private Vector3 groundCheckOffset;
    [SerializeField] private float groundCheckInterval;
    private bool grounded;

    private float startAngularDrag = 0f;

    private void Start()
    {
        startAngularDrag = enemyRb.angularDrag;
        agent.speed = speed + speed * 0.1f;

        CancelInvoke("SetGroundCheckInterval");
        SetGroundCheckInterval();
    }

    private void OnEnable()
    {
        CancelInvoke("SetGroundCheckInterval");
        SetGroundCheckInterval();
    }

    void FixedUpdate()
    {
        FollowThePlayer();
    }

    void FollowThePlayer()
    {
        if (enemyRb.transform.parent != transform) enemyRb.transform.SetParent(transform);

        float sqrEnemyToPlayer = (player.position - enemyRb.transform.position).sqrMagnitude;
        float sqrAgentToPlayer = (player.position - agent.transform.position).sqrMagnitude;
        float sqrAgentToEnemy = (enemyRb.transform.position - agent.transform.position).sqrMagnitude;

        HandleRotation();

        enemyRb.AddForce(Vector3.down * 85f, ForceMode.Acceleration);

        if (sqrEnemyToPlayer < standingDistance * standingDistance)
        {
            agent.SetDestination(enemyRb.transform.position);
            return;
        }

        /*
        if (sqrAgentToEnemy > sqrAgentToPlayer || sqrAgentToEnemy > sqrEnemyToPlayer)
        {
            agent.transform.position = enemyRb.transform.position;
            agent.SetDestination(player.position);
            return;
        }
        */

        /*
        if (sqrAgentToEnemy * 1.5f > sqrEnemyToPlayer || enemyRb.velocity.sqrMagnitude < 5f * 5f && sqrAgentToEnemy > 10f * 10f)
        {
             agent.transform.position = enemyRb.transform.position - (player.position - enemyRb.transform.position).normalized * 8f;
             agent.SetDestination(player.position);
             return;
        }
        else if (sqrEnemyToPlayer < sqrAgentToPlayer * 1.05f && (sqrEnemyToPlayer < 25f * 25f || sqrAgentToPlayer < 25f * 25f))
        {
            enemyRb.AddForce((player.position - enemyRb.transform.position).normalized * speed * 5f, ForceMode.Acceleration);
            agent.SetDestination(player.position);
            return;
        }
        */

        if (!grounded) return;

        agent.SetDestination(player.position);

        Vector3 pathDir = ((sqrEnemyToPlayer < sqrAgentToPlayer && !Physics.Linecast(player.position, enemyRb.transform.position, Environment) 
            ? player.position : agent.transform.position) - enemyRb.transform.position).normalized;

        pathDir.y *= 0.1f;

        enemyRb.AddForce(pathDir * speed * 5f, ForceMode.Acceleration);
    }

    private void HandleRotation()
    {
        float angle = Vector3.Angle(enemyRb.transform.up, Vector3.up);

        if (angle > 15f)
        {
            Quaternion fromTo = Quaternion.FromToRotation(enemyRb.transform.up, Vector3.up);
            enemyRb.AddTorque(new Vector3(fromTo.x, fromTo.y, fromTo.z) * 100f, ForceMode.Acceleration);
        }

        Vector3 vel = (player.position - enemyRb.transform.position);
        vel.y = 0f;

        /*
        float angle2 = Vector3.Angle(enemyRb.transform.forward, vel.normalized);
        float lookAtPlayer = Vector3.SignedAngle(enemyRb.transform.forward, vel.normalized, Vector3.up);

        if (angle2 < 5f)
        {
            if (angle < 15f) enemyRb.angularDrag = 5f;
            return;
        }

        enemyRb.angularDrag = startAngularDrag;
        enemyRb.AddTorque(Vector3.up * lookAtPlayer * 60f, ForceMode.Acceleration);
        */
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(enemyRb.position + enemyRb.transform.TransformDirection(groundCheckOffset), groundCheckHalfExtents * 2f);
    }

    private void SetGroundCheckInterval()
    {
        Quaternion orientation = Quaternion.LookRotation(enemyRb.transform.up);

        grounded = Physics.CheckBox(enemyRb.position + enemyRb.transform.TransformDirection(groundCheckOffset), groundCheckHalfExtents, orientation, Ground);

        Invoke("SetGroundCheckInterval", groundCheckInterval);
    }
}
