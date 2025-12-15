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
    public float chillOutProbability = 0.4f;
    public float chillOutLookDistance = 3f;
    public float chillOutMinDuration = 4f;
    public float chillOutMaxDuration = 10f;
    
    private Vector3 frontFaceDirection = new(0, 0, 1);
    private Vector2Int gridSize = new(1, 1);
    private List<Vector2Int> currentPath;
    private int currentPathIndex = 0;
    private Vector3 targetWorldPosition;
    private bool hasTarget = false;
    private bool isMoving = false;
    private bool isInteracting = false;
    private bool isGoingToChillOut = false;
    private bool isChillingOut = false;
    public int foodPieceCount;
    private PlaceableObject targetObject;
    private Dictionary<ObjectType, float> lastInteractionTime = new Dictionary<ObjectType, float>();
    
    // Tube traversal state
    private bool isInTube = false;
    private List<Vector3> tubePath = null;
    private int tubePathIndex = 0;
    private Tube currentTube = null;
    private bool isBacktracking = false;
    private Vector3 originalEntryPoint = Vector3.zero;
    private Tube originalTube = null;
    private List<Vector3> originalTubePath = null; // Store original path for backtracking
    private bool isExitingToEntryPoint = false; // Flag to indicate we're moving to an entry point to exit
    
    void Start()
    {
        gridManager = FindObjectsByType<GridManager>(FindObjectsSortMode.None).First();
        // Find a target entry point
        StartCoroutine(FindAndSetTarget());
    }
    
    void Update()
    {
        if (isInTube)
        {
            MoveThroughTube();
        }
        else if (isMoving && hasTarget && !isInteracting && !isChillingOut)
        {
            MoveAlongPath();
        }
    }

    public void AddFoodPiece() {
        foodPieceCount++;
    }

    public int GetFoodPieceCount() {
        return foodPieceCount;
    }

    public void RemoveFoodPiece() {
        if (foodPieceCount > 0) {
            foodPieceCount--;
        }
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
        if (targetObject != null) {
            // Record the interaction time for this object type
            lastInteractionTime[targetObject.objectType] = Time.time;
            targetObject.OnInteractEnd(this);
            targetObject = null;
        }
        
        // If we were in a tube, exit tube state
        if (isInTube)
        {
            ExitTube();
        }
        
        StartCoroutine(FindAndSetTarget());
    }
    
    /// <summary>
    /// Starts tube traversal with the given waypoint path
    /// </summary>
    public void StartTubeTraversal(List<Vector3> path, Tube tube, bool isBacktracking = false, Vector3 entryPoint = default)
    {
        isInTube = true;
        tubePath = path;
        tubePathIndex = 0;
        currentTube = tube;
        this.isBacktracking = isBacktracking;
        originalEntryPoint = entryPoint != default ? entryPoint : transform.position;
        originalTube = tube;
        originalTubePath = new List<Vector3>(path); // Store a copy for backtracking
        isMoving = true;
        
        Debug.Log($"Hamster starting tube traversal with {path.Count} waypoints (backtracking: {isBacktracking})");
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log($"  Waypoint {i}: {path[i]}");
        }
    }
    
    /// <summary>
    /// Moves the hamster through the tube using waypoint-based movement
    /// </summary>
    private void MoveThroughTube()
    {
        if (tubePath == null || tubePathIndex >= tubePath.Count)
        {
            // Reached the end of the tube path
            if (isExitingToEntryPoint)
            {
                // We've completed moving to an entry point - fully exit
                CompleteTubeExit();
                return;
            }
            
            ExitTube();
            return;
        }
        
        // Get the current target waypoint
        Vector3 targetWaypoint = tubePath[tubePathIndex];
        
        // Move towards the target waypoint
        Vector3 direction = (targetWaypoint - transform.position).normalized;
        
        // Rotate to face the movement direction
        if (direction != Vector3.zero)
        {
            direction.y = 0; // Keep rotation on horizontal plane
            direction.Normalize();
            SetFacingRotation(direction);
        }
        
        // Move towards waypoint
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        // Check if we've reached the current waypoint
        float distanceToWaypoint = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(targetWaypoint.x, 0, targetWaypoint.z)
        );
        
        if (distanceToWaypoint < 0.1f)
        {
            // Move to next waypoint
            tubePathIndex++;
        }
    }
    
    /// <summary>
    /// Exits tube traversal state and resumes normal behavior
    /// </summary>
    private void ExitTube()
    {
        // Determine the final tube and exit connection point
        Vector3 exitConnectionPoint = transform.position; // Current position should be at the exit connection
        Tube finalTube = FindTubeAtConnectionPoint(exitConnectionPoint);
        
        if (finalTube != null && originalTube != null)
        {
            // Determine the opposite entry point of the FINAL tube (the last tube in the path)
            // We need to find which entry point of the final tube corresponds to where they entered it
            // The exit connection point tells us which connection they're exiting from
            Vector3 oppositeEntryPoint = GetOppositeEntryPoint(finalTube, exitConnectionPoint);
            
            Debug.Log($"Hamster exiting: finalTube={finalTube.gameObject.name}, exitConnectionPoint={exitConnectionPoint}, oppositeEntryPoint={oppositeEntryPoint}");
            
            // Check if the opposite entry point of the final tube is blocked (has a connected tube)
            bool isOppositeEntryBlocked = IsEntryPointBlocked(finalTube, oppositeEntryPoint);
            
            if (!isOppositeEntryBlocked)
            {
                // Opposite entry point is available - move to it
                Debug.Log($"Hamster exiting tube: moving to opposite entry point at {oppositeEntryPoint}");
                List<Vector3> exitPath = new List<Vector3> { exitConnectionPoint, oppositeEntryPoint };
                tubePath = exitPath;
                tubePathIndex = 0;
                isExitingToEntryPoint = true;
                // Stay in tube state until we reach the entry point
                return;
            }
            else
            {
                // Opposite entry point is blocked - backtrack to original entry point
                Debug.Log($"Hamster exiting tube: opposite entry blocked, backtracking to original entry at {originalEntryPoint}");
                
                // Build backtrack path: reverse the entire path we took
                List<Vector3> backtrackPath = new List<Vector3>();
                
                if (originalTubePath != null && originalTubePath.Count > 0)
                {
                    // Start from current exit connection point
                    backtrackPath.Add(exitConnectionPoint);
                    
                    // Reverse the original path we stored
                    List<Vector3> reversedPath = new List<Vector3>(originalTubePath);
                    reversedPath.Reverse();
                    
                    // Add the reversed path (skip the first point if it's the same as exitConnectionPoint)
                    int startIndex = 0;
                    if (reversedPath.Count > 0 && Vector3.Distance(reversedPath[0], exitConnectionPoint) < 0.1f)
                    {
                        startIndex = 1;
                    }
                    
                    for (int i = startIndex; i < reversedPath.Count; i++)
                    {
                        backtrackPath.Add(reversedPath[i]);
                    }
                    
                    // Ensure we end at the original entry point (not the connection)
                    if (backtrackPath.Count > 0)
                    {
                        Vector3 lastPoint = backtrackPath[backtrackPath.Count - 1];
                        if (Vector3.Distance(lastPoint, originalEntryPoint) > 0.1f)
                        {
                            backtrackPath.Add(originalEntryPoint);
                        }
                        else
                        {
                            // Replace last point with actual entry point to ensure we're exactly there
                            backtrackPath[backtrackPath.Count - 1] = originalEntryPoint;
                        }
                    }
                }
                else
                {
                    // Fallback: if we don't have the original path, try to go back through the original tube
                    // Build path from exit connection back through original tube
                    List<Vector3> reverseTubePath = originalTube.GetPathFromEntry(originalEntryPoint);
                    reverseTubePath.Reverse();
                    
                    backtrackPath.Add(exitConnectionPoint);
                    // If exit connection is not the same as the last point of reverse path, add the reverse path
                    if (reverseTubePath.Count > 0 && Vector3.Distance(exitConnectionPoint, reverseTubePath[reverseTubePath.Count - 1]) > 0.1f)
                    {
                        backtrackPath.AddRange(reverseTubePath);
                    }
                    backtrackPath.Add(originalEntryPoint);
                }
                
                tubePath = backtrackPath;
                tubePathIndex = 0;
                isBacktracking = true;
                isExitingToEntryPoint = true; // We're backtracking to an entry point
                Debug.Log($"Backtrack path has {backtrackPath.Count} waypoints");
                return;
            }
        }
        
        // Fallback: exit normally
        isInTube = false;
        tubePath = null;
        tubePathIndex = 0;
        isMoving = false;

        if (currentTube != null)
        {
            currentTube.OnInteractEnd(this);
            currentTube = null;
        }

        isBacktracking = false;
        originalEntryPoint = Vector3.zero;
        originalTube = null;
        originalTubePath = null;
        isExitingToEntryPoint = false;

        Debug.Log("Hamster exited tube system");

        // Check if there's an object with an entry point at the current grid cell
        PlaceableObject objectAtCurrentCell = FindObjectAtCurrentGridCell();

        if (objectAtCurrentCell != null)
        {
            // Found an object with an entry point at the same grid cell - interact with it immediately
            Debug.Log($"Hamster found object {objectAtCurrentCell.gameObject.name} at current grid cell after tube exit, interacting immediately");
            targetObject = objectAtCurrentCell;
            hasTarget = true;
            objectAtCurrentCell.OnInteract(this);
        }
        else
        {
            // Resume normal behavior - find a new target
            StartCoroutine(FindAndSetTarget());
        }
    }
    
    /// <summary>
    /// Completes the tube exit process after reaching an entry point
    /// </summary>
    private void CompleteTubeExit()
    {
        isInTube = false;
        tubePath = null;
        tubePathIndex = 0;
        isMoving = false;

        if (currentTube != null)
        {
            currentTube.OnInteractEnd(this);
            currentTube = null;
        }

        isBacktracking = false;
        originalEntryPoint = Vector3.zero;
        originalTube = null;
        originalTubePath = null;
        isExitingToEntryPoint = false;

        Debug.Log("Hamster completed tube exit at entry point");

        // Check if there's an object with an entry point at the current grid cell
        PlaceableObject objectAtCurrentCell = FindObjectAtCurrentGridCell();

        if (objectAtCurrentCell != null)
        {
            // Found an object with an entry point at the same grid cell - interact with it immediately
            Debug.Log($"Hamster found object {objectAtCurrentCell.gameObject.name} at current grid cell after tube exit, interacting immediately");
            targetObject = objectAtCurrentCell;
            hasTarget = true;
            objectAtCurrentCell.OnInteract(this);
        }
        else
        {
            // Resume normal behavior - find a new target
            StartCoroutine(FindAndSetTarget());
        }
    }

    /// <summary>
    /// Finds an object that has an entry point at the hamster's current grid cell
    /// Excludes Tube objects to prevent infinite tube interactions
    /// </summary>
    private PlaceableObject FindObjectAtCurrentGridCell()
    {
        Vector2Int currentGridPos = gridManager.WorldToGrid(transform.position);
        PlaceableObject[] allObjects = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);

        foreach (PlaceableObject obj in allObjects)
        {
            // Filter out Tube objects to prevent infinite tube interactions
            if (obj is Tube)
            {
                continue;
            }

            Vector3[] entryPoints = obj.GetEntryPoints();

            foreach (Vector3 entryPoint in entryPoints)
            {
                Vector2Int entryGridPos = gridManager.WorldToGrid(entryPoint);

                // Check if this entry point is at the same grid cell as the hamster
                if (entryGridPos == currentGridPos)
                {
                    // Also check that the entry point is close enough in world space
                    float distance = Vector3.Distance(transform.position, entryPoint);
                    if (distance < 0.5f) // Use a small threshold for world space distance
                    {
                        return obj;
                    }
                }
            }
        }

        return null;
    }
    
    /// <summary>
    /// Finds which tube a connection point belongs to
    /// </summary>
    private Tube FindTubeAtConnectionPoint(Vector3 connectionPoint)
    {
        Tube[] allTubes = FindObjectsByType<Tube>(FindObjectsSortMode.None);
        const float threshold = 0.1f;

        foreach (Tube tube in allTubes)
        {
            // Check all connections in the tube
            for (int i = 0; i < tube.Connections.Length; i++)
            {
                if (tube.Connections[i] != null)
                {
                    float dist = Vector3.Distance(connectionPoint, tube.Connections[i].transform.position);
                    if (dist < threshold)
                        return tube;
                }
            }
        }

        return null;
    }
    
    /// <summary>
    /// Gets the opposite entry point for a tube given an exit connection point
    /// </summary>
    private Vector3 GetOppositeEntryPoint(Tube tube, Vector3 exitConnectionPoint)
    {
        // Find which connection index this exit connection point corresponds to
        int connectionIndex = -1;
        float minDist = float.MaxValue;

        for (int i = 0; i < tube.Connections.Length; i++)
        {
            if (tube.Connections[i] != null)
            {
                float dist = Vector3.Distance(exitConnectionPoint, tube.Connections[i].transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    connectionIndex = i;
                }
            }
        }

        // Return the entry point corresponding to this connection
        if (connectionIndex >= 0 && connectionIndex < tube.Entries.Length)
        {
            return tube.Entries[connectionIndex].transform.position;
        }

        // Fallback: return first entry
        return tube.Entries[0].transform.position;
    }
    
    /// <summary>
    /// Checks if an entry point is blocked (has a connected tube)
    /// </summary>
    private bool IsEntryPointBlocked(Tube tube, Vector3 entryPoint)
    {
        GameObject connectionAtEntry = tube.GetConnectionAtEntry(entryPoint);
        List<Tube> connectedTubes = tube.FindConnectedTubesAtConnection(connectionAtEntry);
        bool isBlocked = connectedTubes.Count > 0;
        Debug.Log($"IsEntryPointBlocked: Tube {tube.gameObject.name}, EntryPoint {entryPoint}, Connection {connectionAtEntry.name}, ConnectedTubes: {connectedTubes.Count}, Blocked: {isBlocked}");
        return isBlocked;
    }
    
    /// <summary>
    /// Gets the entry point that corresponds to a given connection point
    /// </summary>
    private Vector3 GetEntryPointForConnection(Tube tube, Vector3 connectionPoint)
    {
        // Find which connection index this connection point corresponds to
        int connectionIndex = -1;
        float minDist = float.MaxValue;

        for (int i = 0; i < tube.Connections.Length; i++)
        {
            if (tube.Connections[i] != null)
            {
                float dist = Vector3.Distance(connectionPoint, tube.Connections[i].transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    connectionIndex = i;
                }
            }
        }

        // Return the entry point corresponding to this connection
        if (connectionIndex >= 0 && connectionIndex < tube.Entries.Length)
        {
            return tube.Entries[connectionIndex].transform.position;
        }

        // Fallback: return first entry
        return tube.Entries[0].transform.position;
    }
    
    private IEnumerator FindAndSetTarget()
    {
        yield return new WaitForSeconds(moveDelay);

        // Don't find new targets while in a tube
        if (isInTube)
            yield break;

        PlaceableObject[] allObjects;
        
        // Priority 1: If hamster has food, find a house
        if (foodPieceCount > 0)
        {
            // Find all PlaceableObjects in the scene
            allObjects = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);

            // Filter for houses only
            PlaceableObject[] houses = allObjects.Where(obj => obj.objectType == ObjectType.House).ToArray();

            if (houses.Length > 0)
            {
                // Use the house-finding logic
                yield return StartCoroutine(FindAndSetTargetForObjects(houses));
                yield break;
            }
        }

        // Roll for chill out probability
        if (Random.Range(0f, 1f) < chillOutProbability)
        {
            // Hamster should chill out
            yield return StartCoroutine(GoToChillOut());
            yield break;
        }

        // Find all PlaceableObjects in the scene
        allObjects = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);

        yield return StartCoroutine(FindAndSetTargetForObjects(allObjects));
    }

    private IEnumerator FindAndSetTargetForObjects(PlaceableObject[] objects)
    {
        // Get hamster's current grid position
        Vector2Int hamsterGridPos = gridManager.WorldToGrid(transform.position);

        // Dictionary to store objects with their valid entry points
        // Key: PlaceableObject, Value: List of (worldPos, path) tuples
        Dictionary<PlaceableObject, List<(Vector3 worldPos, List<Vector2Int> path)>> objectsWithValidEntryPoints =
            new Dictionary<PlaceableObject, List<(Vector3, List<Vector2Int>)>>();

        foreach (PlaceableObject obj in objects)
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

        // If we have objects with valid entry points, prioritize and pick one
        if (objectsWithValidEntryPoints.Count > 0)
        {
            // Create a list of objects with their priority scores
            List<(PlaceableObject obj, float priority)> prioritizedObjects =
                new List<(PlaceableObject, float)>();

            float currentTime = Time.time;

            foreach (PlaceableObject obj in objectsWithValidEntryPoints.Keys)
            {
                float priority = 0f;

                // Priority 1: Objects with empty hamstersInteracting lists get higher priority
                // (multiply by 1000 to ensure this is the primary factor)
                if (obj.hamstersInteracting.Count == 0)
                {
                    priority += 1000f;
                }

                // Priority 2: Objects of types interacted with longest ago get higher priority
                // Get time since last interaction (or use a very large value if never interacted)
                float timeSinceLastInteraction = float.MaxValue;
                if (lastInteractionTime.ContainsKey(obj.objectType))
                {
                    timeSinceLastInteraction = currentTime - lastInteractionTime[obj.objectType];
                }

                // Add time since last interaction to priority (longer = higher priority)
                priority += timeSinceLastInteraction;

                prioritizedObjects.Add((obj, priority));
            }

            // Sort by priority (descending - highest priority first)
            prioritizedObjects.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Get the highest priority objects (may be multiple with same priority)
            float highestPriority = prioritizedObjects[0].priority;
            List<PlaceableObject> highestPriorityObjects = prioritizedObjects
                .Where(x => Mathf.Approximately(x.priority, highestPriority))
                .Select(x => x.obj)
                .ToList();

            // Randomly choose from the highest priority objects
            int randomObjectIndex = Random.Range(0, highestPriorityObjects.Count);
            PlaceableObject chosenObject = highestPriorityObjects[randomObjectIndex];

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
            Debug.Log("Hamster found no valid entry points within range, go to chill instead");
            hasTarget = false;
            isMoving = false;
            yield return StartCoroutine(GoToChillOut());
        }

        yield return null;
    }
    
    private void MoveAlongPath()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count)
        {
            // Reached the end of the path
            isMoving = false;
            
            // If we have a target object, interact with it
            if (targetObject != null)
            {
                Debug.Log("Hamster reached target entry point!");
                targetObject.OnInteract(this);
            }
            // Otherwise, if we're chilling out, start the chill out coroutine
            else if (isGoingToChillOut)
            {
                StartCoroutine(ChillOut());
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
    
    private IEnumerator GoToChillOut()
    {
        hasTarget = false;
        isMoving = false;
        
        // Try to find a reachable spot using GetRandomPath
        List<Vector2Int> chillPath = gridManager.GetRandomPath(transform.position, chillOutLookDistance);
        
        if (chillPath != null && chillPath.Count > 0)
        {
            // Found a valid path, follow it
            Debug.Log($"Hamster found a chill spot, following path to end position {chillPath[chillPath.Count - 1]}");
            currentPath = chillPath;
            currentPathIndex = 0;
            targetWorldPosition = gridManager.GridToWorldCenter(chillPath[chillPath.Count - 1]);
            targetObject = null; // No object to interact with
            hasTarget = true;
            isMoving = true;
            isGoingToChillOut = true;
            
            // Wait until the hamster reaches the destination (handled in MoveAlongPath)
            // The MoveAlongPath will call ChillOutAtDestination when path is complete
            yield break;
        }
        else
        {
            // No valid path found, chill out where we are
            Debug.Log("Hamster chilling out at current location");
            yield return StartCoroutine(ChillOut());
        }
    }
    
    private IEnumerator ChillOut()
    {
        isGoingToChillOut = false;
        // Calculate random chill duration
        float chillDuration = Random.Range(chillOutMinDuration, chillOutMaxDuration);
        
        Debug.Log($"Hamster chilling out for {chillDuration} seconds");
        isChillingOut = true;
        
        // Wait for the chill duration
        yield return new WaitForSeconds(chillDuration);
        
        // Done chilling out, find a new target
        isChillingOut = false;
        StartCoroutine(FindAndSetTarget());
    }
}