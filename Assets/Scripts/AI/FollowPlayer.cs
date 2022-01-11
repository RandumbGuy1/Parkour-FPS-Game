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
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody enemyRb;
    private ScriptManager s;

    public NavMeshAgent Agent { get { return agent; } }

    void Awake()
    {
        agent.speed = 0;
        CancelInvoke("SetGroundCheckInterval");
        SetGroundCheckInterval();

        s = player.GetComponent<ScriptManager>();
        if (s != null) s.PlayerHealth.OnPlayerStateChanged += OnPlayerStateChanged;
    }

    void OnEnable()
    {
        CancelInvoke("SetGroundCheckInterval");
        SetGroundCheckInterval();
    }

    void FixedUpdate() => FollowThePlayer();

    private void FollowThePlayer()
    {
        HandleRotation();

        if (!grounded)
        {
            enemyRb.AddForce(gravityForce * enemyRb.drag * enemyRb.mass * Vector3.down, ForceMode.Acceleration);

            if (!agent.isOnOffMeshLink)
            {
                agent.enabled = false;
                enemyRb.AddForce(0.5f * speed * (player.position - enemyRb.transform.position).normalized, ForceMode.Acceleration);
            }

            return;
        }

        Vector3 enemyToPlayer = player.position - enemyRb.transform.position;
        enemyToPlayer.y *= 0.5f;

        float sqrEnemyToPlayer = enemyToPlayer.sqrMagnitude;
        Vector3 pathDir = GetNextPathPos() - enemyRb.transform.position;

        if (sqrEnemyToPlayer < standingDistance * standingDistance && Vector3.Dot(pathDir.normalized, (player.position - enemyRb.transform.position).normalized) > 0.1f && !Physics.Linecast(enemyRb.transform.position, player.position, Ground))
        {
            if (shooting != null) shooting.OnAttack();
            return;
        }

        enemyRb.AddForce(5f * speed * pathDir.normalized, ForceMode.Acceleration);
    }

    private Vector3 GetNextPathPos()
    {
        agent.enabled = true;

        bool offNavmesh = agent.isOnOffMeshLink;

        agent.nextPosition = offNavmesh ? agent.currentOffMeshLinkData.endPos : enemyRb.position;
        agent.SetDestination(player.position);
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

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(enemyRb.position + enemyRb.transform.TransformDirection(groundCheckOffset), groundCheckHalfExtents * 2f);
    }

    private void SetGroundCheckInterval()
    {
        Quaternion orientation = Quaternion.LookRotation(enemyRb.transform.up);
        grounded = Physics.CheckBox(enemyRb.position + enemyRb.transform.TransformDirection(groundCheckOffset), groundCheckHalfExtents, orientation, Ground);

        Invoke(nameof(SetGroundCheckInterval), groundCheckInterval);
    }

    private void OnPlayerStateChanged(PlayerState newState) => enabled = newState == PlayerState.Alive;
}
