using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowNormalRight : MonoBehaviour
{
	// Update is called once per frame
	void Update ()
    {
        Debug.DrawRay(transform.position, -transform.up * 5f, Color.magenta);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(transform.position, -transform.up, out hit, 10f))
        {
            Debug.DrawRay(hit.point, hit.normal * 5f, Color.yellow);
            Debug.DrawRay(hit.point, hit.normal.Flat() * 5f, Color.green);
            Vector3 rightVector = Vector3.Cross(Vector3.up, hit.normal.Flat());
            Debug.DrawRay(hit.point, rightVector * 5f, Color.blue);

            Debug.DrawRay(hit.point, Vector3.ProjectOnPlane(Vector3.forward, hit.normal) * 5f, Color.black);
        }
	}
}
