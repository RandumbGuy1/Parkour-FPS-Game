using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerInput : MonoBehaviour
{
    public Vector2 input { get { return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); } }
   
    public bool jumping { get { return Input.GetKeyDown(jumpKey); } }
    public bool crouching { get { return Input.GetKey(crouchKey); } }
    public bool interacting { get { return Input.GetKeyDown(interactKey); } }
    public bool dropping { get { return Input.GetKeyDown(dropKey); } }
    public bool reloading { get { return Input.GetKeyDown(reloadKey); } }

    public bool middleClick { get { return Input.GetMouseButtonDown(2); } }
    public bool rightClick { get { return Input.GetMouseButtonDown(1); } }
    public bool leftClick { get { return Input.GetMouseButtonDown(0); } }
    public bool leftHoldClick { get { return Input.GetMouseButton(0); } }

    [Header("KeyBinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.Q;
    public KeyCode reloadKey = KeyCode.R;

    [Header("Assignables")]
    private ScriptManager s;

    void Awake() => s = GetComponent<ScriptManager>();

    void Update() => s.PlayerMovement.SetInput(input, jumping, crouching);
}
