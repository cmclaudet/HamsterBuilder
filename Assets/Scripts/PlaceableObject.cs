
using UnityEngine;

public class PlaceableObject : MonoBehaviour 
{
    public MeshCollider meshCollider;
    public ObjectType objectType;

    public virtual Vector2[] GetEntryPoints() {
        return new Vector2[] {};
    }
}