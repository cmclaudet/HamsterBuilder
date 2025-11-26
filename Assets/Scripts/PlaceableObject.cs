
using UnityEngine;

public class PlaceableObject : MonoBehaviour 
{
    public MeshCollider meshCollider;
    public ObjectType objectType;

    public virtual Vector3[] GetEntryPoints() {
        return new Vector3[] {};
    }

    public virtual void OnInteract(Hamster hamster) {

    }
}