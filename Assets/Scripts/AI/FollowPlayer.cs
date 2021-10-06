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

    void FixedUpdate()
    {
        FollowThePlayer();
    }

    void FollowThePlayer()
    {
        if ((player.position - enemyRb.transform.position).sqrMagnitude < standingDistance)
        {
            agent.SetDestination(agent.transform.position);
            return;
        }

        if ((player.position - enemyRb.transform.position).sqrMagnitude < (player.position - agent.transform.position).sqrMagnitude)
        {
            Vector3 vel = enemyRb.velocity * 0.1f;
            vel.y = 0f;

            agent.transform.position = enemyRb.transform.position + vel;
            agent.SetDestination(player.position);
            return;
        }

        agent.SetDestination(player.position);

        Vector3 pathDir = (agent.transform.position - enemyRb.transform.position).normalized;
        pathDir.y = 0f;

        enemyRb.AddForce(pathDir * speed * 10f, ForceMode.Acceleration);
        enemyRb.AddForce(Vector3.down * 65f);
        //enemyRb.AddTorque(pathDir * angularSpeed * 10f, ForceMode.Acceleration);

        //Quaternion targetRot = Quaternion.LookRotation(pathDir);

        //enemyRb.rotation = Quaternion.RotateTowards(enemyRb.rotation, targetRot, angularSpeed * Time.fixedDeltaTime * 20f);
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
}
