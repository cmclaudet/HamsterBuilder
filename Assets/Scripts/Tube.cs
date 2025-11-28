using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Tube : PlaceableObject
{
    // entries and connections should be the same length. They are paired so that an entry and its corresponding connection can be found by index
    public GameObject[] Entries;
    public GameObject[] Connections;

    // waypoint is pass through point for every entry -> entry combination
    public GameObject Waypoint;
    
    // Threshold distance for detecting tube connections
    private const float CONNECTION_THRESHOLD = 0.1f;

    public override Vector3[] GetEntryPoints()
    {
        Vector3[] entryPoints = new Vector3[Entries.Length];
        for (int i = 0; i < Entries.Length; i++)
        {
            entryPoints[i] = Entries[i].transform.position;
        }
        return entryPoints;
    }

    /// <summary>
    /// Finds the index of the entry point closest to the given world position
    /// </summary>
    public int GetClosestEntryIndex(Vector3 worldPosition)
    {
        int closestIndex = 0;
        float minDistance = Vector3.Distance(worldPosition, Entries[0].transform.position);

        for (int i = 1; i < Entries.Length; i++)
        {
            float distance = Vector3.Distance(worldPosition, Entries[i].transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    /// <summary>
    /// Gets the connection GameObject corresponding to the given entry point
    /// </summary>
    public GameObject GetConnectionAtEntry(Vector3 entryPoint)
    {
        int entryIndex = GetClosestEntryIndex(entryPoint);
        return Connections[entryIndex];
    }

    /// <summary>
    /// Gets a randomly selected exit connection (excluding the entry connection)
    /// </summary>
    public GameObject GetExitConnection(Vector3 entryPoint)
    {
        int entryIndex = GetClosestEntryIndex(entryPoint);

        // If only 2 entries, return the opposite one
        if (Entries.Length == 2)
        {
            return Connections[1 - entryIndex];
        }

        // For more than 2 entries, randomly pick one that isn't the entry
        int exitIndex;
        do
        {
            exitIndex = Random.Range(0, Entries.Length);
        } while (exitIndex == entryIndex);

        return Connections[exitIndex];
    }

    /// <summary>
    /// Gets the entry point position corresponding to a given connection
    /// </summary>
    public Vector3 GetEntryPointForConnection(GameObject connection)
    {
        for (int i = 0; i < Connections.Length; i++)
        {
            if (Connections[i] == connection)
            {
                return Entries[i].transform.position;
            }
        }

        // Fallback: return first entry
        return Entries[0].transform.position;
    }

    /// <summary>
    /// Gets the path through this tube starting from the given entry point.
    /// Returns a list of Vector3 positions: [connection] -> [waypoint] -> [exit connection]
    /// </summary>
    public List<Vector3> GetPathFromEntry(Vector3 entryPoint)
    {
        List<Vector3> path = new List<Vector3>();

        GameObject startConnection = GetConnectionAtEntry(entryPoint);
        GameObject endConnection = GetExitConnection(entryPoint);

        // Start at the connection point
        path.Add(startConnection.transform.position);

        // Waypoint is always used as the midpoint between any 2 different connections
        path.Add(Waypoint.transform.position);

        // End at the exit connection
        path.Add(endConnection.transform.position);

        return path;
    }

    /// <summary>
    /// Finds tubes connected to the given connection point
    /// </summary>
    public List<Tube> FindConnectedTubesAtConnection(GameObject connectionPoint)
    {
        List<Tube> connectedTubes = new List<Tube>();
        Vector3 connectionPos = connectionPoint.transform.position;

        // Find all tubes in the scene
        Tube[] allTubes = FindObjectsByType<Tube>(FindObjectsSortMode.None);

        foreach (Tube tube in allTubes)
        {
            // Skip self
            if (tube == this)
                continue;

            // Check if any of this tube's connections are close to our connection point
            for (int i = 0; i < tube.Connections.Length; i++)
            {
                if (tube.Connections[i] != null)
                {
                    float dist = Vector3.Distance(connectionPos, tube.Connections[i].transform.position);
                    if (dist < CONNECTION_THRESHOLD)
                    {
                        connectedTubes.Add(tube);
                        break; // Only add the tube once, even if multiple connections match
                    }
                }
            }
        }

        return connectedTubes;
    }

    /// <summary>
    /// Builds a complete traversal path through connected tubes starting from the given entry point.
    /// Returns a tuple: (path, needsBacktracking)
    /// </summary>
    public (List<Vector3> path, bool needsBacktracking) BuildTubeTraversalPath(Vector3 entryPoint, HashSet<Tube> visited = null, List<Vector3> currentPath = null)
    {
        if (visited == null)
            visited = new HashSet<Tube>();
        
        if (currentPath == null)
            currentPath = new List<Vector3>();
        
        // Mark this tube as visited
        visited.Add(this);
        
        // Get path through this tube
        List<Vector3> tubePath = GetPathFromEntry(entryPoint);
        
        // Add this tube's path to the current path
        List<Vector3> fullPath = new List<Vector3>(currentPath);
        fullPath.AddRange(tubePath);
        
        // Get the exit connection
        GameObject exitConnection = GetExitConnection(entryPoint);
        
        // Check for connected tubes at the exit
        List<Tube> connectedTubes = FindConnectedTubesAtConnection(exitConnection);
        
        // Filter out already visited tubes to prevent infinite loops
        connectedTubes = connectedTubes.Where(t => !visited.Contains(t)).ToList();
        
        if (connectedTubes.Count > 0)
        {
            // Try each connected tube until we find one with an unblocked exit
            // Track the longest path explored in case all need backtracking
            List<Vector3> longestExploredPath = fullPath;
            
            foreach (Tube nextTube in connectedTubes)
            {
                // Determine which entry point of the next tube corresponds to the connection
                // Find the entry point closest to our exit connection
                Vector3 nextEntryPoint = nextTube.Entries[0].transform.position;
                float minDistance = Vector3.Distance(exitConnection.transform.position, nextEntryPoint);

                for (int i = 1; i < nextTube.Entries.Length; i++)
                {
                    float distance = Vector3.Distance(exitConnection.transform.position, nextTube.Entries[i].transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nextEntryPoint = nextTube.Entries[i].transform.position;
                    }
                }
                
                // Create a copy of visited set for this branch (so backtracking doesn't affect other branches)
                HashSet<Tube> branchVisited = new HashSet<Tube>(visited);
                
                // IMPORTANT: Create a fresh copy of fullPath for each connection attempt
                // This ensures that failed attempts don't pollute the path for the next attempt
                List<Vector3> branchPath = new List<Vector3>(fullPath);
                
                // Recursively build path through connected tubes
                var (nextPath, needsBacktracking) = nextTube.BuildTubeTraversalPath(nextEntryPoint, branchVisited, branchPath);
                
                if (!needsBacktracking && nextPath != null && nextPath.Count > 0)
                {
                    // Found an unblocked exit through this connection!
                    // The nextPath already includes the full path from the recursive call
                    // We just need to return it
                    Debug.Log($"Tube {gameObject.name}: Found unblocked exit through connection to {nextTube.gameObject.name}, path length: {nextPath.Count}");
                    return (nextPath, false);
                }
                
                // If this branch needs backtracking, track the longest path explored
                if (nextPath != null && nextPath.Count > longestExploredPath.Count)
                {
                    longestExploredPath = nextPath;
                }
                
                // Try the next connection
                Debug.Log($"Tube {gameObject.name}: Connection to {nextTube.gameObject.name} needs backtracking, trying next connection");
            }
            
            // All connections lead to dead ends - need to backtrack
            // Return the longest path we explored so the hamster travels through all connected tubes before backtracking
            Debug.Log($"Tube {gameObject.name}: All {connectedTubes.Count} connections lead to dead ends, backtracking. Longest path length: {longestExploredPath.Count}");
            return (longestExploredPath, true);
        }
        else
        {
            // No connected tube - check if the exit entry point is actually unblocked
            // Get the exit entry point (where the hamster would exit)
            Vector3 exitEntryPoint = GetEntryPointForConnection(exitConnection);

            // Check if the exit entry point's grid cell is occupied
            GridManager gridManager = FindObjectsByType<GridManager>(FindObjectsSortMode.None).FirstOrDefault();
            if (gridManager != null)
            {
                Vector2Int exitGridPos = gridManager.WorldToGrid(exitEntryPoint);
                bool isExitBlocked = !gridManager.IsWithinBounds(exitGridPos) || gridManager.IsCellOccupied(exitGridPos);

                if (isExitBlocked)
                {
                    // Exit entry point is blocked by another object - need to backtrack
                    Debug.Log($"Tube {gameObject.name}: Exit entry point at {exitEntryPoint} is blocked (grid cell {exitGridPos} is occupied), backtracking");
                    return (fullPath, true);
                }
            }

            // No connected tube and exit entry point is not blocked - this is an unblocked exit
            Debug.Log($"Tube {gameObject.name}: Exit entry point at {exitEntryPoint} is unblocked");
            return (fullPath, false);
        }
    }

    /// <summary>
    /// Override OnInteract to handle tube traversal
    /// </summary>
    public override void OnInteract(Hamster hamster)
    {
        base.OnInteract(hamster);
        
        // Determine which entry point the hamster reached
        Vector3 entryPoint = hamster.transform.position;
        
        // Build the traversal path through connected tubes
        var (traversalPath, needsBacktracking) = BuildTubeTraversalPath(entryPoint);
        
        if (needsBacktracking && traversalPath != null && traversalPath.Count > 0)
        {
            // No unblocked exit - backtrack by reversing the path
            traversalPath.Reverse();
            hamster.StartTubeTraversal(traversalPath, this, isBacktracking: true, entryPoint: entryPoint);
        }
        else if (traversalPath != null && traversalPath.Count > 0)
        {
            // Valid path found - start hamster on tube traversal
            hamster.StartTubeTraversal(traversalPath, this, isBacktracking: false, entryPoint: entryPoint);
        }
        else
        {
            // Fallback: just reverse this tube's path
            List<Vector3> reversePath = GetPathFromEntry(entryPoint);
            reversePath.Reverse();
            hamster.StartTubeTraversal(reversePath, this, isBacktracking: true, entryPoint: entryPoint);
        }
    }
}
