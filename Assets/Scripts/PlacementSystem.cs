using UnityEngine;
using System.Collections.Generic;

public class PlacementSystem : MonoBehaviour
{
    public Cage cage;
    public Camera mainCamera;
    
    private PlaceableObjectDefinition currentPlaceableObject;
    private GameObject previewObject;
    private Vector2Int currentGridPosition;
    private bool isPlacementValid;
    
    // Track occupied grid cells
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    
    // Materials for preview
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Material previewMaterial;
    
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // Create a transparent material for previews
        previewMaterial = new Material(Shader.Find("Standard"));
        previewMaterial.SetFloat("_Mode", 3); // Transparent mode
        previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.DisableKeyword("_ALPHATEST_ON");
        previewMaterial.EnableKeyword("_ALPHABLEND_ON");
        previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        previewMaterial.renderQueue = 3000;
    }
    
    void Update()
    {
        if (currentPlaceableObject != null && previewObject != null)
        {
            UpdatePreviewPosition();
            
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
    }
    
    public void StartPlacement(PlaceableObjectDefinition placeableObject)
    {
        // Cancel any existing placement
        if (previewObject != null)
        {
            CancelPlacement();
        }
        
        currentPlaceableObject = placeableObject;
        
        // Create preview object
        previewObject = Instantiate(placeableObject.Prefab);
        
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
            
            // Snap to grid
            Vector3 snappedPosition = GridToWorld(gridPos);
            previewObject.transform.position = snappedPosition;
            
            // Validate placement
            isPlacementValid = IsPlacementValid(gridPos, currentPlaceableObject.GridSize);
            
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
        
        // Instantiate the actual object
        GameObject placedObject = Instantiate(currentPlaceableObject.Prefab, 
            previewObject.transform.position, 
            previewObject.transform.rotation);
        
        // Mark grid cells as occupied
        for (int x = 0; x < currentPlaceableObject.GridSize.x; x++)
        {
            for (int z = 0; z < currentPlaceableObject.GridSize.y; z++)
            {
                Vector2Int cellPos = new Vector2Int(currentGridPosition.x + x, currentGridPosition.y + z);
                occupiedCells.Add(cellPos);
            }
        }
        
        // Store grid info on the placed object for potential future removal
        PlacedObjectData placedData = placedObject.AddComponent<PlacedObjectData>();
        placedData.gridPosition = currentGridPosition;
        placedData.gridSize = currentPlaceableObject.GridSize;
        
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
    
    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        // Convert grid coordinates to world position
        float halfWidth = (cage.GridSize.x * cage.GridUnitSize) / 2f;
        float halfDepth = (cage.GridSize.y * cage.GridUnitSize) / 2f;
        
        float worldX = (gridPos.x * cage.GridUnitSize) - halfWidth + (cage.GridUnitSize / 2f);
        float worldZ = (gridPos.y * cage.GridUnitSize) - halfDepth + (cage.GridUnitSize / 2f);
        
        return new Vector3(worldX, 0f, worldZ);
    }
    
    // Helper component to store placement data on objects
    public class PlacedObjectData : MonoBehaviour
    {
        public Vector2Int gridPosition;
        public Vector2Int gridSize;
    }
}

