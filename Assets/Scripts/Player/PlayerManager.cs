using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Scripts")]
    public PlayerHealth PlayerHealth;
    public PlayerController PlayerController;
    public PlayerMovement PlayerMovement;
    public PlayerInput PlayerInput;
    public PlayerInteraction PlayerInteraction;
    public WeaponController WeaponControls;

    [Header("Camera Scripts")]
    public CameraFollow CameraLook;
    public CameraBobbing CameraHeadBob;
    public CameraShaker CameraShaker;

    [Header("Assignables")]
    public Transform orientation;
    public Transform playerHead;
    public Camera cam;
    public Rigidbody rb;
    public CapsuleCollider cc;

    public Vector3 BottomCapsuleSphereOrigin { get { return transform.position - Vector3.up * (cc.height - cc.radius); } }

    void Awake() => Application.targetFrameRate = 80;
    
    void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

        PlayerHealth.OnPlayerStateChanged += OnPlayerStateChanged;
        PlayerHealth.OnPlayerDamage += OnPlayerDamage;
    }

    void OnDisable()
    {
        GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;

        PlayerHealth.OnPlayerStateChanged -= OnPlayerStateChanged;
        PlayerHealth.OnPlayerDamage -= OnPlayerDamage;
    }

    public void OnPlayerDamage(float damage)
    {
        CameraLook.OnPlayerDamage(damage);
    }

    public void OnGameStateChanged(GameState newState)
    {
        RigidbodyManager.Instance.FreezeAll(newState == GameState.Paused);

        PlayerMovement.OnGameStateChanged(newState);
        CameraLook.OnGameStateChanged(newState);
        WeaponControls.OnGameStateChanged(newState);
    }

    public void OnPlayerStateChanged(UnitState newState)
    {
        PlayerMovement.OnPlayerStateChanged(newState);
        WeaponControls.OnPlayerStateChanged(newState);
        PlayerInteraction.OnPlayerStateChanged(newState);
        CameraLook.OnPlayerStateChanged(newState);
    }
}
