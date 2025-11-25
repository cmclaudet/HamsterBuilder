using UnityEngine;

public class Tube : PlaceableObject
{
    public GameObject Entry1;
    public GameObject Entry2;

    public override Vector3[] GetEntryPoints()
    {
        return new Vector3[] {Entry1.transform.position, Entry2.transform.position};
    }
}
