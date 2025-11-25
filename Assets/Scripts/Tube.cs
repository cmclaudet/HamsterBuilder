using UnityEngine;

public class Tube : PlaceableObject
{
    public GameObject Entry1;
    public GameObject Entry2;

    public override Vector2[] GetEntryPoints()
    {
        return new Vector2[] {Entry1.transform.position, Entry2.transform.position};
    }
}
