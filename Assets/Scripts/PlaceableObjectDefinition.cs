using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableObjectDefinition", menuName = "Scriptable Objects/PlaceableObjectDefinition")]
public class PlaceableObjectDefinition : ScriptableObject
{
    public Vector2Int GridSize;
    public GameObject Prefab;
}
