using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

//Handles all input from the player. A "passive" classs
//that primarily answers to StateController.
//[RequireComponent(typeof(StateController))]
public class PlayerInput : MonoBehaviour
{
    //Public parameters to control entity, m_ is for modifiable
    public bool useInput = true;        //Is using controller||debugging
    public float deadZoneRadius = .2f;  //Deadzone radius
    public Vector3 inputMovementVector; //Initial input from joystick
    public Vector3 inputCameraStickVector;
    public int playerNumber = 0;        //Player number

    //Input vars
    private Vector3 lastjoystickVector;         //Last input from joystick
    private Vector3 movementVector;             //Movement vector (world space) from input vector
    private Vector3 cameraStickVector;
    private float joystickAngle;                //Angle of joystick from Vector2.forward to X,Y
    private float cameraStickAngle;
    private float joystickVelocity;             //Speed that joystick is moving
    public bool jumpHeld = false;               //Jump button held
    public bool jumpDown = false;               //Jump button down
    public bool fastFallHeld = false;
    public bool fastFallDown = false;
    public bool angleDown = false;              //AngleDown & Crouch Button
    public bool angleUp = false;
    public bool dodge = false;
    public bool normalAttackHeld = false;
    public bool normalAttackDown = false;
    public bool specialAttackHeld = false;
    public bool specialAttackDown = false;
    public bool chargeAttackHeld = false;
    public bool chargeAttackDown = false;
    public bool pause = false;
    public bool inventory = false;

    //References
    private GameObject playerCameraObject;
    private GameObject combatCameraObject;
    private GameObject activeCameraObject;
    private Camera activeCamera;
    private Player p;

    /*void OnGUI()
    {
        GUI.contentColor = Color.blue;
        GUI.Label(new Rect(10, 10, 500, 500), "Joystick X: " + inputMovementVector.x);
        GUI.Label(new Rect(10, 60, 500, 500), "Joystick Y: " + inputMovementVector.z);
        GUI.Label(new Rect(10, 110, 500, 500), "Joystick Magnitude: " + inputMovementVector.magnitude);
    }*/

    void Start()
    {
        p = ReInput.players.GetPlayer(playerNumber);

        string playerCameraTag = "Player" + (playerNumber+1) + "Camera";
        playerCameraObject = GameObject.FindWithTag(playerCameraTag);

        string combatCameraTag = "CombatCamera";
        combatCameraObject = GameObject.FindWithTag(combatCameraTag);

        UpdateActiveCamera();

        lastjoystickVector = Vector3.zero;
        inputMovementVector = Vector3.zero;
        inputCameraStickVector = Vector3.zero;
        cameraStickVector = Vector3.zero;
    }

    //Update all inputs
    void FixedUpdate()
    {
        //Update inputs from controller if player is controlling (always true unless debugging)
        if (useInput)
        {
            inputMovementVector.x = p.GetAxis("Horizontal");
            inputMovementVector.z = p.GetAxis("Vertical");
            inputCameraStickVector.x = p.GetAxis("CameraHorizontal");
            inputCameraStickVector.z = p.GetAxis("CameraVertical");
        }

        UpdateJoystickAngles();

        //Adjust input vector to world space if necessary
        if ((inputMovementVector.x != 0f || inputMovementVector.z != 0f) && inputMovementVector.magnitude > deadZoneRadius)
            LeftStickInputToWorld();
        else
            movementVector = Vector3.zero;

        Debug.DrawRay(transform.position + Vector3.up * 1f, movementVector * 5f, Color.red);
        Debug.DrawRay(transform.position, activeCameraObject.transform.forward * 5f, Color.blue);

        //Adjust input vector to world space if necessary
        if ((inputCameraStickVector.x != 0f || inputCameraStickVector.z != 0f) && inputCameraStickVector.magnitude > deadZoneRadius)
            RightStickInputToWorld();
        else
            cameraStickVector = Vector3.zero;
    }

    void LateUpdate()
    {
        lastjoystickVector = inputMovementVector;
    }

    public int GetPlayerNumber()
    {
        return playerNumber;
    }

    public bool GetJumpButtonHeld()
    {
        if (useInput)
            jumpHeld = p.GetButton("Jump");
        
        return jumpHeld;
    }

    public bool GetJumpButtonDown()
    {
        if (useInput)
            jumpDown = p.GetButtonDown("Jump");

        return jumpDown;
    }

    public bool GetNormalAttackButtonHeld()
    {
        if (useInput)
            normalAttackHeld = p.GetButton("NormalAttack");

        return normalAttackHeld;
    }

    public bool GetNormalAttackButtonDown()
    {
        if (useInput)
            normalAttackDown = p.GetButtonDown("NormalAttack");

        return normalAttackDown;
    }

    public bool GetSpecialAttackButtonHeld()
    {
        if (useInput)
            specialAttackHeld = p.GetButton("SpecialAttack");

        return specialAttackHeld;
    }

