using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HamsterSpawner : PlaceableObject
{
    public float spawnFrequency;
    public Hamster hamsterPrefab;
    public GameObject hamsterSpawnPosition;
    private List<Hamster> spawnedHamsters = new List<Hamster>();

    public void SpawnHamsters()
    {
        StartCoroutine(Spawn());
    }

    internal void DespawnHamsters()
    {
        foreach (var hamster in spawnedHamsters) {
            hamster.StopAllCoroutines();
            Destroy(hamster.gameObject);
        }
        spawnedHamsters.Clear();
    }

    private IEnumerator Spawn()
    {
        // Spawn 3 hamsters at spawn position
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(spawnFrequency);
            if (HamsterManager.Instance.CanSpawnHamsters()) {
                spawnedHamsters.Add(Instantiate(hamsterPrefab, hamsterSpawnPosition.transform.position, Quaternion.identity));
                HamsterManager.Instance.OnHamsterSpawned();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
