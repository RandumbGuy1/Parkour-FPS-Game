using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptManager : MonoBehaviour
{
    [Header("Player Scripts")]
    public PlayerMovement PlayerMovement;
    public InputManager PlayerInput;
    public WeaponController WeaponControls;

    [Header("Camera Scripts")]
    public CameraFollow CameraInput;
    public CameraEffects CameraLandBob;
    public HeadBobbing CameraHeadBob;
    public CameraShaker CameraShaker;

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
