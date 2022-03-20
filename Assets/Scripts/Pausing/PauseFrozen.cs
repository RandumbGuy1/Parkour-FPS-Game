using UnityEngine;

public class PauseFrozen : MonoBehaviour
{
    void Awake() => RigidbodyManager.Instance.AddToManager(GetComponent<Rigidbody>());
}
