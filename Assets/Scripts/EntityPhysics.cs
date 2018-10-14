using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Handles all physics to this specific entity.  It will update the animator
//as needed.
//[RequireComponent(typeof(StateController))]
//[RequireComponent(typeof(Rigidbody))]
//[RequireComponent(typeof(CapsuleCollider))]
public class EntityPhysics : MonoBehaviour
{
    //References
    private Animator animator;
    [HideInInspector]
    public Rigidbody rb;
    private Rigidbody predictorRB;
    private StateController stateController;
    public GameObject predictor;

    //Animator
    readonly int a_VerticalSpeed = Animator.StringToHash("VerticalSpeed");

    //Public parameters to control class, m_ is for modifiable
    private float m_GroundCheckDistance;          //Distance to check if grounded by default/in air
    private float m_GroundNormalCheckDistance;    //Distance to check if grounded if was previously grounded (used primarily for keeping player attached to slopes)
    private float m_WavedashGroundCheckDistance;

    private float m_AirSpeedAccel;                //Accel used when moving in air
    private float m_WalkSpeedAccel;               //Accel used when walking
    private float m_RunSpeedAccel;                //Accel used when running

    private float m_MaximumWalkSpeed;             //Max speed for walking
    private float m_MaximumRunSpeed;              //Max speed for running
    private float m_MaximumAirSpeed;              //Max speed for when in air
    private float m_MaximumWallJumpSpeed;

    private float m_RotationStrength;             //The default rotation strength for all the visible rotation calculations
    private float m_DragChangeStrength;
    private float m_MaxSpeedShrinkSpeed;          //The strength or rate at which the current max speed shrinks to match the target max speed
    private float m_AngleDiffLerpStrength;        //The strength at which the ForwardAngleDiffVector lerps towards the current visible entity forward

    private float m_DefaultDrag;                  //The default rigidbody drag
    private float m_AirDrag;
    private float m_CrouchDrag;                   //The default rigidbody drag when crouched (very small for increased sliding)
    private float m_AirDodgeDrag;
    private float m_GroundDodgeDrag;
    //private float m_WavedashDrag = 1.5f;

    private float m_StickForce;                   
    private float m_JumpForce;
    private float m_AirJumpForce;
    private float m_WallJumpForce;
    private float m_FastfallForce;
    private float m_GroundDodgeForce;
    private float m_AirDodgeForce;
    private float m_WavedashForce;
    private float m_AdditionalGravityForce;

    private float m_WallJumpVerticalAngle;
    private float m_CrouchMinSpeedToBeAbleToMove; //The minimum speed required in order to get the reduced drag benefit from crouching

    //Internal variables
    [HideInInspector]
    public bool IsGrounded = false;                                 //Is the entity grounded
    private Vector3 ForwardAngleDiffVector;                         //A vector that slowly lerps towards the velocity vector of the entity
    private float angleDiff;                                        //The difference between the entity's velocity and the ForwardAngleDiffVector
    private bool cappingSpeed;                                      //Is the speed getting capped this update
    private Vector3 groundNormal = Vector3.up;                      //The normal of the ground Vector3.up is the default
    private Quaternion visibleInitialRotation = Quaternion.identity;//The starting visible rotation for the entity
    private Quaternion visibleTargetRotation = Quaternion.identity; //The target visible rotation for the entity
    private Vector3 horizontalVelocity;                             //The velocity of the entity without y
    private float currentMaxSpeed;                                  //The current max speed that is constantly changing and moving targets the target max speed
    private float targetMaxSpeed;                                   //The target max speed
    private float targetDrag;
    private Vector3 oldForward;                                     //Unused
    private float currentRotationStrength;                          //The current rotation strength that will either equal m_OnGroundGroundCheckDistance or m_InAirGroundCheckDistance
    private float currentGroundCheckDistance;                       //Unused
    private bool lockRotation = false;                              //The entity's rotation will only correct to the surface normal but keep the y euler angle when true
    private float lockedRotationEulerAngleY;                         //The entity's rotation when locked
    private IEnumerator platDropCoroutine;
    private RaycastHit hitInfo;
    [HideInInspector]
    public bool IsMoving = false;
    private bool IsDodging = false;
    private bool IsWallJumping = false;
    private bool predictingGround = false;
    private bool predictingAir = false;
    private int platenvironLayerMask;

