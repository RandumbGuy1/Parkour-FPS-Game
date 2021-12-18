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

    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask Ground;
    [SerializeField] private Vector3 groundCheckHalfExtents;
    [SerializeField] private Vector3 groundCheckOffset;
    [SerializeField] private float groundCheckInterval;
    private bool grounded;

    [Header("Assignables")]
    [SerializeField] private EnemyShoot shooting;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody enemyRb;

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
        HandleRotation();
        enemyRb.AddForce(80f * enemyRb.mass * Vector3.down, ForceMode.Acceleration);

        if (enemyRb.transform.parent != transform) enemyRb.transform.SetParent(transform);

        if (agent.path.corners.Length > 0 && agent.isOnOffMeshLink)
        {
            agent.nextPosition = agent.currentOffMeshLinkData.endPos;
            enemyRb.AddForce(5f * speed * (agent.transform.position - enemyRb.transform.position).normalized, ForceMode.Acceleration);
            agent.gameObject.SetActive(false);
        }

        agent.transform.localPosition = Vector3.zero;
        agent.transform.localRotation = Quaternion.Euler(Vector3.zero);
        agent.nextPosition = enemyRb.transform.position;

        if (!grounded)
        {
            agent.gameObject.SetActive(false);
            enemyRb.AddForce(0.5f * speed * (player.position - enemyRb.transform.position).normalized, ForceMode.Acceleration);
            return;
        }

        float sqrEnemyToPlayer = (player.position - enemyRb.transform.position).sqrMagnitude;
        agent.gameObject.SetActive(true);
        agent.SetDestination(player.position);

        Vector3 pathDir = (agent.path.corners.Length > 1 ? agent.path.corners[1] : player.transform.position) - enemyRb.transform.position;
        if (sqrEnemyToPlayer < standingDistance * standingDistance && Vector3.Dot(pathDir.normalized, (player.position - enemyRb.transform.position).normalized) > 0.5f && !Physics.Linecast(enemyRb.transform.position, player.position, Ground))
        {
            if (shooting != null) shooting.OnAttack(player.GetComponent<ScriptManager>());
            return;
        } 

        enemyRb.AddForce(5f * speed * pathDir.normalized, ForceMode.Acceleration);
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

    void OnDrawGizmos()
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
