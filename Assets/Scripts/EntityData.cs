using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Data", menuName = "Entity/Data")]
public class EntityData : ScriptableObject
{
    //StateController Frame Data
    public int m_LandLagFrames = 4;                 //Number of frames the entity will be in the landing state
    public int m_TurnaroundFrames = 11;             //Number of frames the entity will be in the turnaround state
    public int m_InitialRunFrames = 2;              //Number of frames the entity will be in the initial run state
    public float m_TurnaroundAngleThreshold = 140;  //The minimum angle to initiate the turnaround state
    public int m_MinInactiveFramesIoIdle = 12;      //Number of frames until an initial run state will be used from idle again
    public int m_JumpSquatFrames = 4;               //Number of frames the entity will be in the jumpsquat state
    public int m_TurnaroundDeaccelFrames = 6;       //Number of frames the entity will deaccelerate at the start of the turnaround state
    public float m_JoystickVelocityToRun = 0.3f;    //Minimum velocity of the joystick required to initiate a run (either initial run state or run state)
    public int m_MaxJumpHeightExtendFrames = 8;     //Number of frames the entity will allow the jump button to be held to extend the initial jump height
    public int m_NumberOfJumpsInAir = 1;
    public int m_NumberOfWallJumps = 1;
    public int m_NumberOfAirDodges = 1;
    public float m_DistanceToWallForWallJump = 1f;
    public int m_WallJumpFrames = 10;
    public int m_FramesToJumpOfWallJump = 3;
    public int m_PlatdropFrames = 20;
    public float m_MinVelocityToFastfall = 7f;
    public int m_TotalDodgeDuration = 40;
    public int m_DodgeStartupFrames = 8;
    public int m_DodgeEndLagFrames = 8;
    public int m_WavedashFrames = 8;



    //EntityPhysics data
    public float e_GroundCheckDistance = 0.65f;         //Distance to check if grounded by default/in air
    public float e_GroundNormalCheckDistance = 2.5f;    //Distance to check if grounded if was previously grounded (used primarily for keeping player attached to slopes)
    public float e_WavedashGroundCheckDistance = 1.6f;

    public float e_AirSpeedAccel = 100f;                //Accel used when moving in air
    public float e_WalkSpeedAccel = 5000f;              //Accel used when walking
    public float e_RunSpeedAccel = 5000f;               //Accel used when running

    public float e_MaximumWalkSpeed = 5f;               //Max speed for walking
    public float e_MaximumRunSpeed = 12f;               //Max speed for running
    public float e_MaximumAirSpeed = 10f;               //Max speed for when in air
    public float e_MaximumWallJumpSpeed = 18f;

    public float e_RotationStrength = 10f;              //The default rotation strength for all the visible rotation calculations
    public float e_DragChangeStrength = 5f;
    public float e_MaxSpeedShrinkSpeed = 5f;            //The strength or rate at which the current max speed shrinks to match the target max speed
    public float e_AngleDiffLerpStrength = 3f;          //The strength at which the ForwardAngleDiffVector lerps towards the current visible entity forward

    public float e_DefaultDrag = 5.5f;                  //The default rigidbody drag
    public float e_AirDrag = 3.5f;
    public float e_CrouchDrag = 2f;                     //The default rigidbody drag when crouched (very small for increased sliding)
    public float e_AirDodgeDrag = 2f;
    public float e_GroundDodgeDrag = 2f;

    public float e_StickForce = 500f;
    public float e_JumpForce = 470f;
    public float e_AirJumpForce = 26f;
    public float e_WallJumpForce = 19f;
    public float e_FastfallForce = 35f;
    public float e_GroundDodgeForce = 780f;
    public float e_AirDodgeForce = 690f;
    public float e_WavedashForce = 23f;
    public float e_AdditionalGravityForce = 25f;

    public float e_WallJumpVerticalAngle = 2f;
    public float e_CrouchMinSpeedToBeAbleToMove = 8f;   //The minimum speed required in order to get the reduced drag benefit from crouching
}
