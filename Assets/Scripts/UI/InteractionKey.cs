using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractionKey : MonoBehaviour
{
    [SerializeField] private ScriptManager s;
    private TextMeshProUGUI key;

    private void Awake()
    {
        key = GetComponent<TextMeshProUGUI>();

        key.text = s.PlayerInput.interactKey.ToString();
    }
}
