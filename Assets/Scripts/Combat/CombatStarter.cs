using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

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
    private bool canStopCombat = false;
    private bool start = false;
    private bool stop = false;
    private Vector3 startLocation = Vector3.zero;

    public CinemachineVirtualCamera[] personalCams;

    public CinemachineBrain brainOfCamera;

    private void Awake()
    {
        bc = GetComponent<BoxCollider>();
        bc.size = new Vector3 (widthOfArena * 2f, heightOfArena, widthOfArena * 2f);
        bc.center = Vector3.up * percHeightCorr * heightOfArena;
        bc.enabled = false;

        explosRad = widthOfArena * 15f;

        SetupOffsets();

        //StartCombat();       
    }

    /// <summary>Call this to put in a request to start combat when possible</summary>
    public void StartCombat(Vector3 location)
    {
        startLocation = location;
        start = true;
        stop = false;

        //Debug.Log("Starting!");

    }

    /// <summary>Call this to put in a request to stop combat when possible</summary>
    public void StopCombat()
    {
        stop = true;
        start = false;
    }

    private void FixedUpdate()
    {
        if (start && canStartCombat)
        {
            start = false;
            StartCoroutine(SetupCombat());
        }

        if (stop && canStopCombat)
        {
            stop = false;
            StartCoroutine(TeardownCombat());
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCombat(GameObject.Find("Player 1").transform.GetChild(2).position);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            StopCombat();
        }
    }

    /// <summary>The actual process to get combat setup</summary>
    private IEnumerator SetupCombat()
    {
        Stopwatch totalTime = new Stopwatch();

        totalTime.Start();

        canStartCombat = false;
        canStopCombat = false;

        transform.position = startLocation;

        beforeArenaLocation = transform.position;
        yield return new WaitForSeconds(.1f); //Give objects enough time to trigger OnTriggerEnter()
        bc.enabled = true;
        yield return new WaitForSeconds(.1f); //Give objects enough time to trigger OnTriggerEnter()

        //Clone objects to new location
        CloneAndMoveObjectsToArena();

        yield return new WaitForSeconds(.1f);

        brainOfCamera.enabled = true;

        //transform.position = combatArenaLocation;

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
                //Debug.Log("Found one! " + g.name);
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

        canStartCombat = false;
        canStopCombat = true;

        totalTime.Stop();

        UnityEngine.Debug.Log("Total time: " + totalTime.Elapsed);
        //yield return new WaitForSeconds(3f);

        //StartCoroutine(EndCombat());
    }

    /// <summary>The actual process to get combat torn down</summary>
    private IEnumerator TeardownCombat()
    {
        canStartCombat = false;
        canStopCombat = false;

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

        brainOfCamera.enabled = false;

        foreach (GameObject e in entityObjects)
        {
            e.transform.position = beforeArenaLocation + (e.transform.position - transform.position);
        }

        yield return new WaitForSeconds(0.1f);

        brainOfCamera.enabled = true;

        transform.position = beforeArenaLocation;

        clonedCombatObjects = null;
        combatObjects = new List<GameObject>();
        entityObjects = new List<GameObject>();
        rightObjects = null;
        allSlicedObjects = null;
        rigidbodyObjects = null;

        canStartCombat = true;
        canStopCombat = false;
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
            clonedCombatObjects[i] = Instantiate(combatObjects[i], displacement + transform.position, oldrotation, transform) as GameObject;
            clonedCombatObjects[i].transform.localScale = combatObjects[i].transform.localScale;
            clonedCombatObjects[i].tag = "CombatEnvironment";

            if (combatObjects[i].GetComponent<Rigidbody>())
            {
                combatObjects[i].GetComponent<Rigidbody>().isKinematic = true;
            }
        }

        for (int i = 0; i < entityObjects.Count; i++)
        {
            entityObjects[i].transform.position = combatArenaLocation + (entityObjects[i].transform.position - transform.position);
        }

        brainOfCamera.enabled = false;

        transform.position = combatArenaLocation;

        bc.enabled = false;
    }

    /// <summary>Slices all environmental objects</summary>
    private void SliceObjects()
    {
        //For each cut to the environment (4 total)
        for (int i = 0; i < raycastPositionOffsets.GetLength(0); i++)
        {
            int tempIteration = i + 1;
            UnityEngine.Debug.Log("Slice: " + tempIteration + "/4");
 
            Stopwatch timeForCurrentCut = new Stopwatch();
            timeForCurrentCut.Start();

            hits = Physics.BoxCastAll(raycastPositionOffsets[i, 0], new Vector3(0f, heightOfArena * 2f, 1f), raycastPositionOffsets[i, 1], Quaternion.LookRotation(raycastPositionOffsets[i, 1], Vector3.up), raycastOffset * 2f, ULayTags.cuttableLayers);

            //For the current cut, 
            //for each object the cut hit,
            foreach (RaycastHit hit in hits)
            {
                UnityEngine.Debug.Log("Getting a list of objects to cut for " + hit.collider.gameObject.name + "...");

                Stopwatch timeForCurrentObject = new Stopwatch();
                timeForCurrentObject.Start();

                listOfHitPoints.Add(hit.point);

                if (hit.collider.tag == "CombatEnvironment")
                {
                    //For the current cut, 
                    //for the current object the cut hit, 
                    //add all children to cut
                    List<GameObject> victims = new List<GameObject>();

                    //We first need to check if the primary object has a mesh renderer on it
                    GameObject parentVictim = hit.collider.gameObject;
                    if (parentVictim.GetComponent<MeshRenderer>())
                        victims.Add(parentVictim);
                    //Destroy(parentVictim.GetComponent<Collider>());

                    //We now need to check if the primary object has children AND if the children have mesh renderers
                    for (int c = 0; c < parentVictim.transform.childCount; c++)
                    {
                        Transform currentChild = parentVictim.transform.GetChild(c);

                        if (currentChild.GetComponent<MeshRenderer>())
                        {
                            victims.Add(currentChild.gameObject);
                        }
                        //If a child has a LODGroup attached to it, it's going to have more children as well that will have mesh renderers
                        if (currentChild.GetComponent<LODGroup>())
                        {
                            //Keep the nesting going...
                            for (int k = 0; k < currentChild.childCount; k++)
                            {
                                Transform currentChildChild = currentChild.GetChild(k);

                                if (currentChildChild.GetComponent<MeshRenderer>())
                                {
                                    victims.Add(currentChildChild.gameObject);
                                }
                            }
                        }
                    }

                    //For the current cut, 
                    //for the current object the cut hit, 
                    //for either the object itself or one of its children, 
                    foreach (GameObject victim in victims)
                    {
                        UnityEngine.Debug.Log("Cutting " + victim.name + "...");

                        Stopwatch timeForCurrentVictim = new Stopwatch();
                        timeForCurrentVictim.Start();

                        //Cut the mesh
                        GameObject[] pieces = BLINDED_AM_ME.MeshCut.Cut(victim, raycastPositionOffsets[i, 0], -Vector3.Cross(raycastPositionOffsets[i, 1], Vector3.up), capMaterial);

                        //For the current cut, 
                        //for the current object the cut hit, 
                        //for either the object itself or one of its children, 
                        //for both pieces after the mesh cut
                        for (int j = 0; j < pieces.Length; j++)
                        {
                            if (pieces[j].GetComponent<Collider>())
                            {
                                //Destroy(pieces[j].GetComponent<Collider>());
                            }

                            //All right pieces need to have rigidbodies to be able to explode away from the arena
                            if (j == 1)
                            {
                                if (pieces[j].GetComponent<Rigidbody>())
                                {
                                    Rigidbody rb = pieces[j].GetComponent<Rigidbody>();
                                    Destroy(rb);
                                }

                                if (!rigidbodyObjects.Contains(pieces[j]))
                                    rigidbodyObjects.Add(pieces[j]);
                            }
                            //Left pieces will only need a rigidbody if they have one already
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

                            //Add the current piece to a master pieces game object list
                            if (!allSlicedObjects.Contains(pieces[j]))
                                allSlicedObjects.Add(pieces[j]);
                        }

                        //Add the RIGHT piece to a master RIGHT pieces game object list
                        if (!rightObjects.Contains(pieces[1]))
                            rightObjects.Add(pieces[1]);

                        timeForCurrentVictim.Stop();
                        UnityEngine.Debug.Log("Time for victim " + victim.name + " is " + timeForCurrentVictim.Elapsed);
                    }                    
                }

                timeForCurrentObject.Stop();
                UnityEngine.Debug.Log("Time for object " + hit.collider.name + " is " + timeForCurrentObject.Elapsed);
            }

            timeForCurrentCut.Stop();
            UnityEngine.Debug.Log("Time for cut " + tempIteration + " is " + timeForCurrentCut.Elapsed);
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
