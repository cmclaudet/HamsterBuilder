
using UnityEngine;

public class PlaceableObject : MonoBehaviour 
{
    public MeshCollider meshCollider;
    public ObjectType objectType;
    public int[] ViableRotations;
    private int currentRotationIndex;

    void Start() {
        if (ViableRotations == null || ViableRotations.Length == 0) {
            ViableRotations = new int[] {0, 90, 180, 270};
        }
        currentRotationIndex = 0;
    }

    public virtual Vector3[] GetEntryPoints() {
        return new Vector3[] {};
    }

    public virtual void OnInteract(Hamster hamster) {

    }

    public int Rotate() {
        currentRotationIndex = (currentRotationIndex + 1) % ViableRotations.Length;
        int targetRotation = ViableRotations[currentRotationIndex];
        // Set rotation absolutely using eulerAngles
        transform.eulerAngles = new Vector3(0, targetRotation, 0);
        return targetRotation;
    }
}