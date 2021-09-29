using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float kickForce;
    [SerializeField] private float standingDistance;
    [SerializeField] private LayerMask Kicks;
    [Space(10)]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;

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

        rb.AddForce((transform.forward + dirTo).normalized * kickForce * 1.3f, ForceMode.VelocityChange);
        rb.AddForce(Vector3.up * kickForce * 0.3f, ForceMode.VelocityChange);
    }
}