    public Transform displayModel;


    public ForceMode forceM = ForceMode.Acceleration;

    void Start()
    {        
        //Hehehhehehhe

        rb = GetComponent<Rigidbody>();

        stateController = GetComponent<StateController>();

        animator = stateController.animator;

        //String tag = "Player" + (stateController.GetPlayerNumber() + 1) + "Predictor";
        //Debug.Log(tag);
        GameObject instance = Instantiate(predictor, Vector3.zero, Quaternion.identity);
        predictorRB = instance.GetComponent<Rigidbody>();
        //predictorRB = GameObject.Find("Predictor").GetComponent<Rigidbody>();
        currentMaxSpeed = m_MaximumRunSpeed;
        targetMaxSpeed = m_MaximumRunSpeed;
        ForwardAngleDiffVector = Vector3.forward;

        platenvironLayerMask = LayerMask.GetMask("Environment", "CombatEnvironment", "Platform");
    }

    /*void OnGUI()
    {
        GUI.contentColor = Color.blue;
        GUI.Label(new Rect(10, 10, 500, 500), "Current Max Speed: " + currentMaxSpeed);
        GUI.Label(new Rect(10, 30, 500, 500), "Target Max Speed: " + targetMaxSpeed);
        GUI.Label(new Rect(10, 60, 500, 500), "Current Drag: " + rb.drag);
        GUI.Label(new Rect(10, 80, 500, 500), "Target Drag: " + targetDrag);
    }*/

