using System.Linq;
using UnityEngine;

public class House : PlaceableObject {
    public GameObject[] Entries;
    public GameObject hamsterPosition;
    public float interactionDuration;
    public float foodDropFrequency;
    public GameObject foodPrefab;
    public Vector3 foodPositionOffset;
    private Vector3 hamsterFaceDirection = new(-1, 0, 0);

    public override Vector3[] GetEntryPoints()
    {
        return Entries.Select(e => e.transform.position).ToArray();
    }

    public override void OnInteract(Hamster hamster)
    {
        base.OnInteract(hamster);
        StartCoroutine(HouseInteraction(hamster));
    }

    private System.Collections.IEnumerator HouseInteraction(Hamster hamster)
    {
        hamster.StartInteraction();
        
        // Store original position
        Vector3 originalPosition = hamster.transform.position;

        // Place hamster at the house position
        hamster.SetPosition(hamsterPosition.transform.position);
        SetHamsterRotation(hamster);

        // Check if hamster has any food
        if (hamster.GetFoodPieceCount() > 0)
        {
            // Drop food one by one
            while (hamster.GetFoodPieceCount() > 0)
            {
                // Generate random offset in XZ plane between 0 and 0.2
                float randomDistance = Random.Range(0f, 0.2f);
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 randomXZplaneOffset = new Vector3(
                    Mathf.Cos(randomAngle) * randomDistance,
                    0f,
                    Mathf.Sin(randomAngle) * randomDistance
                );
                
                float randomRotation = Random.Range(0f, 360f);

                // Calculate spawn position
                Vector3 spawnPosition = hamsterPosition.transform.position + foodPositionOffset + randomXZplaneOffset;

                // Instantiate food prefab
                var food = Instantiate(foodPrefab, spawnPosition, Quaternion.identity);
                food.transform.Rotate(90, randomRotation, 0);

                // Remove one food piece from hamster
                hamster.RemoveFoodPiece();

                // Wait for food drop frequency before next drop
                yield return new UnityEngine.WaitForSeconds(foodDropFrequency);
            }
        }
        else
        {
            // No food, just wait for interaction duration
            yield return new UnityEngine.WaitForSeconds(interactionDuration);
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