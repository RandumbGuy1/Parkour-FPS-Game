using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptManager : MonoBehaviour
{
    [Header("Player Scripts")]
    public PlayerMovement PlayerMovement;
    public PlayerInput PlayerInput;
    public WeaponController WeaponControls;

    [Header("Camera Scripts")]
    public CameraFollow CameraLook;
    public CameraBobbing CameraHeadBob;
    public CameraShaker CameraShaker;

    [Header("Assignables")]
    public Transform orientation;
    public Transform playerHead;
    public Transform groundCheck;
    public Transform cam;
    public Rigidbody rb;
}
