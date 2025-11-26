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
    
    private Vector3 frontFaceDirection = new(0, 0, 1);
    private Vector2Int gridSize = new(1, 1);
    private List<Vector2Int> currentPath;
    private int currentPathIndex = 0;
    private Vector3 targetWorldPosition;
    private bool hasTarget = false;
    private bool isMoving = false;
    private bool isInteracting = false;
    private int foodPieceCount;
    private PlaceableObject targetObject;
    
    void Start()
    {
        gridManager = FindObjectsByType<GridManager>(FindObjectsSortMode.None).First();
        // Find a target entry point
        StartCoroutine(FindAndSetTarget());
    }
    
    void Update()
    {
        if (isMoving && hasTarget && !isInteracting)
        {
            MoveAlongPath();
        }
    }

    public void AddFoodPiece() {
        foodPieceCount++;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void RotateToFace(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep rotation on horizontal plane
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void StartInteraction()
    {
        isInteracting = true;
    }

    public void EndInteraction()
    {
        isInteracting = false;
        hasTarget = false;
        StartCoroutine(FindAndSetTarget());
    }
    
    private IEnumerator FindAndSetTarget()
    {
        yield return new WaitForSeconds(moveDelay);
        // Find all PlaceableObjects in the scene
        PlaceableObject[] allObjects = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);
        
        // Get hamster's current grid position
        Vector2Int hamsterGridPos = gridManager.WorldToGrid(transform.position);
        
        // Dictionary to store objects with their valid entry points
        // Key: PlaceableObject, Value: List of (worldPos, path) tuples
        Dictionary<PlaceableObject, List<(Vector3 worldPos, List<Vector2Int> path)>> objectsWithValidEntryPoints = 
            new Dictionary<PlaceableObject, List<(Vector3, List<Vector2Int>)>>();
        
        foreach (PlaceableObject obj in allObjects)
        {
            Vector3[] entryPoints = obj.GetEntryPoints();
            List<(Vector3 worldPos, List<Vector2Int> path)> validEntryPointsForThisObject = 
                new List<(Vector3, List<Vector2Int>)>();
            
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
                    validEntryPointsForThisObject.Add((entryPoint, path));
                }
            }
            
            // Only add objects that have at least one valid entry point
            if (validEntryPointsForThisObject.Count > 0)
            {
                objectsWithValidEntryPoints[obj] = validEntryPointsForThisObject;
            }
        }
        
        // If we have objects with valid entry points, pick one object at random
        if (objectsWithValidEntryPoints.Count > 0)
        {
            // Randomly choose one object from all objects with valid entry points
            List<PlaceableObject> validObjects = new List<PlaceableObject>(objectsWithValidEntryPoints.Keys);
            int randomObjectIndex = Random.Range(0, validObjects.Count);
            PlaceableObject chosenObject = validObjects[randomObjectIndex];
            
            // Randomly choose one entry point from the chosen object's valid entry points
            List<(Vector3 worldPos, List<Vector2Int> path)> entryPointsForChosenObject = 
                objectsWithValidEntryPoints[chosenObject];
            int randomEntryIndex = Random.Range(0, entryPointsForChosenObject.Count);
            var chosenEntry = entryPointsForChosenObject[randomEntryIndex];
            
            currentPath = chosenEntry.path;
            currentPathIndex = 0;
            targetWorldPosition = chosenEntry.worldPos;
            targetObject = chosenObject;
            hasTarget = true;
            isMoving = true;
            
            Debug.Log($"Hamster found {objectsWithValidEntryPoints.Count} objects with valid entry points, chose object with {entryPointsForChosenObject.Count} entry points, selected entry at {targetWorldPosition}");
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
            
            // Trigger interaction with the target object
            if (targetObject != null)
            {
                targetObject.OnInteract(this);
            }
            return;
        }
        
        // Get the current target grid cell
        Vector2Int targetGridPos = currentPath[currentPathIndex];
        Vector3 targetCellWorldPos = gridManager.GridToWorldCenter(targetGridPos);
        targetCellWorldPos.y = transform.position.y; // Keep hamster's Y position
        
        // Move towards the target cell
        Vector3 direction = (targetCellWorldPos - transform.position).normalized;
        
        // Rotate to face the movement direction using frontFaceDirection (Y-axis only)
        if (direction != Vector3.zero)
        {
            direction.y = 0; // Keep rotation on horizontal plane
            direction.Normalize();
            SetFacingRotation(direction);
        }

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

    public void SetFacingRotation(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            // Transform frontFaceDirection from local space to world space
            Vector3 worldFrontFaceDirection = transform.rotation * frontFaceDirection.normalized;
            worldFrontFaceDirection.y = 0; // Keep on horizontal plane
            worldFrontFaceDirection.Normalize();

            // Calculate rotation to align frontFaceDirection with movement direction (Y-axis only)
            if (worldFrontFaceDirection != Vector3.zero)
            {
                // Calculate the angle between current forward and target direction in XZ plane
                float angle = Vector3.SignedAngle(worldFrontFaceDirection, direction, Vector3.up);

                // Create rotation around Y axis only
                Quaternion yRotation = Quaternion.Euler(0, angle, 0);
                transform.rotation = yRotation * transform.rotation;

                // Ensure rotation is constrained to Y axis by extracting only Y component
                Vector3 eulerAngles = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(0, eulerAngles.y, 0);
            }
        }
    }
}