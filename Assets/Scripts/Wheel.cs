using System;
using System.Linq;
using UnityEngine;

public class Wheel : PlaceableObject
{
    public GameObject[] Entries;
    public GameObject hamsterPosition;
    public float rotationSpeed;
    public float interactionDuration;
    public GameObject WheelMesh;
    private Vector3 hamsterFaceDirection = new(-1, 0, 0);

    public override Vector3[] GetEntryPoints()
    {
        return Entries.Select(e => e.transform.position).ToArray();
    }

    public override void OnInteract(Hamster hamster)
    {
        base.OnInteract(hamster);
        StartCoroutine(WheelInteraction(hamster));
    }

    private System.Collections.IEnumerator WheelInteraction(Hamster hamster)
    {
        hamster.StartInteraction();
        SetHamsterRotation(hamster);
        
        // Store original position
        Vector3 originalPosition = hamster.transform.position;
        
        // Place hamster at the wheel position
        hamster.SetPosition(hamsterPosition.transform.position);
        
        // Rotate the wheel for the interaction duration
        float elapsedTime = 0f;
        while (elapsedTime < interactionDuration)
        {
            // Rotate around Z axis
            WheelMesh.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Return hamster to original position
        hamster.SetPosition(originalPosition);
        
        hamster.EndInteraction();
    }

    private void SetHamsterRotation(Hamster hamster)
    {
        // Transform hamsterFaceDirection from wheel's local space to world space
        // This accounts for the wheel's current Y rotation
        Vector3 worldDirection = transform.TransformDirection(hamsterFaceDirection.normalized);
        worldDirection.y = 0; // Keep on horizontal plane
        worldDirection.Normalize();
        
        hamster.SetFacingRotation(worldDirection);
    }
}
