﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerInput : MonoBehaviour
{
    public Vector2 InputVector { get { return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); } }
   
    public bool Jumping { get { return Input.GetKeyDown(jumpKey); } }
    public bool Crouching { get { return Input.GetKey(crouchKey); } }
    public bool Interacting { get { return Input.GetKeyDown(interactKey); } }
    public bool Dropping { get { return Input.GetKeyDown(dropKey); } }
    public bool Reloading { get { return Input.GetKeyDown(reloadKey); } }

    public bool MiddleClick { get { return Input.GetMouseButtonDown(2); } }
    public bool RightClick { get { return Input.GetMouseButtonDown(1); } }
    public bool LeftClick { get { return Input.GetMouseButtonDown(0); } }
    public bool LeftHoldClick { get { return Input.GetMouseButton(0); } }

    [Header("KeyBinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.Q;
    public KeyCode reloadKey = KeyCode.R;

    [Header("Assignables")]
    private ScriptManager s;

    void Awake() => s = GetComponent<ScriptManager>();
}
