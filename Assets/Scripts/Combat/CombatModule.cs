using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StateController))]
public class CombatModule : MonoBehaviour
{
    public Attack[] activeAttacks = new Attack[17];
    public Attack temp;

    public bool AreHitboxesLoaded = false;

    private void Start()
    {
        LoadAttacks();
    }

    private void FixedUpdate()
    {
        /*if (Input.GetKeyDown(KeyCode.Space))
            SetAttack(temp);*/
    }

    //Assigns an attack to its respective slot
    public void SetAttack(Attack attack)
    {
        if (activeAttacks[(int)attack.attackType] != null)
        {
            activeAttacks[(int)attack.attackType] = attack;
            LoadAttacks();
        }
    }

    //Spawns all of the actual collision boxes
    public void LoadAttacks()
    {
        UnloadAttacks();

        foreach (Attack attack in activeAttacks)
        {
            if (attack != null)
                attack.SpawnHitboxes(transform);
        }

        AreHitboxesLoaded = true;
    }

    //Unloads all of the actual collision boxes by destroying them
    public void UnloadAttacks()
    {
        foreach (Attack attack in activeAttacks)
        {
            if (attack != null)
                attack.DestroyHitboxes();
        }

        AreHitboxesLoaded = false;
    }

    //Inflicts damage on a target
    public void InflictDamage(Hitbox hb, Collider target, AttackType at)
    {
        Debug.Log("Inflicting damage to: " + target.name + " from attack type: " + at);
    }

    public void InitializeAttack(AttackType at, out int frameDuration)
    {
        Debug.Log("Attack Initialized.");
        frameDuration = activeAttacks[(int)at].GetTotalDuration();
        //Play animation o/s
    }

    public void Attack(AttackType at, int currentFrame)
    {
        //Convert current frame to correct frame number for hitboxes 
        currentFrame -= activeAttacks[(int)at].GetLastEndFrame() + activeAttacks[(int)at].WhiffEndLag;

        //If negative its past startup (STILL RUNS DURING ENDLAG)
        if (currentFrame <= 0)
        {
            currentFrame = Mathf.Abs(currentFrame);

            activeAttacks[(int)at].UpdateHitboxStatus(currentFrame);
        }
        //If positive its in startup
        else 
        {

        }

        //TODO return on hit end lag & landing lag as necessary
    }
}
