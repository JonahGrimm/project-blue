using UnityEngine;
using System.Collections;

public class ExampleUseof_MeshCut : MonoBehaviour
{

	public Material capMaterial;
	
	void Update(){

		if(Input.GetKeyDown(KeyCode.Space))
        {
			RaycastHit[] hits;

            hits = Physics.RaycastAll(transform.position, transform.forward);

            foreach (RaycastHit hit in hits)
            {

                GameObject victim = hit.collider.gameObject;

                GameObject[] pieces = BLINDED_AM_ME.MeshCut.Cut(victim, transform.position, transform.right, capMaterial);

                Destroy(pieces[0].GetComponent<Collider>());
                pieces[0].AddComponent<MeshCollider>();
                pieces[0].GetComponent<MeshCollider>().convex = true;

                Destroy(pieces[1]);

                //Destroy(pieces[0].GetComponent<Rigidbody>());
                //pieces[0].AddComponent<Rigidbody>();
                //pieces[0].GetComponent<Rigidbody>().drag = 4;
                //pieces[0].GetComponent<Rigidbody>().angularDrag = 4;


                /*if (!pieces[1].GetComponent<Rigidbody>())
                {
                    pieces[1].AddComponent<Rigidbody>();
                    pieces[1].GetComponent<Rigidbody>().drag = 4;
                    pieces[1].GetComponent<Rigidbody>().angularDrag = 4;
                }*/
               
            }

            //Destroy(pieces[1], 1);
		}
	}

	void OnDrawGizmosSelected() {

		Gizmos.color = Color.green;

		Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5.0f);
		Gizmos.DrawLine(transform.position + transform.up * 0.5f, transform.position + transform.up * 0.5f + transform.forward * 5.0f);
		Gizmos.DrawLine(transform.position + -transform.up * 0.5f, transform.position + -transform.up * 0.5f + transform.forward * 5.0f);

		Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.5f);
		Gizmos.DrawLine(transform.position,  transform.position + -transform.up * 0.5f);

	}

}
