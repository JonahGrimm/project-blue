using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshCombiner))]
public class MeshCombinerTester : MonoBehaviour
{
    public bool activateSimpleCombine = false;
    public bool activateAdvancedCombine = false;

    // Update is called once per frame
    void Update ()
    {
		if (activateSimpleCombine)
        {
            activateSimpleCombine = false;
            GetComponent<MeshCombiner>().CombineMeshes();
        }

        if (activateAdvancedCombine)
        {
            activateAdvancedCombine = false;
            GetComponent<MeshCombiner>().AdvancedMerge();
        }
    }
}
