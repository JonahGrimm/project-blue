using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineTargetGroup))]
public class TargetGroupAutoAssigner : MonoBehaviour
{
    private CinemachineTargetGroup ctg;
    public float weightWeightPerc = .75f;
    public float playerWeightPerc = .8f;
    public float enemyWeightPerc = .2f;
    public bool updateTargets = false;

    // Use this for initialization
    void Start ()
    {
        UpdateTargets();
	}

    private void UpdateTargets()
    {
        ctg = GetComponent<CinemachineTargetGroup>();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] weights = GameObject.FindGameObjectsWithTag("Weight");
        ctg.m_Targets = new CinemachineTargetGroup.Target[players.Length + enemies.Length + weights.Length];

        float weightWeight = (players.Length + enemies.Length) * weightWeightPerc;
        float playerWeight = (players.Length + enemies.Length) * playerWeightPerc;
        float enemyWeight = (players.Length + enemies.Length) * enemyWeightPerc;

        for (int i = 0; i < ctg.m_Targets.Length; i++)
        {
            if (i < weights.Length)
            {
                ctg.m_Targets[i].target = weights[i].transform;
                ctg.m_Targets[i].weight = weightWeight;
            }
            if (i >= weights.Length && i < players.Length + weights.Length)
            {
                ctg.m_Targets[i].target = players[i - weights.Length].transform;
                ctg.m_Targets[i].weight = playerWeight;
            }
            if (i >= players.Length + weights.Length && i < enemies.Length + players.Length + weights.Length)
            {
                ctg.m_Targets[i].target = enemies[i - (players.Length + weights.Length)].transform;
                ctg.m_Targets[i].weight = enemyWeight;
            }
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
		if (updateTargets)
        {
            UpdateTargets();
            updateTargets = false;
        }
	}
}
