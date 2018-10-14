using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitBoxShape
{
    Cube,
    Sphere,
    Capsule
}

[CreateAssetMenu(fileName = "New Hitbox", menuName = "Combat/Hitbox")]
public class Hitbox : ScriptableObject
{
    public new string name = "New hitbox";
    public string targetTag = "Enemy";
    public HitBoxShape shape;
    public Vector3 positionOffSet = Vector3.zero;
    public Vector3 rotationOffSet = Vector3.zero;
    public Vector3 scaleOffSet = Vector3.zero;
    public int startFrame = 0;
    public int endFrame = 4;
    public float baseDamage = 8;
    public float baseKnockback = 5;
    public float knockbackScaling = .5f;
    public Vector3 baseAngle;
    public float baseHitstun = 10;
    public float hitstunScaling = 5;
    public float hitpause = 2;
    public int priority = 1;
    public Vector3 movementForceDirection = Vector3.zero;
    public float movementForce = 0;
    //VFX (Attack smear)
    //VFX on hit (Particles)
    //SFX on hit
    //Status effect
    private GameObject hitboxObject;
    private GameObject instanceOfHitbox;
    private DetectEntityCollisions dec;
    private GameObject currentEntity;
    private AttackType attackType;

    private void Awake()
    {
        hitboxObject = Resources.Load("Hitbox", typeof(GameObject)) as GameObject;
    }

    //Spawns a physical hitbox and sets up references
    public void SpawnHitbox(Transform entity, AttackType at)
    {
        //Instantiate a physical hitbox
        instanceOfHitbox = Instantiate(hitboxObject, entity.position + positionOffSet, entity.rotation * Quaternion.Euler(rotationOffSet), entity);
        instanceOfHitbox.transform.localScale = scaleOffSet;
        //Get a reference to the script the only detects collisions
        dec = instanceOfHitbox.GetComponent<DetectEntityCollisions>();
        //Setup a reference between this script and the collision detector script
        dec.SetReference(this);
        //Declare the type of entity (Enemy or player) that the hitbox will be looking for 
        dec.SetTargetTag(targetTag);

        BoxCollider bcol = instanceOfHitbox.GetComponent<BoxCollider>();
        SphereCollider scol = instanceOfHitbox.GetComponent<SphereCollider>();
        CapsuleCollider ccol = instanceOfHitbox.GetComponent<CapsuleCollider>();

        //Enable the target shape of this hitbox
        if (shape == HitBoxShape.Cube)
        {
            bcol.enabled = true;
            Destroy(scol);
            Destroy(ccol);
        }
        else if (shape == HitBoxShape.Sphere)
        {
            scol.enabled = true;
            Destroy(bcol);
            Destroy(ccol);
        }
        else if (shape == HitBoxShape.Capsule)
        {
            ccol.enabled = true;
            Destroy(bcol);
            Destroy(scol);
        }

        //Disable the object until an attack is used
        instanceOfHitbox.SetActive(false);
        //Setup a reference to the current entity that this hitbox is attached to
        currentEntity = entity.gameObject;
        attackType = at;

        //Debug.Log(name + " spawned.");
    }

    //Destroys the physical hitbox
    public void DestroyHitbox()
    {
        if (instanceOfHitbox != null)
        {
            Destroy(instanceOfHitbox);
            instanceOfHitbox = null;
        }
    }

    public void Enable(bool status)
    {
        instanceOfHitbox.SetActive(status);
        //Debug.Log(name + " status is updated.");
    }

    public void OnHit(Collider col)
    {
        currentEntity.GetComponent<CombatModule>().InflictDamage(this, col, attackType);
    }
}
