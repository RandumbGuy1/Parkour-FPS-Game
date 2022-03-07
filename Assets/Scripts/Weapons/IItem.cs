using UnityEngine;

public interface IItem
{
    HoldingSettings SwaySettings { get; }
    Sprite ItemSprite { get; }
    PlayerManager Player { get; }

    string ReadData();
    string ReadName();

    void OnPickup(PlayerManager s);
    void OnDrop();
    void ItemUpdate();
}

[System.Serializable]
public struct HoldingSettings
{
    [SerializeField] private Vector3 defaultPos;
    [SerializeField] private Vector3 defaultRot;
    [Space(10)]
    [SerializeField] private Vector3 aimPos;
    [SerializeField] private Vector3 aimRot;
    [Space(10)]
    [SerializeField] private Vector3 sprintOffsetPos;
    [SerializeField] private Vector3 sprintOffsetRot;
    [SerializeField] private float sprintOffsetPosMulti;
    [SerializeField] private float sprintOffsetRotMulti;
    [SerializeField] private float weight;

    public float Weight => weight;

    public Vector3 DefaultPos => defaultPos;
    public Vector3 DefaultRot => defaultRot;

    public Vector3 AimPos => aimPos;
    public Vector3 AimRot => aimRot;

    public Vector3 SprintOffsetPos => sprintOffsetPos;
    public Vector3 SprintOffsetRot => sprintOffsetRot;
    public float SprintOffsetPosMulti => sprintOffsetPosMulti;
    public float SprintOffsetRotMulti => sprintOffsetRotMulti;
}
