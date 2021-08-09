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

    public bool rightClick { get { return Input.GetMouseButtonDown(1); } }
    public bool leftClick { get { return Input.GetMouseButtonDown(0); } }
    public bool leftHoldClick { get { return Input.GetMouseButton(0); } }

    [Header("KeyBinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    [Header("Assignables")]
    private ScriptManager s;

    void Awake() => s = GetComponent<ScriptManager>();

    void Update() => s.PlayerMovement.SetInput(input, jumping, crouching);
}
