using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class Hamster : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float lookDistance = 10f;
    public float moveDelay = 2f;
    public GridManager gridManager;
    
    private Vector2Int gridSize = new(1, 1);
    private List<Vector2Int> currentPath;
    private int currentPathIndex = 0;
    private Vector3 targetWorldPosition;
    private bool hasTarget = false;
    private bool isMoving = false;
    
    void Start()
    {
        gridManager = FindObjectsByType<GridManager>(FindObjectsSortMode.None).First();
        // Find a target entry point
        StartCoroutine(FindAndSetTarget());
    }
    
    void Update()
    {
        if (isMoving && hasTarget)
        {
            MoveAlongPath();
        }
    }
    
    private IEnumerator FindAndSetTarget()
    {
        yield return new WaitForSeconds(moveDelay);
        // Find all PlaceableObjects in the scene
        PlaceableObject[] allObjects = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);
        
        // Get hamster's current grid position
        Vector2Int hamsterGridPos = gridManager.WorldToGrid(transform.position);
        
        // List to store valid entry points with their paths
        List<(Vector3 worldPos, List<Vector2Int> path)> validEntryPoints = new List<(Vector3, List<Vector2Int>)>();
        
        foreach (PlaceableObject obj in allObjects)
        {
            Vector3[] entryPoints = obj.GetEntryPoints();
            
            foreach (Vector3 entryPoint in entryPoints)
            {
                // Check distance
                float distance = Vector3.Distance(transform.position, entryPoint);
                if (distance > lookDistance)
                    continue;
                
                // Convert to grid position
                Vector2Int entryGridPos = gridManager.WorldToGrid(entryPoint);
                
                // Check if entry point cell is occupied
                if (gridManager.IsCellOccupied(entryGridPos))
                    continue;
                
                // Try to find a path to this entry point
                List<Vector2Int> path = gridManager.FindPath(hamsterGridPos, entryGridPos);
                
                if (path != null && path.Count > 0)
                {
                    validEntryPoints.Add((entryPoint, path));
                }
            }
        }
        
        // If we have valid entry points, pick one at random
        if (validEntryPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, validEntryPoints.Count);
            var chosen = validEntryPoints[randomIndex];
            
            currentPath = chosen.path;
            currentPathIndex = 0;
            targetWorldPosition = chosen.worldPos;
            hasTarget = true;
            isMoving = true;
            
            Debug.Log($"Hamster found {validEntryPoints.Count} valid entry points, chose one at {targetWorldPosition}");
        }
        else
        {
            Debug.Log("Hamster found no valid entry points within range");
            hasTarget = false;
            isMoving = false;
        }
    }
    
    private void MoveAlongPath()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count)
        {
            // Reached the end of the path
            isMoving = false;
            Debug.Log("Hamster reached target entry point!");
            return;
        }
        
        // Get the current target grid cell
        Vector2Int targetGridPos = currentPath[currentPathIndex];
        Vector3 targetCellWorldPos = gridManager.GridToWorldCenter(targetGridPos);
        targetCellWorldPos.y = transform.position.y; // Keep hamster's Y position
        
        // Move towards the target cell
        Vector3 direction = (targetCellWorldPos - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        // Check if we've reached the current waypoint
        float distanceToWaypoint = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(targetCellWorldPos.x, 0, targetCellWorldPos.z)
        );
        
        if (distanceToWaypoint < 0.1f)
        {
            // Move to next waypoint
            currentPathIndex++;
        }
    }
}