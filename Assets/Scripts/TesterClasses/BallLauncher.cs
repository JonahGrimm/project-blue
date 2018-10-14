using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallLauncher : MonoBehaviour
{
    private Rigidbody rb;
    public Transform start;
    public Transform target;

    public float peakHeight = 20;
    public float gravity = -9;

	void Start ()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
	}

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            transform.position = start.position;
            Launch();
        }
    }

    void Launch()
    {
        Physics.gravity = Vector3.up * gravity;
        rb.useGravity = true;
        rb.velocity = CalculateLaunchVelocity();
        Debug.Log(rb.velocity);
    }

    Vector3 CalculateLaunchVelocity()
    {
        Vector3 displacement = target.position - rb.position;
        Vector3 velocity = displacement.Flat() / 
                            (Mathf.Sqrt(-2 * peakHeight / gravity) + 
                             Mathf.Sqrt(2 * (displacement.y - peakHeight) / gravity) );
        velocity.y = Mathf.Sqrt(-2 * gravity * peakHeight);

        return velocity;

    }
}
