using System.Collections;
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

    public static ObjectPooler Instance;
    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake() => Instance = this;

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

        spawnedObject.transform.position = position;
        spawnedObject.transform.rotation = rotation;
        spawnedObject.SetActive(true);

        poolDictionary[tag].Enqueue(spawnedObject);

        return spawnedObject;
    }

    public ParticleSystem SpawnParticle(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        GameObject spawnedObject = poolDictionary[tag].Dequeue();

        spawnedObject.SetActive(true);
        spawnedObject.transform.position = position;
        spawnedObject.transform.rotation = rotation;

        poolDictionary[tag].Enqueue(spawnedObject);

        ParticleSystem particle = spawnedObject.GetComponent<ParticleSystem>();
        if (particle == null) return null;

        particle.Play();

        return particle;
    }
}
