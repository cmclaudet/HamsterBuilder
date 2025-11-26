using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Tube : PlaceableObject
{
    public GameObject Entry1;
    public GameObject Entry2;

    // waypoint positions are path waypoints ordered from connection1 to connection2
    public GameObject[] Waypoints;
    
    // start path at connection1 if hamster enters from entry1
    public GameObject Connection1;

    // start path at connection2 if hamster enters from entry2
    public GameObject Connection2;

    // Threshold distance for detecting tube connections
    private const float CONNECTION_THRESHOLD = 0.1f;

    public override Vector3[] GetEntryPoints()
    {
        return new Vector3[] {Entry1.transform.position, Entry2.transform.position};
    }

    /// <summary>
    /// Determines which entry point (Entry1 or Entry2) the given world position is closest to
    /// </summary>
    public bool IsEntry1(Vector3 worldPosition)
    {
        float distToEntry1 = Vector3.Distance(worldPosition, Entry1.transform.position);
        float distToEntry2 = Vector3.Distance(worldPosition, Entry2.transform.position);
        return distToEntry1 < distToEntry2;
    }

    /// <summary>
    /// Gets the connection GameObject corresponding to the given entry point
    /// </summary>
    public GameObject GetConnectionAtEntry(Vector3 entryPoint)
    {
        return IsEntry1(entryPoint) ? Connection1 : Connection2;
    }

    /// <summary>
    /// Gets the exit connection (opposite of entry connection)
    /// </summary>
    public GameObject GetExitConnection(Vector3 entryPoint)
    {
        return IsEntry1(entryPoint) ? Connection2 : Connection1;
    }

    /// <summary>
    /// Gets the path through this tube starting from the given entry point.
    /// Returns a list of Vector3 positions: [connection] -> [waypoints] -> [exit connection]
    /// </summary>
    public List<Vector3> GetPathFromEntry(Vector3 entryPoint)
    {
        List<Vector3> path = new List<Vector3>();
        
        bool enteringFromEntry1 = IsEntry1(entryPoint);
        GameObject startConnection = enteringFromEntry1 ? Connection1 : Connection2;
        GameObject endConnection = enteringFromEntry1 ? Connection2 : Connection1;
        
        // Start at the connection point
        path.Add(startConnection.transform.position);
        
        // Add waypoints (forward if Entry1, reverse if Entry2)
        if (Waypoints != null && Waypoints.Length > 0)
        {
            if (enteringFromEntry1)
            {
                // Forward order: waypoints as defined
                foreach (GameObject waypoint in Waypoints)
                {
                    if (waypoint != null)
                        path.Add(waypoint.transform.position);
                }
            }
            else
            {
                // Reverse order: waypoints in reverse
                for (int i = Waypoints.Length - 1; i >= 0; i--)
                {
                    if (Waypoints[i] != null)
                        path.Add(Waypoints[i].transform.position);
                }
            }
        }
        
        // End at the exit connection
        path.Add(endConnection.transform.position);
        // if (enteringFromEntry1) {
        //     path.Add(Entry2.transform.position);
        // } else {
        //     path.Add(Entry1.transform.position);
        // }
        
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
            
            // Check if this tube's Connection1 or Connection2 is close to our connection point
            if (tube.Connection1 != null)
            {
                float dist1 = Vector3.Distance(connectionPos, tube.Connection1.transform.position);
                if (dist1 < CONNECTION_THRESHOLD)
                {
                    connectedTubes.Add(tube);
                    continue;
                }
            }
            
            if (tube.Connection2 != null)
            {
                float dist2 = Vector3.Distance(connectionPos, tube.Connection2.transform.position);
                if (dist2 < CONNECTION_THRESHOLD)
                {
                    connectedTubes.Add(tube);
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
            foreach (Tube nextTube in connectedTubes)
            {
                // Determine which entry point of the next tube corresponds to the connection
                // Find the entry point closest to our exit connection
                Vector3 nextEntryPoint;
                float distToEntry1 = Vector3.Distance(exitConnection.transform.position, nextTube.Entry1.transform.position);
                float distToEntry2 = Vector3.Distance(exitConnection.transform.position, nextTube.Entry2.transform.position);
                
                if (distToEntry1 < distToEntry2)
                    nextEntryPoint = nextTube.Entry1.transform.position;
                else
                    nextEntryPoint = nextTube.Entry2.transform.position;
                
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
                // If this branch needs backtracking, try the next connection
                Debug.Log($"Tube {gameObject.name}: Connection to {nextTube.gameObject.name} needs backtracking, trying next connection");
            }
            
            // All connections lead to dead ends - need to backtrack
            Debug.Log($"Tube {gameObject.name}: All {connectedTubes.Count} connections lead to dead ends, backtracking");
            return (fullPath, true);
        }
        else
        {
            // No connected tube - this is an unblocked exit
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
