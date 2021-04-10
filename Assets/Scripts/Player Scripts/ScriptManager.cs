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

    [Header("Assignables")]
    public Transform orientation;
    public Transform playerHead;
    public Transform groundCheck;
    public Transform cam;
    public Rigidbody rb;
}