    void FixedUpdate()
    {
        //Check for if grounded
        CheckGroundStatus();
        //Update the current max speed
        UpdateCurrentMaxSpeed();
        //Update the current drag
        UpdateCurrentDrag();

        if (!IsGrounded && rb.velocity.y > 0)
            //Ascending Jump 1 Layer (Can move through platforms)
            this.gameObject.layer = 10;
        else
            //Apply additional gravity 
            ApplyAdditionalGravity();

        if (platDropCoroutine == null && rb.velocity.y < 0)
            //Default entity layer
            this.gameObject.layer = 14;
        
        //If the entity is idle
        if (stateController.currentState == State.Idle)
        {
            //Speed up max speed capping
            targetMaxSpeed = m_MaximumWalkSpeed;
            UpdateCurrentMaxSpeed();
        }

        //Animator
        animator.SetFloat(a_VerticalSpeed, rb.velocity.y);

        //Keeps the entity from falling out of the world
        if (transform.position.y < -5f)
            transform.position = Vector3.up * 15f;

        if (stateController.currentState != State.Dodge && stateController.currentState != State.AirDodge)
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        else
            rb.constraints = RigidbodyConstraints.FreezeAll;

        //If capping the speed of the entity
        if (cappingSpeed)
        {
            Vector3 predictorHorizontalVelocity;

            if (IsGrounded && stateController.currentState != State.AscentJump1)
                //If grounded we want y velocity too bc we don't know how extreme the normal of the surface is
                predictorHorizontalVelocity = predictorRB.velocity;
            else
                //If it's in the air, we know the y velocity will just be jump/gravity
                predictorHorizontalVelocity = new Vector3(predictorRB.velocity.x, 0f, predictorRB.velocity.z);

            //If the predictor's speed is exceeding the current max speed
            if (predictorHorizontalVelocity.magnitude > currentMaxSpeed)
            {
                //Constrain the predictor's velocity to the current max speed
                predictorHorizontalVelocity = predictorHorizontalVelocity.normalized;
                predictorHorizontalVelocity *= currentMaxSpeed;

                //Set the rigidbody's velocity to the constrained predictor's velocity
                if (IsGrounded && stateController.currentState != State.AscentJump1)
                    //Use full vector if it was grounded since we don't know what the ground slope is
                    rb.velocity = predictorHorizontalVelocity;
                else
                    //Use only the x and z portion of the vector since the y velocity will just be jump/gravity
                    rb.velocity = new Vector3(predictorHorizontalVelocity.x, rb.velocity.y, predictorHorizontalVelocity.z);
            }
            else
            {
                //Add the force as normal since the prediction showed it did not exceed
                if (IsGrounded && stateController.currentState != State.AscentJump1)
                    //Use full vector if it was grounded since we don't know what the ground slope is
                    rb.velocity = predictorRB.velocity;
                else
                    //Use only the x and z portion of the vector since the y velocity will just be jump/gravity
                    rb.velocity = new Vector3(predictorRB.velocity.x, rb.velocity.y, predictorRB.velocity.z);
            }

            predictorRB.velocity = Vector3.zero;

            cappingSpeed = false;
        }
        //If predicting if there will be ground for the entity
        if (predictingGround)
        {
            RaycastHit hit;

            //If previously grounded OR (in the air AND moving downwards)
            if (IsGrounded || (!IsGrounded && predictorRB.velocity.y <= 0.1))
            {
                //Draw a ray of the check distance
                //Debug.DrawRay(predictorRB.position + (predictorRB.transform.up * 0.3f), -predictorRB.transform.up * m_GroundCheckDistance, Color.cyan);

                //Check if entity is on the ground
                if (Physics.Raycast(predictorRB.position + (predictorRB.transform.up * 0.3f), -predictorRB.transform.up, out hit, m_GroundCheckDistance, platenvironLayerMask))
                {
                    transform.position = predictorRB.position;
                }
                //In the air
                else
                {
                    rb.velocity = Vector3.zero;
                }
            }
            //In the air
            else
            {
                rb.velocity = Vector3.zero;
            }

            predictorRB.velocity = Vector3.zero;

            predictingGround = false;
        }
        //If predicting if there will be more air for the entity
        if (predictingAir)
        {
            if (predictorRB.velocity.y <= 0) 
            {
                RaycastHit hit;

                //Draw a ray of the check distance
                //Debug.DrawRay(predictorRB.position + (predictorRB.transform.up * .6f), -predictorRB.transform.up * m_WavedashGroundCheckDistance, Color.cyan);

                //Check if entity is on the ground
                if (Physics.Raycast(predictorRB.position + (predictorRB.transform.up * .6f), -predictorRB.transform.up, out hit, m_WavedashGroundCheckDistance, platenvironLayerMask))
                {
                    stateController.GoToWavedash();
                    predictorRB.position = hit.point;
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                }
            }

            transform.position = predictorRB.position;

            predictorRB.gameObject.layer = 13;

            predictorRB.velocity = Vector3.zero;

            predictingAir = false;
        }

        //Update target drag
        UpdateTargetDrag();

        IsMoving = false;
        IsDodging = false;
        IsWallJumping = false;

        //Draw a red ray representing the ridibody velocity
        //Debug.DrawRay(transform.position + (transform.up * .5f) + (transform.forward * .3f), rb.velocity /*new Vector3(rb.velocity.x, 0f, rb.velocity.z)*/, Color.red);
        //Debug.DrawRay(transform.position + (transform.up * .8f), transform.forward * 5f, Color.black);
        //Debug.Log("state " + stateController.currentState + " vel " + rb.velocity);
    }

    void LateUpdate()
    {
        //Prevents walking on walls
        if (transform.up.y < .2f)
        {
            groundNormal = Vector3.up;
            transform.up = groundNormal;
            IsGrounded = false;
        }

        UpdateRotation();

        //Debug.DrawRay(transform.position + (transform.up * .8f), ForwardAngleDiffVector * 5f, Color.yellow);
    }

    //When called it initiates a "Platdrop" by starting a coroutine that makes the player change physics layers
    public void Platformdrop(int frames)
    {
        if (platDropCoroutine == null)
        {
            IsGrounded = false;
            this.gameObject.layer = 10;
            platDropCoroutine = IEPlatformdrop(frames);
            StartCoroutine(platDropCoroutine);
        }
    }

