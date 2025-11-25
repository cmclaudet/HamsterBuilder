using UnityEngine;
using System.Collections.Generic;
using System;

public class PlacementSystem : MonoBehaviour
{
    public Cage cage;
    public Camera mainCamera;
    public Material previewMaterial;
    
    private PlaceableObjectDefinition currentPlaceableObject;
    private GameObject previewObject;
    private Vector2Int currentGridPosition;
    private bool isPlacementValid;
    private float previewYOffset; // Y offset to place object on floor
    private int currentRotation = 0; // Track rotation in degrees (0, 90, 180, 270)
    
    // Track occupied grid cells
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    
    // Materials for preview
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    
    // Drag and drop state
    private GameObject draggedObject;
    private PlacedObjectData draggedObjectData;
    private Vector2Int dragOriginalGridPosition;
    private Vector2Int dragOriginalGridSize;
    private float dragYOffset;
    private bool isDragging = false;
    
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    void Update()
    {
        if (currentPlaceableObject != null && previewObject != null)
        {
            // Preview mode for placing new objects
            UpdatePreviewPosition();

            if (Input.GetKeyDown(KeyCode.R)) 
            {
                RotatePreviewObject();
            }
            
            // Place object on left mouse click
            if (Input.GetMouseButtonDown(0) && isPlacementValid)
            {
                PlaceObject();
            }
            
            // Cancel placement on right mouse click or ESC
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }
        else if (isDragging)
        {
            // Update dragged object position
            UpdateDragPosition();
            
            // Release dragged object on mouse up
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }
        else
        {
            // Not in preview mode or dragging - handle interaction with placed objects
            
            // Start dragging on left mouse down
            if (Input.GetMouseButtonDown(0))
            {
                TryStartDrag();
            }
            
            // Delete object on right mouse down
            if (Input.GetMouseButtonDown(1))
            {
                TryDeleteObject();
            }
        }
    }