    public bool GetSpecialAttackButtonDown()
    {
        if (useInput)
            specialAttackDown = p.GetButtonDown("SpecialAttack");

        return specialAttackDown;
    }

    public bool GetChargeAttackButtonHeld()
    {
        if (useInput)
            chargeAttackHeld = p.GetButton("ChargeAttack");

        return chargeAttackHeld;
    }

    public bool GetChargeAttackButtonDown()
    {
        if (useInput)
            chargeAttackDown = p.GetButtonDown("ChargeAttack");

        return chargeAttackDown;
    }

    public bool GetFastFallButtonHeld()
    {
        if (useInput)
            fastFallHeld = p.GetButton("Fastfall");

        return fastFallHeld;
    }

    public bool GetFastFallButtonDown()
    {
        if (useInput)
            fastFallDown = p.GetButtonDown("Fastfall");

        return fastFallDown;
    }

    public bool GetAngleDownButton()
    {
        if (useInput)
            angleDown = p.GetButton("AngleDown");

        return angleDown;
    }

    public bool GetAngleUpButton()
    {
        if (useInput)
            angleUp = p.GetButton("AngleUp");

        return angleUp;
    }

    public bool GetDodgeButtonDown()
    {
        if (useInput)
            dodge = p.GetButtonDown("Dodge");

        return dodge;
    }

    public bool GetPauseButtonDown()
    {
        if (useInput)
            pause = p.GetButtonDown("Pause");

        return pause;
    }

    public bool GetInventoryButtonDown()
    {
        if (useInput)
            inventory = p.GetButtonDown("Inventory");

        return inventory;
    }

    public float GetJoystickVelocity()
    {
        joystickVelocity = inputMovementVector.magnitude - lastjoystickVector.magnitude;
        if (joystickVelocity < 0f)
            joystickVelocity = 0f;
        return joystickVelocity;
    }

    public Vector3 GetMovementVector()
    {
        //Return the movement vector calculated earlier at the beginning of the frame
        return movementVector;
    }

    public Vector3 GetCameraStickVector()
    {
        //Return the adjusted camera vector calculated earlier at the beginning of the frame
        return cameraStickVector;
    }

    public Vector3 GetCameraForward()
    {
        //Return the active camera's forward
        return activeCameraObject.transform.forward;
    }

    public float GetJoystickAngle()
    {
        return joystickAngle;
    }

    void UpdateActiveCamera()
    {
        //If player personal camera is active 
        if (playerCameraObject.GetComponent<Camera>().isActiveAndEnabled)
            activeCameraObject = playerCameraObject;
        //Combat camera is active and personal camera is disabled
        else
            activeCameraObject = combatCameraObject;
        //Set activeCamera to the activeCameraObject's camera component
        activeCamera = activeCameraObject.GetComponent<Camera>();
    }

    void UpdateJoystickAngles()
    {
        //Get the initial angle the joystick is making normally
        joystickAngle = Vector3.Angle(Vector3.forward, inputMovementVector);
        //Perform a cross to see if it resides in the left or right hemisphere
        Vector3 cross = Vector3.Cross(Vector3.forward, inputMovementVector);
        //If in the left hemisphere convert from counterclockwise to clockwise
        if (cross.y < 0)
            joystickAngle = 180f + (180f - joystickAngle);


        //Get the initial angle the joystick is making normally
        cameraStickAngle = Vector3.Angle(Vector3.forward, inputCameraStickVector);
        //Perform a cross to see if it resides in the left or right hemisphere
        cross = Vector3.Cross(Vector3.forward, inputCameraStickVector);
        //If in the left hemisphere convert from counterclockwise to clockwise
        if (cross.y < 0)
            cameraStickAngle = 180f + (180f - cameraStickAngle);
    }

    void LeftStickInputToWorld()
    {
        //Make a final angle that will be used for the final movementVector (relative to camera)
        float finalAngle = activeCameraObject.transform.eulerAngles.y + joystickAngle;
        //Construct a final movementVector (world space) based off of the final angle)
        movementVector = new Vector3(Mathf.Sin(finalAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(finalAngle * Mathf.Deg2Rad));
        //Make sure magnitude is correct
        movementVector *= Mathf.Clamp01(inputMovementVector.magnitude);
    }

    void RightStickInputToWorld()
    {
        //Make a final angle that will be used for the final movementVector (relative to camera)
        float finalAngle = activeCameraObject.transform.eulerAngles.y + cameraStickAngle;
        //Construct a final movementVector (world space) based off of the final angle)
        cameraStickVector = new Vector3(Mathf.Sin(finalAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(finalAngle * Mathf.Deg2Rad));
        //Make sure magnitude is correct
        cameraStickVector *= Mathf.Clamp01(inputCameraStickVector.magnitude);
    }
}