    //The coroutine to keep the entity from colliding with platforms
    IEnumerator IEPlatformdrop(int frames)
    {
        while (frames > 0)
        {
            yield return new WaitForEndOfFrame();
            this.gameObject.layer = 10;
            frames--;
        }

        platDropCoroutine = null;
    }

    //When called it makes the entity "Fastfall" by setting the velocity.y to a big negative number
    public void Fastfall()
    {
        rb.velocity = new Vector3(rb.velocity.x, -m_FastfallForce, rb.velocity.z);
    }

    //Returns the angle difference bewtween the current input vector and the slowly lerping ForwardAngleDiffVector (Turnarounds)
    public float GetAngleDiff(Vector3 playerInputMovementVector)
    {
        angleDiff = Vector3.Angle(playerInputMovementVector, ForwardAngleDiffVector);
        return angleDiff;
    }

    //Locks or unlocks the rotation of the entity
    public void LockRotation(bool status)
    {
        if (status)
        {
            lockedRotationEulerAngleY = visibleInitialRotation.eulerAngles.y;
            lockRotation = true;
        }
        else
        {
            lockRotation = false;
        }
    }

    //Locks the rotation to a specific forward vector (No 'tilt')
    public void LockRotation(Vector3 forward)
    {
        Vector3 euler;
        if (forward != Vector3.zero)
        {
            euler = Quaternion.LookRotation(forward).eulerAngles;
            lockedRotationEulerAngleY = euler.y;
        }
        else
        {
            lockedRotationEulerAngleY = visibleInitialRotation.eulerAngles.y;
        }
        lockRotation = true;
    }

    //When called it makes the entity jump
    public void Jump()
    {
        rb.AddForce(m_JumpForce * Vector3.up, ForceMode.Acceleration);
    }

