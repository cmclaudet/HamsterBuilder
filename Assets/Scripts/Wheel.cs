using System.Linq;
using UnityEngine;

public class Wheel : PlaceableObject
{
    public GameObject[] Entries;

    public override Vector3[] GetEntryPoints()
    {
        return Entries.Select(e => e.transform.position).ToArray();
    }
}
