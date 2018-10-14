using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectEntityCollisions : MonoBehaviour
{
    Hitbox reference;
    string targetTag;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.GetMask("Entity") && other.gameObject.tag == targetTag)
        {
            reference.OnHit(other);
        }
    }

    public void SetTargetTag(string tag)
    {
        targetTag = tag; 
    }

    public void SetReference(Hitbox hb)
    {
        reference = hb;
    }
}
