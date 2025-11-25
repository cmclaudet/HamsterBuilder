using UnityEngine;

public class Cage : MonoBehaviour
{
    public float GridUnitSize;
    public Vector2Int GridSize;
    public int WallHeightGridUnits;

    public MeshFilter FloorMesh;
    public MeshFilter ZPlaneWallMesh;
    public MeshFilter XPlaneWallMesh;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     #if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to initialize cage floor and walls in edit mode
    /// </summary>
    [ContextMenu("Snap Cage to Grid")]
    private void EditorInitializeCage()
    {
        if (FloorMesh == null || ZPlaneWallMesh == null || XPlaneWallMesh == null)
        {
            Debug.LogWarning("Cage: One or more mesh filters are not assigned.");
            return;
        }

        // Calculate dimensions
        float floorWidth = GridSize.x * GridUnitSize;
        float floorDepth = GridSize.y * GridUnitSize;
        float wallHeight = WallHeightGridUnits * GridUnitSize;

        // Unity's default plane is 10x10 units, so we scale by desiredSize / 10
        const float defaultPlaneSize = 10f;

        // Initialize FloorMesh
        FloorMesh.transform.localScale = new Vector3(
            floorWidth / defaultPlaneSize,
            1f,
            floorDepth / defaultPlaneSize
        );
        FloorMesh.transform.localPosition = new Vector3(
            0,
            0f,
            0
        );

        // Initialize ZPlaneWallMesh (walls along Z axis - front/back)
        // These walls span the width (X) of the floor
        ZPlaneWallMesh.transform.localScale = new Vector3(
            wallHeight / defaultPlaneSize,
            1f,
            floorWidth / defaultPlaneSize
        );
        ZPlaneWallMesh.transform.position = new Vector3(
            0f,
            wallHeight / 2f,
            floorDepth / 2f
        );

        // Initialize XPlaneWallMesh (walls along X axis - left/right)
        // These walls span the depth (Z) of the floor
        XPlaneWallMesh.transform.localScale = new Vector3(
            wallHeight / defaultPlaneSize,
            1f,
            floorDepth / defaultPlaneSize
        );
        XPlaneWallMesh.transform.position = new Vector3(
            floorWidth / 2f,
            wallHeight / 2f,
            0f
        );

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(FloorMesh);
        UnityEditor.EditorUtility.SetDirty(ZPlaneWallMesh);
        UnityEditor.EditorUtility.SetDirty(XPlaneWallMesh);
        #endif
    }
    #endif
}
