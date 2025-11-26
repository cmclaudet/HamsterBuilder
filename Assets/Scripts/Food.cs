using System.Linq;
using UnityEngine;

public class Food : PlaceableObject {
    public GameObject[] Entries;
    public GameObject[] FoodPieces;
    public int foodPickUpCount;
    public float foodPickUpFrequency;

    public override Vector3[] GetEntryPoints()
    {
        var availableFoodPieces = GetAvailableFoodPieces();
        if (availableFoodPieces.Length == 0) {
            return new Vector3[] {};
        } else {
            return Entries.Select(e => e.transform.position).ToArray();
        }
    }

    public override void OnInteract(Hamster hamster)
    {
        StartCoroutine(FoodInteraction(hamster));
    }

    private System.Collections.IEnumerator FoodInteraction(Hamster hamster)
    {
        hamster.StartInteraction();
        
        // Rotate hamster to face the food object
        hamster.RotateToFace(transform.position);
        
        // Pick up food pieces
        int pickedUpCount = 0;
        
        while (pickedUpCount < foodPickUpCount)
        {
            // Wait for the pickup frequency
            yield return new UnityEngine.WaitForSeconds(foodPickUpFrequency);

            // Disable the food piece
            GameObject availableFoodPiece = FoodPieces.FirstOrDefault(f => f.activeInHierarchy);
            if (availableFoodPiece != null)
            {
                availableFoodPiece.SetActive(false);
                // Add food piece to hamster
                hamster.AddFoodPiece();
                pickedUpCount++;
            } else {
                break;
            }
        }
        
        hamster.EndInteraction();
    }

    private GameObject[] GetAvailableFoodPieces() {
        return FoodPieces.Where(f => f.activeInHierarchy).ToArray();
    }
}