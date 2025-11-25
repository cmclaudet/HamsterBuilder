using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableObject", menuName = "Scriptable Objects/PlaceableObject")]
public class PlaceableObject : ScriptableObject
{
    public Vector2Int GridSize;
    public GameObject Prefab;
}
