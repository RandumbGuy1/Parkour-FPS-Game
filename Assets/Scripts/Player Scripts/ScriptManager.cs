using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptManager : MonoBehaviour
{
    [Header("Scripts")]
    public PlayerMovement PlayerMovement;
    public InputManager PlayerInput;
    public CameraFollow CamInput;
    public CameraEffects Effects;
    public HeadBobbing MoveCamera;
    public CameraShaker Shake;

    [Header("Assignables")]
    public Transform orientation;
    public Transform playerHead;
    public Transform groundCheck;
    public Transform cam;
    public Rigidbody rb;

    public float magnitude { get; private set; }
    public Vector3 velocity { get; private set; }

    void FixedUpdate()
    {
        magnitude = rb.velocity.magnitude;
        velocity = rb.velocity;
    }
}
