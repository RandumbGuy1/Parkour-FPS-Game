using System.Collections.Generic;
using UnityEngine;

public class RigidbodyManager : MonoBehaviour
{
    [Header("Rigidbodies In Scene")]
    [SerializeField] private List<Rigidbody> allRigidbodies = new List<Rigidbody>();
    [SerializeField] private bool allFrozen = false;
    private readonly Dictionary<Rigidbody, AffectedBody> affectedBodies = new Dictionary<Rigidbody, AffectedBody>();
    public static RigidbodyManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;

        AffectedBody.SetDictionary(affectedBodies);

        for (int i = 0; i < allRigidbodies.Count; i++)
        {
            if (allRigidbodies[i] == null) continue;
            affectedBodies.Add(allRigidbodies[i], new AffectedBody(allRigidbodies[i]));
        }
    }

    private void RegisterRigidbodies()
    {
        for (int i = 0; i < allRigidbodies.Count; i++) affectedBodies[allRigidbodies[i]].RegisterState();
    }

    public void FreezeAll(bool freeze = true, bool reapplyVelocity = true)
    {
        allFrozen = freeze;
        if (allFrozen) RegisterRigidbodies();

        for (int i = 0; i < allRigidbodies.Count; i++)
        {
            if (!allRigidbodies[i].gameObject.activeInHierarchy) return;
            affectedBodies[allRigidbodies[i]].Freeze(freeze, reapplyVelocity);
        }
    }

    public void FreezeOne(Rigidbody rb, bool freeze = true, bool reapplyVelocity = true)
    {
        if (!affectedBodies.ContainsKey(rb))
        {
            affectedBodies.Add(rb, new AffectedBody(rb, true));
            FreezeOne(rb, freeze, reapplyVelocity);
            return;
        }

        if (freeze) affectedBodies[rb].RegisterState();
        affectedBodies[rb].Freeze(freeze, reapplyVelocity);
    }

    public void AddToManager(Rigidbody rb)
    {
        if (rb == null || allRigidbodies.Contains(rb)) return;

        allRigidbodies.Add(rb);
        affectedBodies.Add(rb, new AffectedBody(rb));

        if (allFrozen) FreezeOne(rb);
    }
}

[System.Serializable]
public class AffectedBody
{
    private static Dictionary<Rigidbody, AffectedBody> affectedBodies;
    private readonly Rigidbody rb;

    private Vector3 velocity;
    private Vector3 angularVelocity;

    private CollisionDetectionMode collisionDetectionMode;
    private RigidbodyInterpolation interpolation;
    private RigidbodyConstraints constraints;
    private bool isKinematic;

    private int avoidFreezeThisGen = 0;
    private readonly bool removeAfterFreeze;

    public AffectedBody(Rigidbody rb, bool removeAfterFreeze = false)
    {
        this.removeAfterFreeze = removeAfterFreeze;
        this.rb = rb;
        RegisterState();
    }

    public static void SetDictionary(Dictionary<Rigidbody, AffectedBody> affectedBodies)
    {
        AffectedBody.affectedBodies = affectedBodies;
    }

    public void RegisterState()
    {
        if (rb.isKinematic)
        {
            avoidFreezeThisGen = 2;
            return;
        }

        isKinematic = rb.isKinematic;
        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;

        collisionDetectionMode = rb.collisionDetectionMode;
        interpolation = rb.interpolation;
        constraints = rb.constraints;
    }

    public void Freeze(bool freeze = true, bool reapplyVelocity = true)
    {
        if (avoidFreezeThisGen > 0)
        {
            avoidFreezeThisGen--;
            return;
        }

        if (freeze)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            return;
        }

        rb.constraints = constraints;
        rb.isKinematic = false;
        rb.collisionDetectionMode = collisionDetectionMode;
        rb.interpolation = interpolation;
        rb.isKinematic = isKinematic;

        if (reapplyVelocity)
        {
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
        }

        if (removeAfterFreeze) affectedBodies.Remove(rb);
    }
}
