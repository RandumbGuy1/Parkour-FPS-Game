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

    private void Start()
    {
        agent.speed = 0;
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

        agent.gameObject.SetActive(true);

        agent.transform.localPosition = Vector3.zero;
        agent.transform.localRotation = Quaternion.Euler(Vector3.zero);

        float sqrEnemyToPlayer = (player.position - enemyRb.transform.position).sqrMagnitude;
        float sqrAgentToPlayer = (player.position - agent.transform.position).sqrMagnitude;

        HandleRotation();

        enemyRb.AddForce(Vector3.down * 85f, ForceMode.Acceleration);

        if (!grounded) return;
        if (sqrEnemyToPlayer < standingDistance * standingDistance) return;

        agent.SetDestination(player.position);

        Vector3 pathDir = (agent.path.corners.Length > 1 && !agent.isOnOffMeshLink ? agent.path.corners[1] : player.transform.position) - enemyRb.transform.position;
      
        for (int i = 0; i < agent.path.corners.Length - 1; i++)
        {
            Debug.DrawLine(agent.path.corners[i], agent.path.corners[i + 1], Color.red);
        }

        Debug.DrawRay(enemyRb.transform.position, pathDir.normalized * 3f, Color.red);

        agent.isStopped = true;
        enemyRb.AddForce(pathDir.normalized * speed * 5f, ForceMode.Acceleration);

        if (agent.isOnOffMeshLink) agent.gameObject.SetActive(false);
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
