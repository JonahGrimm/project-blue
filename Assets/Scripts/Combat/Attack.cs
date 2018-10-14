using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    Jab,
    Ftilt,
    Utilt,
    Dtilt,
    Dashattack,
    Nair,
    Fair,
    Bair,
    Uair,
    Dair,
    Fcharge,
    Ucharge,
    Dcharge,
    Nspecial,
    Fspecial,
    Uspecial,
    Dspecial
}

[CreateAssetMenu(fileName = "New Attack", menuName = "Combat/Attack")]
public class Attack : ScriptableObject
{
    public new string name = "New attack";
    public string description = "New description";
    public AttackType attackType;
    public int StartLag = 8;
    public int WhiffEndLag = 8;
    public int HitEndLag = 3;
    public int LandLag = 6;
    public Hitbox[] hitbox;

    private int totalDurationOnWhiff;
    private int totalDurationOnHit;
    private int[] hitboxEnableFrames;
    private int[] hitboxDisableFrames;
    private int lastEndFrame;
    //VFX on startup (potential)
    //SFX on startup
    //Animation

    private void Awake()
    {
        CalculateValues();
    }

    private void CalculateValues()
    {
        lastEndFrame = 0;

        int l = hitbox.Length;
        hitboxEnableFrames = new int[l];
        hitboxDisableFrames = new int[l];

        for (int i = 0; i < l; i++)
        {
            if (hitbox[i].endFrame > lastEndFrame)
            {
                lastEndFrame = hitbox[i].endFrame;
            }
            hitboxEnableFrames[i] = hitbox[i].startFrame;
            hitboxDisableFrames[i] = hitbox[i].endFrame;
        }

        totalDurationOnWhiff = StartLag + lastEndFrame + WhiffEndLag;
        totalDurationOnHit = StartLag + lastEndFrame + HitEndLag;
    }

    public void SpawnHitboxes(Transform entity)
    {
        CalculateValues();
        foreach (Hitbox hb in hitbox)
        {
            hb.SpawnHitbox(entity, attackType);
        }
    }

    public void DestroyHitboxes()
    {
        foreach (Hitbox hb in hitbox)
        {
            hb.DestroyHitbox();
        }
    }

    public void UpdateHitboxStatus(int frame)
    {
        //Debug.Log(name + " is updating hitbox status.");

        for (int i = 0; i < hitbox.Length; i++)
        {
            if (hitboxEnableFrames[i] == frame)
            {
                //Debug.Log(hitbox[i].name + " is enabling.");

                hitbox[i].Enable(true);
            }
            if (hitboxDisableFrames[i] == frame)
            {
                //Debug.Log(hitbox[i].name + " is disabling.");

                hitbox[i].Enable(false);
            }
        }
    }

    public int GetTotalDuration()
    {
        return totalDurationOnWhiff;
    }

    public int GetTotalDurationOnHit()
    {
        return totalDurationOnHit;
    }

    public int GetLastEndFrame()
    {
        return lastEndFrame;
    }
}
