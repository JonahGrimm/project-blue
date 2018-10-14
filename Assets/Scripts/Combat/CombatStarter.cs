using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CombatStarter : MonoBehaviour
{
    List<GameObject> combatObjects = new List<GameObject>();
    List<GameObject> entityObjects = new List<GameObject>();
    List<GameObject> rightObjects = new List<GameObject>();
    List<GameObject> allSlicedObjects = new List<GameObject>();
    List<GameObject> rigidbodyObjects = new List<GameObject>();

    List<Vector3> listOfHitPoints = new List<Vector3>();
    RaycastHit[] hits;

    public delegate void RewindTimebodies();
    public event RewindTimebodies RewindTime;

    private Vector3 displacement;
    private Quaternion oldrotation;
    GameObject[] clonedCombatObjects;
    public float widthOfArena = 40f;
    public float heightOfArena = 80f;
    public float percHeightCorr = .45f;
    public Vector3 combatArenaLocation;
    private Vector3 beforeArenaLocation;
    public float raycastOffset = 100f;
    BoxCollider bc;
    public Material capMaterial;
    public float dragOfNewObject = 1f;
    public float angularDragOfNewObject = 1f;

    public float explosStrength = 10f;
    private float explosRad;
    public float explosUpForce = 1f;

    Vector3[,] raycastPositionOffsets;

    private bool canStartCombat = true;

    public CinemachineVirtualCamera[] personalCams;

    private void Awake()
    {
        bc = GetComponent<BoxCollider>();
        bc.size = new Vector3 (widthOfArena * 2f, heightOfArena, widthOfArena * 2f);
        bc.center = Vector3.up * percHeightCorr * heightOfArena;
        bc.enabled = false;

        explosRad = widthOfArena * 15f;

        SetupOffsets();

        StartCoroutine(StartCombat());

        
    }

    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canStartCombat)
        {
            StartCoroutine(StartCombat());
            canStartCombat = false;
        }
    }

    /// <summary>Initializes combat</summary>
    IEnumerator StartCombat()
    {
        canStartCombat = false;
        beforeArenaLocation = transform.position;
        yield return new WaitForSeconds(.1f); //Give objects enough time to trigger OnTriggerEnter()
        bc.enabled = true;
        yield return new WaitForSeconds(.1f); //Give objects enough time to trigger OnTriggerEnter()

        //Clone objects to new location
        CloneAndMoveObjectsToArena();

        yield return new WaitForSeconds(.1f);

        rightObjects = new List<GameObject>();
        allSlicedObjects = new List<GameObject>();
        rigidbodyObjects = new List<GameObject>();

        //Slice the objects
        SliceObjects();

        //Take a brief pause to give everything a chance to update
        yield return new WaitForSeconds(.1f);


        foreach (GameObject g in clonedCombatObjects)
        {
            if (g.GetComponent<Rigidbody>())
            {
                Debug.Log("Found one! " + g.name);
                rigidbodyObjects.Add(g);
                Destroy(g.GetComponent<Rigidbody>());
            }
        }

        //Add mesh colliders BACK to all environmental objects
        foreach (GameObject m in allSlicedObjects)
        {
            m.AddComponent<MeshCollider>();
            m.GetComponent<MeshCollider>().convex = true;
        }

        //Take a brief pause to give everything a chance to update
        yield return new WaitForSeconds(.1f);

        //Add rigidbody components BACK to all specified objects
        foreach (GameObject r in rigidbodyObjects)
        {
            r.AddComponent<Rigidbody>();
            r.GetComponent<Rigidbody>().drag = dragOfNewObject;
            r.GetComponent<Rigidbody>().angularDrag = angularDragOfNewObject;
            r.AddComponent<Timebody>();
            Timebody tb = r.GetComponent<Timebody>();
            RewindTime += tb.RewindTime;
        }

        //Take a brief pause to give everything a chance to update
        yield return new WaitForSeconds(.1f);

        //Explode all objects outside the arena
        foreach (GameObject o in rightObjects)
        {
            o.GetComponent<Rigidbody>().AddExplosionForce(explosStrength, transform.position, explosRad, explosUpForce, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(3f);

        StartCoroutine(EndCombat());
    }

    IEnumerator EndCombat()
    {
        RewindTime();

        foreach (GameObject r in rigidbodyObjects)
        {
            Timebody tb = r.GetComponent<Timebody>();
            RewindTime -= tb.RewindTime;
        }

        bc.enabled = false;

        while (!Timebody.finishedRewinding)
        {
            yield return null;
        }

        foreach (GameObject o in clonedCombatObjects)
        {
            Destroy(o);
        }

        foreach (GameObject o in combatObjects)
        {
            if (o.GetComponent<Rigidbody>())
                o.GetComponent<Rigidbody>().isKinematic = false;
        }

        foreach (GameObject o in rightObjects)
        {
            Destroy(o);
        }

        //brainOfCamera.enabled = false;

        foreach (GameObject e in entityObjects)
        {
            e.transform.position -= combatArenaLocation;
        }

        yield return new WaitForSeconds(0.1f);

        //brainOfCamera.enabled = true;

        transform.position = beforeArenaLocation;

        clonedCombatObjects = null;
        combatObjects = new List<GameObject>();
        entityObjects = new List<GameObject>();
        rightObjects = null;
        allSlicedObjects = null;
        rigidbodyObjects = null;

        canStartCombat = true;
    }

    /// <summary>Finds objects to send to the combat arena</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == ULayTags.environmentTag)
        {
            combatObjects.Add(other.gameObject);
        }
        //TODO gameObject.layer is an INDEX where entityLayer is in binary
        else if (other.gameObject.tag == ULayTags.playerTag || other.gameObject.tag == ULayTags.enemyTag)
        {
            Debug.Log("Added entity!");
            entityObjects.Add(other.gameObject);
        }
    }

    /// <summary>Clones and move objects to the arena</summary>
    private void CloneAndMoveObjectsToArena()
    {
        clonedCombatObjects = new GameObject[combatObjects.Count];
        for (int i = 0; i < combatObjects.Count; i++)
        {
            displacement = combatObjects[i].transform.position - transform.position;
            oldrotation = combatObjects[i].transform.rotation;
            clonedCombatObjects[i] = Instantiate(combatObjects[i], displacement, oldrotation, transform) as GameObject;
            clonedCombatObjects[i].transform.localScale = combatObjects[i].transform.localScale;
            clonedCombatObjects[i].tag = "CombatEnvironment";

            if (combatObjects[i].GetComponent<Rigidbody>())
            {
                combatObjects[i].GetComponent<Rigidbody>().isKinematic = true;
            }
        }

        for (int i = 0; i < entityObjects.Count; i++)
        {
            entityObjects[i].transform.position += combatArenaLocation;
        }

        //brainOfCamera.enabled = false;

        transform.position = combatArenaLocation;

        bc.enabled = false;
    }

    /// <summary>Slices all environmental objects</summary>
    private void SliceObjects()
    {
        for (int i = 0; i < raycastPositionOffsets.GetLength(0); i++)
        {
            hits = Physics.BoxCastAll(raycastPositionOffsets[i, 0], new Vector3(0f, heightOfArena * 2f, 1f), raycastPositionOffsets[i, 1], Quaternion.LookRotation(raycastPositionOffsets[i, 1], Vector3.up), raycastOffset * 2f, ULayTags.cuttableLayers);

            foreach (RaycastHit hit in hits)
            {
                listOfHitPoints.Add(hit.point);

                if (hit.collider.tag == "CombatEnvironment")
                {
                    GameObject victim = hit.collider.gameObject;

                    GameObject[] pieces = BLINDED_AM_ME.MeshCut.Cut(victim, raycastPositionOffsets[i, 0], -Vector3.Cross(raycastPositionOffsets[i, 1], Vector3.up), capMaterial);

                    for (int j = 0; j < pieces.Length; j++)
                    {
                        Destroy(pieces[j].GetComponent<Collider>());

                        if (j == 1)
                        {
                            if (pieces[j].GetComponent<Rigidbody>())
                            {
                                Rigidbody rb = pieces[j].GetComponent<Rigidbody>();
                                Destroy(rb);

                                if (!rigidbodyObjects.Contains(pieces[j]))
                                    rigidbodyObjects.Add(pieces[j]);
                            }
                            else
                            {
                                if (!rigidbodyObjects.Contains(pieces[j]))
                                    rigidbodyObjects.Add(pieces[j]);
                            }
                        }
                        else if (j == 0)
                        {
                            if (pieces[j].GetComponent<Rigidbody>())
                            {
                                Rigidbody rb = pieces[j].GetComponent<Rigidbody>();
                                Destroy(rb);

                                if (!rigidbodyObjects.Contains(pieces[j]))
                                    rigidbodyObjects.Add(pieces[j]);
                            }
                        }

                        if (!allSlicedObjects.Contains(pieces[j]))
                            allSlicedObjects.Add(pieces[j]);

                    }

                    if (!rightObjects.Contains(pieces[1]))
                        rightObjects.Add(pieces[1]);
                }
            }
        }
    }

    /// <summary>Sets up the different offset values for future slicing</summary>
    private void SetupOffsets()
    {
        raycastPositionOffsets = new Vector3[4, 2];

        raycastPositionOffsets[0, 0] = combatArenaLocation + Vector3.right * widthOfArena - Vector3.forward * raycastOffset;
        raycastPositionOffsets[0, 1] = Vector3.forward;

        raycastPositionOffsets[1, 0] = combatArenaLocation + Vector3.forward * widthOfArena + Vector3.right * raycastOffset;
        raycastPositionOffsets[1, 1] = -Vector3.right;

        raycastPositionOffsets[2, 0] = combatArenaLocation - Vector3.right * widthOfArena + Vector3.forward * raycastOffset;
        raycastPositionOffsets[2, 1] = -Vector3.forward;

        raycastPositionOffsets[3, 0] = combatArenaLocation - Vector3.forward * widthOfArena - Vector3.right * raycastOffset;
        raycastPositionOffsets[3, 1] = Vector3.right;

    }

    /*void OnDrawGizmos()
    {
        if (EditorApplication.isPlaying)
        {
            Gizmos.color = Color.red;

            for (int i = 0; i < raycastPositionOffsets.GetLength(0); i++)
            {
                //Draw a Ray forward from GameObject toward the maximum distance
                Gizmos.DrawRay(raycastPositionOffsets[i, 0], raycastPositionOffsets[i, 1] * raycastOffset * 2f);
                //Draw a cube at the maximum distance
                Gizmos.DrawWireCube(raycastPositionOffsets[i, 0] + raycastPositionOffsets[i, 1] * raycastOffset * 2f, new Vector3(0f, heightOfArena * 2f, 1f));
            }

            foreach (Vector3 p in listOfHitPoints)
            {
                Gizmos.DrawWireCube(p, Vector3.one * 5f);
            }
        }
    }*/
}