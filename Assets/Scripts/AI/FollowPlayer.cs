using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float speed;
    [SerializeField] private float kickForce;
    [SerializeField] private LayerMask Kicks;
    [SerializeField] private float standingDistance;
    [Space(10)]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;

    private void Start()
    {
        agent.speed = speed;
    }

    void Update()
    {
        FollowThePlayer();
    }

    void FollowThePlayer()
    {
        if ((player.position - transform.position).sqrMagnitude < standingDistance * standingDistance)
        {
            agent.SetDestination(transform.position);
            return;
        }

        agent.SetDestination(player.position);
    }

    private void OnCollisionEnter(Collision col)
    {
        int layer = col.gameObject.layer;
        if (Kicks != (Kicks | 1 << layer)) return;

        Vector3 dirTo = (col.transform.position - transform.position).normalized;
        dirTo.y = 0f;

        if (Vector3.Dot(dirTo, transform.forward) < 0.4f) return;

        Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();

        if (rb == null) return;

        rb.AddForce((transform.forward + dirTo).normalized * kickForce, ForceMode.Impulse);
    }
}
