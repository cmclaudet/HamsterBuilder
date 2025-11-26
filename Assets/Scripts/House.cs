using System.Linq;
using UnityEngine;

public class House : PlaceableObject {
    public GameObject[] Entries;
    public GameObject hamsterPosition;
    public float interactionDuration;

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
        
        // Wait for the interaction duration
        yield return new UnityEngine.WaitForSeconds(interactionDuration);
        
        // Return hamster to original position
        hamster.SetPosition(originalPosition);
        
        hamster.EndInteraction();
    }
}