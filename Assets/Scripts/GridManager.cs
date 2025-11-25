using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    public Cage cage;
    
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    
    public bool IsCellOccupied(Vector2Int cell)
    {
        return occupiedCells.Contains(cell);
    }
    
    public void OccupyCells(Vector2Int gridPos, Vector2Int gridSize)
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int z = 0; z < gridSize.y; z++)
            {
                Vector2Int cellPos = new Vector2Int(gridPos.x + x, gridPos.y + z);
                occupiedCells.Add(cellPos);
            }
        }
    }
    
    public void FreeCells(Vector2Int gridPos, Vector2Int gridSize)
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int z = 0; z < gridSize.y; z++)
            {
                Vector2Int cellPos = new Vector2Int(gridPos.x + x, gridPos.y + z);
                occupiedCells.Remove(cellPos);
            }
        }
    }
    
    public bool IsWithinBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < cage.GridSize.x &&
               cell.y >= 0 && cell.y < cage.GridSize.y;
    }
    
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // Convert world position to grid coordinates
        // The cage is centered at origin, so we need to offset
        float halfWidth = (cage.GridSize.x * cage.GridUnitSize) / 2f;
        float halfDepth = (cage.GridSize.y * cage.GridUnitSize) / 2f;
        
        int gridX = Mathf.FloorToInt((worldPos.x + halfWidth) / cage.GridUnitSize);
        int gridZ = Mathf.FloorToInt((worldPos.z + halfDepth) / cage.GridUnitSize);
        
        return new Vector2Int(gridX, gridZ);
    }
    
    public Vector3 GridToWorld(Vector2Int gridPos, Vector2Int objectSize)
    {
        // Convert grid coordinates to world position
        float halfWidth = (cage.GridSize.x * cage.GridUnitSize) / 2f;
        float halfDepth = (cage.GridSize.y * cage.GridUnitSize) / 2f;
        
        // Calculate offset based on object size so objects align to grid properly
        float offsetX = (objectSize.x * cage.GridUnitSize) / 2f;
        float offsetZ = (objectSize.y * cage.GridUnitSize) / 2f;
        
        float worldX = (gridPos.x * cage.GridUnitSize) - halfWidth + offsetX;
        float worldZ = (gridPos.y * cage.GridUnitSize) - halfDepth + offsetZ;
        
        return new Vector3(worldX, 0f, worldZ);
    }
    
    public Vector3 GridToWorldCenter(Vector2Int gridPos)
    {
        // Convert grid coordinates to world position (cell center)
        return GridToWorld(gridPos, Vector2Int.one);
    }
    
    /// <summary>
    /// Find a path from start grid position to end grid position using A* pathfinding.
    /// Returns a list of grid positions representing the path, or null if no path exists.
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // Check if start and end are valid
        if (!IsWithinBounds(start) || !IsWithinBounds(end))
            return null;
        
        // If end is occupied, no path is possible
        if (IsCellOccupied(end))
            return null;
        
        // If start is the same as end
        if (start == end)
            return new List<Vector2Int> { start };
        
        // A* pathfinding
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        HashSet<Vector2Int> openSet = new HashSet<Vector2Int> { start };
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
        
        gScore[start] = 0;
        fScore[start] = Heuristic(start, end);
        
        while (openSet.Count > 0)
        {
            // Get node with lowest fScore
            Vector2Int current = openSet.OrderBy(node => fScore.GetValueOrDefault(node, float.MaxValue)).First();
            
            if (current == end)
            {
                // Reconstruct path
                return ReconstructPath(cameFrom, current);
            }
            
            openSet.Remove(current);
            closedSet.Add(current);
            
            // Check neighbors (4-directional movement)
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x, current.y + 1),
                new Vector2Int(current.x, current.y - 1)
            };
            
            foreach (Vector2Int neighbor in neighbors)
            {
                // Skip if out of bounds
                if (!IsWithinBounds(neighbor))
                    continue;
                
                // Skip if occupied (unless it's the end point)
                if (neighbor != end && IsCellOccupied(neighbor))
                    continue;
                
                // Skip if already evaluated
                if (closedSet.Contains(neighbor))
                    continue;
                
                float tentativeGScore = gScore[current] + 1; // Cost of 1 per move
                
                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    continue; // This is not a better path
                }
                
                // This is the best path so far
                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);
            }
        }
        
        // No path found
        return null;
    }
    
    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        // Manhattan distance
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int> { current };
        
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        
        return path;
    }
}

