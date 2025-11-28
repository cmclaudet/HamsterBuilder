
using UnityEngine;

public class HamsterManager : MonoBehaviour {
    public static HamsterManager Instance;
    private int spawnedHamsterCount;
    private int maxSpawnedHamsters = 200;

    void Awake() {
        Instance = this;
    }

    public void OnHamsterSpawned() {
        spawnedHamsterCount++;
    }

    public bool CanSpawnHamsters() {
        return spawnedHamsterCount < maxSpawnedHamsters;
    }

    public void ResetSpawnedHamsters() {
        spawnedHamsterCount = 0;
    }
}