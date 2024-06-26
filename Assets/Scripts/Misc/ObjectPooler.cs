﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public static ObjectPooler Instance { get; private set; }
    [SerializeField] private List<Pool> pools;
    [SerializeField] private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject poolSend = Instantiate(pool.prefab);
                poolSend.SetActive(false);
                poolSend.transform.SetParent(transform);
                objectPool.Enqueue(poolSend);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        GameObject spawnedObject = poolDictionary[tag].Dequeue();
        spawnedObject.SetActive(false);

        spawnedObject.transform.SetPositionAndRotation(position, rotation);
        spawnedObject.SetActive(true);

        poolDictionary[tag].Enqueue(spawnedObject);

        return spawnedObject;
    }
}
