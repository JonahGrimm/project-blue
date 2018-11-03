using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPlayerForCombat : MonoBehaviour
{
    private SphereCollider sc;

    private void Start()
    {
        sc = GetComponent<SphereCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("found something!");

        if (other.gameObject.tag == ULayTags.playerTag)
        {
            //Debug.Log("Touch! " + other.gameObject.name);
            GameObject.Find("CombatStarter").GetComponent<CombatStarter>().StartCombat(other.transform.position);
            sc.enabled = false;
        }
    }
}
