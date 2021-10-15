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

    private void Start()
    {
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

        if ((player.position - enemyRb.transform.position).sqrMagnitude < standingDistance * standingDistance)
        {
            agent.SetDestination(enemyRb.transform.position);
            return;
        }

        if ((player.position - enemyRb.transform.position).sqrMagnitude * 1.05f < (player.position - agent.transform.position).sqrMagnitude)
        {
            agent.transform.position = enemyRb.transform.position;
            agent.SetDestination(player.position);
            return;
        }

        if ((player.position - agent.transform.position).sqrMagnitude < agent.stoppingDistance * agent.stoppingDistance && (player.position - enemyRb.transform.position).sqrMagnitude > 125f)
        {
             agent.transform.position = enemyRb.transform.position;
             agent.SetDestination(player.position);
             return;
        }

        float angle = Vector3.Angle(enemyRb.transform.up, Vector3.up);

        if (angle > 15f)
        {
            Quaternion fromTo = Quaternion.FromToRotation(enemyRb.transform.up, Vector3.up);
            enemyRb.AddTorque(new Vector3(fromTo.x, fromTo.y, fromTo.z) * 95f, ForceMode.Acceleration);
        }

        if (!grounded && angle >= 90f) return;

        agent.SetDestination(player.position);

        Vector3 pathDir = (agent.transform.position - enemyRb.transform.position).normalized;
        pathDir.y *= 0.1f;

        enemyRb.AddForce(pathDir * speed * 5f, ForceMode.Acceleration);
        enemyRb.AddForce(Vector3.down * 85f, ForceMode.Acceleration);
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
