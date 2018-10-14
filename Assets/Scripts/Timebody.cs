using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timebody : MonoBehaviour
{
    bool isRewinding = false;

    private float recordTime = 3f;

    private float shrinkTime = .1f;

    private float timeBetweenSizeChanges = .75f;

    private float growTime = .1f;

    public static bool finishedRewinding = false;

    List<PointInTime> pointsInTime;

    Rigidbody rb;

    // Use this for initialization
    void Awake()
    {
        pointsInTime = new List<PointInTime>();
        rb = GetComponent<Rigidbody>();
        finishedRewinding = false;
    }

    void FixedUpdate()
    {
        if (isRewinding)
            Rewind();
        else
            Record();
    }

    void Rewind()
    {
        if (pointsInTime.Count > 0)
        {
            PointInTime pointInTime = pointsInTime[0];
            transform.position = pointInTime.position;
            transform.rotation = pointInTime.rotation;
            pointsInTime.RemoveAt(0);
        }
        else
        {
            Timebody tb = GetComponent<Timebody>();
            finishedRewinding = true;
            Destroy(tb);
        }
    }

    void Record()
    {
        if (pointsInTime.Count < Mathf.Round(recordTime / Time.fixedDeltaTime))
        {
            pointsInTime.Insert(0, new PointInTime(transform.position, transform.rotation));
        }
    }

    public void RewindTime()
    {
        rb.isKinematic = true;
        StartCoroutine(ShrinkGrowRewind());
    }

    IEnumerator ShrinkGrowRewind()
    {
        Vector3 startScale = transform.localScale;

        float elapsed = 0f;

        while (elapsed < shrinkTime)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / shrinkTime);
            yield return null;
        }

        yield return new WaitForSeconds(timeBetweenSizeChanges);

        isRewinding = true;

        elapsed = 0f;

        while (elapsed < growTime)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, startScale, elapsed / growTime);
            yield return null;
        }
    }

    public void StopRewind()
    {
        isRewinding = false;
        rb.isKinematic = false;
    }
}