    //When called it makes the entity do a double jump 
    public void AirJump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(m_AirJumpForce * Vector3.up, ForceMode.Impulse);
    }

    //When called it makes the entity do a ground dodge
    public void GroundDodge(Vector3 direction)
    {
        IsDodging = true;
        Vector3 movementVector = Vector3.ProjectOnPlane(direction, groundNormal);
        targetDrag = m_GroundDodgeDrag;

        //rb.velocity = Vector3.zero;
        AddForceWithGroundPrediction(movementVector * m_GroundDodgeForce, ForceMode.Acceleration);
    }

    //When called it makes the entity do an air dodge
    public void AirDodge(Vector3 direction)
    {
        IsDodging = true;
        targetDrag = m_AirDodgeDrag;

        rb.velocity = Vector3.zero;
        AddForceWithAirPrediction(direction * m_GroundDodgeForce, ForceMode.Acceleration);
    }

    //When called it makes the entity wavedash
    public void Wavedash(Vector3 direction)
    {
        Vector3 movementVector = Vector3.ProjectOnPlane(direction, groundNormal);

        //targetDrag = m_WavedashDrag;
        ApplySurfaceStickForce();


        rb.AddForce(movementVector * m_WavedashForce, ForceMode.Impulse);
    }

    //When called it makes the entity do a wall jump
    public void WallJump(Vector3 rawWallJumpVector)
    {
        IsWallJumping = true;
        this.gameObject.layer = 10;
        targetMaxSpeed = m_MaximumWallJumpSpeed;
        rb.velocity = Vector3.zero;
        rawWallJumpVector.y = m_WallJumpVerticalAngle;
        rb.AddForce(rawWallJumpVector * m_WallJumpForce, ForceMode.Impulse);
        //Debug.DrawRay(transform.position, rawWallJumpVector * m_WallJumpForce, Color.red, 5f);
    }

    //When called it makes the entity walk
    public void Walk(Vector3 direction)
    {
        IsMoving = true;
        //Doubles speed cap 'velocity'
        UpdateCurrentMaxSpeed();

        Vector3 movementVector;

        if (IsGrounded)
            movementVector = Vector3.ProjectOnPlane(direction, groundNormal);
        else
            //If it's in the air, there's no need for all the 'slope' and
            //'normal' calculations like above
            movementVector = direction;

        targetMaxSpeed = m_MaximumWalkSpeed;

        rb.AddForce(movementVector * m_WalkSpeedAccel, forceM);
        //Move the player but with prediction to cap speed if it exceeds
        //AddForceWithMaxHorizontalSpeedPrediction(movementVector * m_WalkSpeedAccel, ForceMode.Acceleration);
    }

    //When called it makes the entity run
    public void Run(Vector3 direction)
    {
        IsMoving = true;
        Vector3 movementVector;

        if (IsGrounded)
            movementVector = Vector3.ProjectOnPlane(direction, groundNormal);
        else
            //If it's in the air, there's no need for all the 'slope' and
            //'normal' calculations like above
            movementVector = direction;

        targetMaxSpeed = m_MaximumRunSpeed;

        rb.AddForce(movementVector * m_RunSpeedAccel, forceM);

        //Move the player but with prediction to cap speed if it exceeds
        //AddForceWithMaxHorizontalSpeedPrediction(movementVector * m_RunSpeedAccel, ForceMode.Acceleration);
    }

    //When called it makes the entity crouch
    public void Crouching(Vector3 direction)
    {
        if (rb.velocity.magnitude > m_CrouchMinSpeedToBeAbleToMove && currentMaxSpeed > m_MaximumRunSpeed)
        {
            targetDrag = m_CrouchDrag;
        }

        targetMaxSpeed = rb.velocity.magnitude;
    }

    //When called it makes the entity move while in the air also used for the brief landing lag state
    public void AirMove(Vector3 direction)
    {
        IsMoving = true;
        Vector3 movementVector;

        if (IsGrounded)
            movementVector = Vector3.ProjectOnPlane(direction, groundNormal);
        else
            //If it's in the air, there's no need for all the 'slope' and
            //'normal' calculations like above
            movementVector = direction;

        targetMaxSpeed = m_MaximumAirSpeed;

        rb.AddForce(movementVector * m_AirSpeedAccel, forceM);
        //Move the player but with prediction to cap speed if it exceeds
        //AddForceWithMaxHorizontalSpeedPrediction(movementVector * m_AirSpeedAccel, ForceMode.Acceleration);
    }

    //Updates the rotation of the entiy (what will be actually rendered)
    public void UpdateRotation()
    {
        //Get the horizontal velocity set up
        horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //Determines the rotation speed that will be used
        //If in initial run
        if (stateController.currentState == State.InitialRun)
        {
            angleDiff = 0;
            //Set the current rotation strength really high for instant rotation
            currentRotationStrength = 300f;
        }
        else
        {
            //Set the current rotation strength to the normal rotation strength
            currentRotationStrength = m_RotationStrength;
        }

        if (!lockRotation)
        {
            //If the entity is in the air
            if (!IsGrounded)
            {
                //Reset the entity's rotational 'tilt,' likely from surface normals
                displayModel.eulerAngles = new Vector3(0f, displayModel.eulerAngles.y, 0f);
                //Set this final direction as the target for the Quaternion.Lerp
                visibleTargetRotation = displayModel.rotation;
            }

            //If the entity has some horizontal speed
            if (IsGrounded && horizontalVelocity.magnitude > .2f)
            {
                //Since some usable velocity exists, look towards it
                displayModel.rotation = Quaternion.LookRotation(rb.velocity, displayModel.up);
                //Set this final direction as the target for the Quaternion.Lerp
                visibleTargetRotation = displayModel.rotation;
            }

            //First set the entity's rotation to where the visible rotation was last frame
            displayModel.rotation = visibleInitialRotation;
            //Lerp a little bit more towards the target rotation
            displayModel.rotation = Quaternion.Lerp(visibleInitialRotation, visibleTargetRotation, m_RotationStrength * Time.fixedDeltaTime);
            //Store this rotation to be worked on more next frame
            visibleInitialRotation = displayModel.rotation;

            //Slowly lerp the ForwardAngleDiffVector towards the transform's final visual forward
            ForwardAngleDiffVector = Vector3.Lerp(ForwardAngleDiffVector, new Vector3(displayModel.forward.x, 0f, displayModel.forward.z), m_AngleDiffLerpStrength * Time.fixedDeltaTime);
        }
        else
        {
            //If locked ONLY update the "roll" portion of the entity's rotation
            displayModel.up = groundNormal;
            displayModel.RotateAround(displayModel.position, displayModel.up, lockedRotationEulerAngleY);

            //Lock the ForwardDiffAngleVector to the velocity of the entity
            ForwardAngleDiffVector = new Vector3(rb.velocity.x, 0f, rb.velocity.z).normalized;
            //Lock the visible rotations to prevent strange visual behavior after the release of the lock
            visibleTargetRotation = displayModel.rotation;
            visibleInitialRotation = displayModel.rotation;
        }
    }

    //Applys additional gravity to the entity (when it has no more positive vertical momentum)
    private void ApplyAdditionalGravity()
    {
        rb.AddForce(Vector3.down * m_AdditionalGravityForce, ForceMode.Acceleration);
    }

    //Uses the normal of the surface and an input movement vector to create an adjusted movement vector for 'planar' movement
    /*private Vector3 CalculateSlopeVector(Vector3 inputMovementVector, Vector3 cameraForward, float joystickAngle)
    {
        Vector3 adjustedMovementVector;

        //Get the camera's and entity's forward but 'flattened'
        Vector3 camForwardNoY = new Vector3(cameraForward.x, 0f, cameraForward.z);
        Vector3 entityForwardNoY = new Vector3(transform.forward.x, 0f, transform.forward.z);

        //Weird issue where entity forward can rotate 90 degrees when very tiny
        //which is why theres a small catch to correct it
        if (entityForwardNoY.magnitude < .15f)
            entityForwardNoY = Vector3.forward;

        //Get the angle between the camera's forward and entity's forward
        float angleDifference = Vector3.Angle(camForwardNoY, entityForwardNoY);

        //Orient the character properly before adjusting to camera
        transform.RotateAround(transform.position, transform.up, -angleDifference);

        //Rotate to joystick angle
        transform.RotateAround(transform.position, transform.up, joystickAngle);

        //Assign the movement vector
        adjustedMovementVector = transform.forward;
        adjustedMovementVector *= inputMovementVector.magnitude;

        //Debug
        Debug.DrawRay(transform.position + (transform.up * .3f), adjustedMovementVector * 4f, Color.green);
        //Debug.Log("3 " + stateController.currentState + " OUT: " + adjustedMovementVector + " IN: " + playerInputMovementVector);
        //Debug.DrawRay(transform.position + (transform.up * .2f), camForwardNoY * 5f, Color.red);
        //Debug.DrawRay(transform.position + (transform.up * .1f), entityForwardNoY * 6f, Color.magenta);
        //Debug.DrawRay(transform.position + (transform.up * .01f), -transform.up * 6f, Color.black);
        return adjustedMovementVector;
    }*/

    /*private Vector3 CalculateSlopeVector(Vector3 inputMovementVector)
    {
        Vector3 adjustedMovementVector;

        Vector3 rightVector = Vector3.Cross(Vector3.up, groundNormal.Flat());

        adjustedMovementVector = Vector3.ProjectOnPlane(inputMovementVector,groundNormal);

        //Debug
        Debug.DrawRay(transform.position + (transform.up * .3f), adjustedMovementVector * 4f, Color.green);

        return adjustedMovementVector;
    }*/

    //Updates the current max speed by either 'snapping it up' or slowly decreasing it down to the target max speed
    private void UpdateCurrentMaxSpeed()
    {
        //If the current max speed is greater than the target max speed, slowly move towards it
        if (currentMaxSpeed > targetMaxSpeed)
        {
            currentMaxSpeed = currentMaxSpeed - (m_MaxSpeedShrinkSpeed * Time.fixedDeltaTime);
        }
        //If the current max speed is less than the target max speed, snap it up to the target max speed
        if (currentMaxSpeed < targetMaxSpeed)
        {
            currentMaxSpeed = targetMaxSpeed;
        }
    }
    
    //Updates the current drag by either slowly increasing it up or 'snapping it down' to the target drag
    private void UpdateCurrentDrag()
    {
        //If the current max speed is greater than the target max speed, slowly move towards it
        if (rb.drag < targetDrag)
        {
            rb.drag += (m_DragChangeStrength * Time.fixedDeltaTime);
        }
        //If the current max speed is less than the target max speed, snap it to the target max speed
        if (rb.drag > targetDrag)
        {
            rb.drag = targetDrag;
        }
    }

    //Updates the target drag
    private void UpdateTargetDrag()
    {
        if (IsGrounded)
            targetDrag = m_DefaultDrag;
        else
            targetDrag = m_AirDrag;

        if (IsGrounded && !IsMoving && rb.velocity.y <= -0.5f && stateController.currentState != State.Wavedash && stateController.currentState != State.AirDodge)
            targetDrag = m_CrouchDrag;

        if (IsWallJumping)
            targetDrag = m_AirDrag;

        if (IsDodging && IsGrounded && stateController.currentState != State.Wavedash && stateController.currentState != State.AirDodge)
            targetDrag = m_GroundDodgeDrag;
        else if (IsDodging && !IsGrounded)
            targetDrag = m_AirDodgeDrag;
    }

    //Applys a strong downards force to keep the entity stuck to the platform they are standing on (important when moving across ramps)
    private void ApplySurfaceStickForce()
    {
        rb.AddForce(-transform.up * m_StickForce);
    }

    //Updates the ground status by reporting if the entity is grounded and what the surface normal is
    private void CheckGroundStatus()
    {
        //If previously grounded AND the entity isn't starting to jump/jumping
        if (IsGrounded && stateController.currentState != State.Jumpsquat && stateController.currentState != State.AscentJump1)
        {
            //Draw a ray of the check distance
            //Debug.DrawRay(transform.position + (transform.up * 0.3f), -transform.up * m_GroundNormalCheckDistance, Color.cyan);

            //Check for the ground normal
            if (Physics.Raycast(transform.position + (transform.up * 0.3f), -transform.up, out hitInfo, m_GroundNormalCheckDistance, platenvironLayerMask))
            {                
                //Update the ground normal
                groundNormal = hitInfo.normal;

                //Set entity rotation relative to normal of platform, actual display
                //rotation is calculated in LateUpdate()
                transform.up = hitInfo.normal;

                //Draw a ray of the normal at the hit point
                //Debug.DrawRay(hitInfo.point, hitInfo.normal * m_GroundNormalCheckDistance, Color.green);
            }
            //No ground normal found
            else
            {
                groundNormal = Vector3.up;
                transform.up = groundNormal;
            }
        }
        //No ground normal found
        else
        {
            groundNormal = Vector3.up;
            transform.up = groundNormal;
        }

        //If previously grounded OR (in the air AND moving downwards)
        if (IsGrounded || (!IsGrounded && rb.velocity.y <= 0.1))
        {
            //Draw a ray of the check distance
            //Debug.DrawRay(transform.position + (transform.up * 0.3f), -transform.up * m_GroundCheckDistance, Color.cyan);

            //Check if entity is on the ground
            if (Physics.Raycast(transform.position + (transform.up * 0.3f), -transform.up, out hitInfo, m_GroundCheckDistance, platenvironLayerMask) && stateController.currentState != State.AscentJump1)
            {
                //Keep the entity stuck to the ground
                ApplySurfaceStickForce();
                //Update IsGrounded status
                IsGrounded = true;
            }
            //In the air
            else
            {
                //Update IsGrounded status
                IsGrounded = false;
            }
        }
        //In the air
        else
        {
            //Update IsGrounded status
            IsGrounded = false;
        }
    }

    //When AddForce is called, it just queues. It actually updates before LateUpdate()
    private void AddForceWithMaxHorizontalSpeedPrediction(Vector3 AddForceVector, ForceMode forceMode)
    {
        //Predict if the force will exceed the given maximum velocity
        predictorRB.velocity = rb.velocity;
        predictorRB.drag = rb.drag;

        //Apply the desired movement force to the dummy object first
        predictorRB.gameObject.transform.position = Vector3.up * -1000f;
        predictorRB.AddForce(AddForceVector, forceMode);

        //Finished in LateUpdate() so the velocity will update and we can react accordingly
        cappingSpeed = true;
    }

    //When AddForce is called, it just queues. It actually updates before LateUpdate()
    private void AddForceWithGroundPrediction(Vector3 AddForceVector, ForceMode forceMode)
    {
        predictorRB.position = transform.position;
        //Predict if the force will exceed the given maximum velocity
        predictorRB.velocity = Vector3.zero;
        predictorRB.drag = rb.drag;

        //Apply the desired movement force to the dummy object first
        //predictorRB.gameObject.transform.position = Vector3.up * -1000f;
        predictorRB.AddForce(AddForceVector, forceMode);

        predictingGround = true;
    }

    //When AddForce is called, it just queues. It actually updates before LateUpdate()
    private void AddForceWithAirPrediction(Vector3 AddForceVector, ForceMode forceMode)
    {
        predictorRB.position = transform.position;
        //Predict if the force will exceed the given maximum velocity
        predictorRB.velocity = Vector3.zero;
        predictorRB.drag = rb.drag;

        //Apply the desired movement force to the dummy object first
        //predictorRB.gameObject.transform.position = Vector3.up * -1000f;
        predictorRB.AddForce(AddForceVector, forceMode);

        if (AddForceVector.y >= 0)
            predictorRB.gameObject.layer = 15;

        predictingAir = true;
    }

    public void LoadPhysicsData()
    {
        m_GroundCheckDistance           = stateController.data.e_GroundCheckDistance;
        m_GroundNormalCheckDistance     = stateController.data.e_GroundNormalCheckDistance;    
        m_WavedashGroundCheckDistance   = stateController.data.e_WavedashGroundCheckDistance;
        m_AirSpeedAccel                 = stateController.data.e_AirSpeedAccel;                
        m_WalkSpeedAccel                = stateController.data.e_WalkSpeedAccel;              
        m_RunSpeedAccel                 = stateController.data.e_RunSpeedAccel;               
        m_MaximumWalkSpeed              = stateController.data.e_MaximumWalkSpeed;
        m_MaximumRunSpeed               = stateController.data.e_MaximumRunSpeed; ;               
        m_MaximumAirSpeed               = stateController.data.e_MaximumAirSpeed;               
        m_MaximumWallJumpSpeed          = stateController.data.e_MaximumWallJumpSpeed;
        m_RotationStrength              = stateController.data.e_RotationStrength;              
        m_DragChangeStrength            = stateController.data.e_DragChangeStrength;
        m_MaxSpeedShrinkSpeed           = stateController.data.e_MaxSpeedShrinkSpeed;            
        m_AngleDiffLerpStrength         = stateController.data.e_AngleDiffLerpStrength;          
        m_DefaultDrag                   = stateController.data.e_DefaultDrag;                  
        m_AirDrag                       = stateController.data.e_AirDrag;
        m_CrouchDrag                    = stateController.data.e_CrouchDrag;                    
        m_AirDodgeDrag                  = stateController.data.e_AirDodgeDrag;
        m_GroundDodgeDrag               = stateController.data.e_GroundDodgeDrag;
        m_StickForce                    = stateController.data.e_StickForce;
        m_JumpForce                     = stateController.data.e_JumpForce;
        m_AirJumpForce                  = stateController.data.e_AirJumpForce;
        m_WallJumpForce                 = stateController.data.e_WallJumpForce;
        m_FastfallForce                 = stateController.data.e_FastfallForce;
        m_GroundDodgeForce              = stateController.data.e_GroundDodgeForce;
        m_AirDodgeForce                 = stateController.data.e_AirDodgeForce;
        m_WavedashForce                 = stateController.data.e_WavedashForce;
        m_AdditionalGravityForce        = stateController.data.e_AdditionalGravityForce;
        m_WallJumpVerticalAngle         = stateController.data.e_WallJumpVerticalAngle;
        m_CrouchMinSpeedToBeAbleToMove  = stateController.data.e_CrouchMinSpeedToBeAbleToMove;
    }
}
