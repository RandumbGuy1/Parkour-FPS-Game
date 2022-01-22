using UnityEngine;
using UnityEngine.AI;

public interface IPathFinding
{
    NavMeshAgent Agent { get; }
    Transform Target { get; }

    void MoveToTarget();
}
