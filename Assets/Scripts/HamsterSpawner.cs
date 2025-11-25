using System;
using System.Collections;
using UnityEngine;

public class HamsterSpawner : MonoBehaviour
{
    public float spawnFrequency;
    public Hamster hamsterPrefab;
    public GameObject hamsterSpawnPosition;

    public void SpawnHamsters()
    {
        StartCoroutine(Spawn());
    }

    private IEnumerator Spawn()
    {
        // Spawn 3 hamsters at spawn position
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(spawnFrequency);
            Instantiate(hamsterPrefab, hamsterSpawnPosition.transform.position, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
