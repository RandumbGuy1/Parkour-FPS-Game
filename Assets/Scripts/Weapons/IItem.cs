using UnityEngine;

public interface IItem
{
    ItemArtSettings SpriteSettings { get; }
    HoldingSettings SwaySettings { get; }
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
    [SerializeField] private float sprintMulti;
    [SerializeField] private float weight;

    public float Weight => weight;

    public Vector3 DefaultPos => defaultPos;
    public Vector3 DefaultRot => defaultRot;

    public Vector3 AimPos => aimPos;
    public Vector3 AimRot => aimRot;

    public Vector3 SprintOffsetPos => sprintOffsetPos;
    public Vector3 SprintOffsetRot => sprintOffsetRot;
    public float SprintMulti => sprintMulti;
}

[System.Serializable]
public struct ItemArtSettings
{
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private Vector3 scale;
    [SerializeField] private Vector3 rotation;
    [SerializeField] private Vector3 position;

    public Sprite ItemSprite => itemSprite;
    public Vector3 Scale => scale;
    public Vector3 Rotation => rotation;
    public Vector3 Position => position;
}
