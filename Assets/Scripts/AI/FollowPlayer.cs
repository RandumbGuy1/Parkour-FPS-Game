using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float speed;
    [SerializeField] private float angularSpeed;
    [SerializeField] private float kickForce;
    [SerializeField] private float standingDistance;
    [SerializeField] private LayerMask Kicks;
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
        agent.speed = speed + 2.5f;

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

        if (sqrEnemyToPlayer < standingDistance * standingDistance)
        {
            agent.SetDestination(enemyRb.transform.position);
            return;
        }

        if (sqrAgentToEnemy * 1.5f > sqrAgentToPlayer || enemyRb.velocity.sqrMagnitude < 3f * 3f && sqrAgentToEnemy > 15f * 15f)
        {
             agent.transform.position = enemyRb.transform.position - (player.position - enemyRb.transform.position).normalized * 5f;
             agent.SetDestination(player.position);
             return;
        }
        else if (sqrEnemyToPlayer * 1.1f < sqrAgentToPlayer)
        {
            agent.transform.position = enemyRb.transform.position;
            agent.SetDestination(player.position);
            return;
        }

        float angle = Vector3.Angle(enemyRb.transform.up, Vector3.up);

        if (angle > 15f)
        {
            Quaternion fromTo = Quaternion.FromToRotation(enemyRb.transform.up, Vector3.up);
            enemyRb.AddTorque(new Vector3(fromTo.x, fromTo.y, fromTo.z) * 100f, ForceMode.Acceleration);
        }

        if (!grounded && angle >= 90f) return;

        agent.SetDestination(player.position);

        Vector3 pathDir = (agent.transform.position - enemyRb.transform.position).normalized;
        pathDir.y *= 0.1f;

        enemyRb.AddForce(pathDir * speed * 5f, ForceMode.Acceleration);
        enemyRb.AddForce(Vector3.down * 85f, ForceMode.Acceleration);

        Vector3 vel = enemyRb.velocity;
        vel.y = 0f;

        Quaternion lookAtPlayer = Quaternion.FromToRotation(enemyRb.transform.forward, vel.normalized);
        enemyRb.AddTorque(Vector3.up * lookAtPlayer.y * 65f, ForceMode.Acceleration);

        if (angle < 15f && Vector3.Angle(enemyRb.transform.forward, vel.normalized) < 5f) enemyRb.angularDrag = 3f;
        else enemyRb.angularDrag = startAngularDrag;
    }

    private void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Kicks != (Kicks | 1 << layer)) return;

        Vector3 dirTo = (col.transform.position - enemyRb.transform.position).normalized;
        dirTo.y = 0f;

        if (Vector3.Dot(dirTo, enemyRb.transform.forward) < 0.6f) return;
 
        Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();

        if (rb == null) return;

        rb.AddForce((enemyRb.transform.forward + dirTo).normalized * kickForce * 1.3f, ForceMode.VelocityChange);
        rb.AddForce(Vector3.up * kickForce * 0.3f, ForceMode.VelocityChange);
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
