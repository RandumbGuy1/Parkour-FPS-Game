using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseFrozen : MonoBehaviour
{
    void Awake() => RigidbodyManager.Instance.AddToManager(GetComponent<Rigidbody>());
}
