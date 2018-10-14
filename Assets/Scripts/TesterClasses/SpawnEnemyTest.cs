using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class SpawnEnemyTest : MonoBehaviour
{
    public int playerNumber = 0;
    private Player p;

    public GameObject enemy;

    private void Start()
    {
        p = ReInput.players.GetPlayer(playerNumber);
    }

    private void Update()
    {
        if (p.GetButtonDown("Pause"))
            Instantiate(enemy, Vector3.up * 50f, Quaternion.identity);

    }
}