    public void StartPlacement(PlaceableObjectDefinition placeableObject)
    {
        // Cancel any existing placement
        if (previewObject != null)
        {
            CancelPlacement();
        }
        
        currentPlaceableObject = placeableObject;
        currentRotation = 0; // Reset rotation for new object
        
        // Create preview object
        previewObject = Instantiate(placeableObject.Prefab.gameObject);
        
        // Calculate Y offset BEFORE disabling colliders (so bounds are available)
        PlaceableObject placeableObj = previewObject.GetComponent<PlaceableObject>();
        if (placeableObj != null && placeableObj.meshCollider != null)
        {
            // Get the bounds of the mesh collider in world space
            Bounds bounds = placeableObj.meshCollider.bounds;
            // Store the offset from object position to its bottom
            previewYOffset = previewObject.transform.position.y - bounds.min.y;
        }
        else
        {
            previewYOffset = 0f;
        }
        
        // Store original materials and apply preview material
        originalMaterials.Clear();
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            originalMaterials[renderer] = renderer.materials;
            
            // Create transparent materials for preview
            Material[] previewMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                previewMaterials[i] = new Material(previewMaterial);
                previewMaterials[i].color = new Color(1f, 1f, 1f, 0.5f);
            }
            renderer.materials = previewMaterials;
        }
        
        // Disable colliders on preview
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
        
        UpdatePreviewPosition();
    }

    private void RotatePreviewObject()
    {
        if (previewObject != null) {
            Debug.Log("Rotate!");
            previewObject.transform.Rotate(new Vector3(0, 90, 0), Space.World);
            currentRotation = (currentRotation + 90) % 360;
        }
    }
    
    // Get the effective grid size based on current rotation
    private Vector2Int GetRotatedGridSize()
    {
        if (currentPlaceableObject == null)
            return Vector2Int.zero;
            
        Vector2Int originalSize = currentPlaceableObject.GridSize;
        
        // For 90 and 270 degree rotations, swap x and y
        if (currentRotation == 90 || currentRotation == 270)
        {
            return new Vector2Int(originalSize.y, originalSize.x);
        }
        
        return originalSize;
    }
    
    private void UpdatePreviewPosition()
    {
        if (previewObject == null || cage == null)
            return;
        
        // Cast ray from mouse to ground plane
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            
            // Convert to grid coordinates
            Vector2Int gridPos = WorldToGrid(worldPoint);
            currentGridPosition = gridPos;
            
            // Get the rotated grid size
            Vector2Int rotatedSize = GetRotatedGridSize();
            
            // Snap to grid (pass rotated object size for proper centering)
            Vector3 snappedPosition = GridToWorld(gridPos, rotatedSize);
            
            // Adjust Y position to place object on floor using pre-calculated offset
            snappedPosition.y = previewYOffset;
            
            previewObject.transform.position = snappedPosition;
            
            // Validate placement
            isPlacementValid = IsPlacementValid(gridPos, rotatedSize);
            
            // Update preview color based on validity
            UpdatePreviewColor(isPlacementValid);
        }
    }
    
    private void UpdatePreviewColor(bool isValid)
    {
        Color color = isValid ? new Color(1f, 1f, 1f, 0.5f) : new Color(1f, 0.3f, 0.3f, 0.5f);
        
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = color;
            }
        }
    }
    
    private bool IsPlacementValid(Vector2Int gridPos, Vector2Int objectSize)
    {
        // Check if all cells are within the cage bounds
        for (int x = 0; x < objectSize.x; x++)
        {
            for (int z = 0; z < objectSize.y; z++)
            {
                Vector2Int cellPos = new Vector2Int(gridPos.x + x, gridPos.y + z);
                
                // Check if within cage bounds
                if (cellPos.x < 0 || cellPos.x >= cage.GridSize.x ||
                    cellPos.y < 0 || cellPos.y >= cage.GridSize.y)
                {
                    return false;
                }
                
                // Check if cell is already occupied
                if (occupiedCells.Contains(cellPos))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    private void PlaceObject()
    {
        if (currentPlaceableObject == null || previewObject == null)
            return;
        
        // Get the position from preview (which already has correct Y position)
        Vector3 placePosition = previewObject.transform.position;
        
        // Instantiate the actual object
        GameObject placedObject = Instantiate(currentPlaceableObject.Prefab.gameObject, 
            placePosition, 
            previewObject.transform.rotation);
        
        // Get the rotated grid size
        Vector2Int rotatedSize = GetRotatedGridSize();
        
        // Mark grid cells as occupied
        for (int x = 0; x < rotatedSize.x; x++)
        {
            for (int z = 0; z < rotatedSize.y; z++)
            {
                Vector2Int cellPos = new Vector2Int(currentGridPosition.x + x, currentGridPosition.y + z);
                occupiedCells.Add(cellPos);
            }
        }
        
        // Store grid info on the placed object for potential future removal
        PlacedObjectData placedData = placedObject.AddComponent<PlacedObjectData>();
        placedData.gridPosition = currentGridPosition;
        placedData.gridSize = rotatedSize; // Store the rotated size
        
        // Continue placement mode (don't cancel)
        // User can keep placing the same object type
    }
    
    private void CancelPlacement()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
        
        currentPlaceableObject = null;
        originalMaterials.Clear();
    }
    
    private void TryStartDrag()
    {
        // Raycast to find a placed object
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the hit object or its parent has PlacedObjectData
            GameObject hitObject = hit.collider.gameObject;
            PlacedObjectData placedData = hitObject.GetComponent<PlacedObjectData>();
            
            // If not found on the hit object, check parent
            if (placedData == null)
            {
                placedData = hitObject.GetComponentInParent<PlacedObjectData>();
            }
            
            if (placedData != null)
            {
                // Start dragging this object
                draggedObject = placedData.gameObject;
                draggedObjectData = placedData;
                dragOriginalGridPosition = placedData.gridPosition;
                dragOriginalGridSize = placedData.gridSize;
                isDragging = true;
                
                // Calculate Y offset for proper positioning
                PlaceableObject placeableObj = draggedObject.GetComponent<PlaceableObject>();
                if (placeableObj != null && placeableObj.meshCollider != null)
                {
                    Bounds bounds = placeableObj.meshCollider.bounds;
                    dragYOffset = draggedObject.transform.position.y - bounds.min.y;
                }
                else
                {
                    dragYOffset = draggedObject.transform.position.y;
                }
                
                // Free up the occupied cells that this object was using
                FreeOccupiedCells(dragOriginalGridPosition, dragOriginalGridSize);
                
                // Apply preview material to dragged object
                ApplyPreviewMaterial(draggedObject);
            }
        }
    }
    
    private void UpdateDragPosition()
    {
        if (draggedObject == null || cage == null)
            return;
        
        // Cast ray from mouse to ground plane
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            
            // Convert to grid coordinates
            Vector2Int gridPos = WorldToGrid(worldPoint);
            currentGridPosition = gridPos;
            
            // Snap to grid
            Vector3 snappedPosition = GridToWorld(gridPos, dragOriginalGridSize);
            snappedPosition.y = dragYOffset;
            
            draggedObject.transform.position = snappedPosition;
            
            // Validate placement
            isPlacementValid = IsPlacementValid(gridPos, dragOriginalGridSize);
            
            // Update color based on validity
            UpdateDragColor(isPlacementValid);
        }
    }
    
    private void EndDrag()
    {
        if (draggedObject == null)
        {
            isDragging = false;
            return;
        }
        
        // Restore original materials
        RestoreOriginalMaterial(draggedObject);
        
        if (isPlacementValid)
        {
            // Place in new position
            draggedObjectData.gridPosition = currentGridPosition;
            
            // Mark new grid cells as occupied
            OccupyCells(currentGridPosition, dragOriginalGridSize);
        }
        else
        {
            // Return to original position
            Vector3 originalPosition = GridToWorld(dragOriginalGridPosition, dragOriginalGridSize);
            originalPosition.y = dragYOffset;
            draggedObject.transform.position = originalPosition;
            
            // Re-occupy original cells
            OccupyCells(dragOriginalGridPosition, dragOriginalGridSize);
        }
        
        // Clear drag state
        draggedObject = null;
        draggedObjectData = null;
        isDragging = false;
    }
    
    private void TryDeleteObject()
    {
        // Raycast to find a placed object
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the hit object or its parent has PlacedObjectData
            GameObject hitObject = hit.collider.gameObject;
            PlacedObjectData placedData = hitObject.GetComponent<PlacedObjectData>();
            
            // If not found on the hit object, check parent
            if (placedData == null)
            {
                placedData = hitObject.GetComponentInParent<PlacedObjectData>();
            }
            
            if (placedData != null)
            {
                // Free up occupied cells
                FreeOccupiedCells(placedData.gridPosition, placedData.gridSize);
                
                // Destroy the object
                Destroy(placedData.gameObject);
            }
        }
    }
    
    private void ApplyPreviewMaterial(GameObject obj)
    {
        originalMaterials.Clear();
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            originalMaterials[renderer] = renderer.materials;
            
            // Create transparent materials for preview
            Material[] previewMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                previewMaterials[i] = new Material(previewMaterial);
                previewMaterials[i].color = new Color(1f, 1f, 1f, 0.5f);
            }
            renderer.materials = previewMaterials;
        }
    }
    
    private void RestoreOriginalMaterial(GameObject obj)
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.materials = kvp.Value;
            }
        }
        originalMaterials.Clear();
    }
    
    private void UpdateDragColor(bool isValid)
    {
        Color color = isValid ? new Color(1f, 1f, 1f, 0.5f) : new Color(1f, 0.3f, 0.3f, 0.5f);
        
        Renderer[] renderers = draggedObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = color;
            }
        }
    }
    
    private void OccupyCells(Vector2Int gridPos, Vector2Int gridSize)
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
    
    private void FreeOccupiedCells(Vector2Int gridPos, Vector2Int gridSize)
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
    
    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // Convert world position to grid coordinates
        // The cage is centered at origin, so we need to offset
        float halfWidth = (cage.GridSize.x * cage.GridUnitSize) / 2f;
        float halfDepth = (cage.GridSize.y * cage.GridUnitSize) / 2f;
        
        int gridX = Mathf.FloorToInt((worldPos.x + halfWidth) / cage.GridUnitSize);
        int gridZ = Mathf.FloorToInt((worldPos.z + halfDepth) / cage.GridUnitSize);
        
        return new Vector2Int(gridX, gridZ);
    }
    
    private Vector3 GridToWorld(Vector2Int gridPos, Vector2Int objectSize)
    {
        // Convert grid coordinates to world position
        float halfWidth = (cage.GridSize.x * cage.GridUnitSize) / 2f;
        float halfDepth = (cage.GridSize.y * cage.GridUnitSize) / 2f;
        
        // Calculate offset based on object size so objects align to grid properly
        // For even-sized objects (2x2, 4x4), this positions them at grid intersections
        // For odd-sized objects (1x1, 3x3), this centers them in their cells
        float offsetX = (objectSize.x * cage.GridUnitSize) / 2f;
        float offsetZ = (objectSize.y * cage.GridUnitSize) / 2f;
        
        float worldX = (gridPos.x * cage.GridUnitSize) - halfWidth + offsetX;
        float worldZ = (gridPos.y * cage.GridUnitSize) - halfDepth + offsetZ;
        
        return new Vector3(worldX, 0f, worldZ);
    }
    
    // Helper component to store placement data on objects
    public class PlacedObjectData : MonoBehaviour
    {
        public Vector2Int gridPosition;
        public Vector2Int gridSize;
    }
}

