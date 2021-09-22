using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestShake : MonoBehaviour
{
    [SerializeField] private CameraShaker shaker;
    private Vector3 orignalPos;

    void Awake()
    {
        orignalPos = transform.position;
    }

    void LateUpdate()
    {
        transform.position = orignalPos + shaker.Offset;
    }
}
