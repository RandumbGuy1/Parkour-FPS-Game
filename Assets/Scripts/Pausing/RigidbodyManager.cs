using System.Collections.Generic;
using UnityEngine;

public class RigidbodyManager : MonoBehaviour
{
    [Header("Rigidbodies In Scene")]
    [SerializeField] private List<Rigidbody> allRigidbodies = new List<Rigidbody>();
    private readonly List<AffectedBody> affectedBodies = new List<AffectedBody>();
    public static RigidbodyManager Instance { get; private set; }

    void Awake()
    {
        RegisterRigidbodies();
        Instance = this;
    }

    private void RegisterRigidbodies()
    {
        int syncLists = allRigidbodies.Count - affectedBodies.Count;
        if (syncLists != 0) 
            for (int i = Mathf.Max(0, affectedBodies.Count - 1); i < syncLists; i++) 
                affectedBodies.Add(new AffectedBody(allRigidbodies[i]));

        for (int i = 0; i < affectedBodies.Count; i++) affectedBodies[i].RegisterState();
    }

    public void FreezeAll(bool freeze = true, bool reapplyVelocity = true)
    {
        if (freeze) RegisterRigidbodies();

        for (int i = 0; i < allRigidbodies.Count; i++)
        {
            if (!allRigidbodies[i].gameObject.activeInHierarchy) return;
            affectedBodies[i].Freeze(freeze, reapplyVelocity);
        }
    }

    public void FreezeOne(Rigidbody rb, bool freeze = true, bool reapplyVelocity = true)
    {
        if (!allRigidbodies.Contains(rb)) return;
        if (freeze) RegisterRigidbodies();

        int i = allRigidbodies.IndexOf(rb);
        affectedBodies[i].Freeze(freeze, reapplyVelocity);
    }

    public void AddToManager(Rigidbody rb)
    {
        allRigidbodies.Add(rb);
        RegisterRigidbodies();
    }
}

public class AffectedBody
{
    private readonly Rigidbody rb;

    private Vector3 velocity;
    private Vector3 angularVelocity;

    private CollisionDetectionMode collisionDetectionMode;
    private RigidbodyInterpolation interpolation;

    public AffectedBody(Rigidbody rb)
    {
        this.rb = rb;
        RegisterState();
    }

    public void RegisterState()
    {
        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;

        collisionDetectionMode = rb.collisionDetectionMode;
        interpolation = rb.interpolation;
    }

    public void Freeze(bool freeze = true, bool reapplyVelocity = true)
    {
        if (freeze)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
            return;
        }

        rb.isKinematic = false;
        rb.collisionDetectionMode = collisionDetectionMode;
        rb.interpolation = interpolation;

        if (reapplyVelocity)
        {
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
        }
    }
}
