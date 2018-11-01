using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

//Handles all the state of the entity. Prompts PlayerInput or an entity controller
//for inputs based on the state and passes them to EntityPhysics.
public enum State
{
    Idle,
    Walk,
    InitialRun,
    Run,
    Crouching,
    Turnaround,
    Dodge,
    Wavedash,
    Attack,
    Jumpsquat,
    Landing,
    LandingAttackLag,
    LandingHitstun,
    InAir,
    AscentJump1,
    WallJump,
    Hitstun,
    AirAttack,
    AirDodge
}
//[RequireComponent(typeof(EntityPhysics))]
//[RequireComponent(typeof(CombatModule))]
public class StateController : MonoBehaviour
{
    //State
    public State currentState = State.Idle;

    //Rewired player number
    public int playerNumber = 0;

    //References
    private EntityPhysics physics;
    private CombatModule combatModule;
    public Animator animator;
    private Player p;
    public EntityData data;
    public ParticleSystem landDustParticles;
    public ParticleSystem runDustParticles;
    public ParticleSystem turnaroundDustParticles;
    public ParticleSystem jumpDustParticles;
    public ParticleSystem wallJumpDustParticles;
    [HideInInspector]
    public PlayerInput playerInput;
    
    //Public parameters to control class, m_ is for modifiable
    private int m_LandLagFrames;                     //Number of frames the entity will be in the landing state
    private int m_TurnaroundFrames;                  //Number of frames the entity will be in the turnaround state
    private int m_InitialRunFrames;                  //Number of frames the entity will be in the initial run state
    private float m_TurnaroundAngleThreshold;        //The minimum angle to initiate the turnaround state
    private int m_MinInactiveFramesIoIdle;           //Number of frames until an initial run state will be used from idle again
    private int m_JumpSquatFrames;                   //Number of frames the entity will be in the jumpsquat state
    private int m_TurnaroundDeaccelFrames;           //Number of frames the entity will deaccelerate at the start of the turnaround state
    private float m_JoystickVelocityToRun;           //Minimum velocity of the joystick required to initiate a run (either initial run state or run state)
    private int m_MaxJumpHeightExtendFrames;         //Number of frames the entity will allow the jump button to be held to extend the initial jump height
    private int m_NumberOfJumpsInAir;                
    private int m_NumberOfWallJumps;
    private int m_NumberOfAirDodges;
    private float m_DistanceToWallForWallJump;
    private int m_WallJumpFrames;
    private int m_FramesToJumpOfWallJump;
    private int m_PlatdropFrames;
    private float m_MinVelocityToFastfall;
    private int m_TotalDodgeDuration;
    private int m_DodgeStartupFrames;
    private int m_DodgeEndLagFrames;
    private int m_WavedashFrames;

    //State change related parameter variables
    private bool IsGrounded;                        //Copy of IsGrounded from EntityPhysics
    private int currentLagFrames;                   //A general counter variable that counts down frames
    private int currentNumberOfJumps;
    private int currentNumberOfWallJumps;
    private int currentNumberOfAirDodges;
    private int switchBreakerCount;                 //Prevents an infinite loop if there are too many state changes in one update
    private int wallJumpMask;
    private RaycastHit hitInfo = new RaycastHit();
    private bool wallPresent = false;
    private Vector3 temporaryVector;
    private float temporaryJoystickAngle;
    public bool IsVulnerable = true;
    private AttackType currentAttack;
    private bool isInCombat = false;

    //Local PlayerInput Variables
    private Vector3 playerInputMovementVector;      //Copy of the input movement vector from PlayerInput
    private Vector3 cameraForward;                  //Copy of the forward of the active camera from PlayerInput
    private float joystickAngle;                    //Copy of the angle the joystick is currently creating from PlayerInput
    private float joystickVelocity;                 //Copy of the velocity of the joystick from PlayerInput
    private bool jumpButtonHeld;                    //Copy of the status of the jump button held from PlayerInput
    private bool jumpButtonDown;                    //Copy of the status of the jump button down from PlayerInput
    private bool angleDown;                         //Copy of the status of the angle down button from PlayerInput
    private bool angleUp;
    private float angleDiff;                        //Copy of the entity's change in direction from EntityPhysics
    private bool fastFallDown;
    private bool fastFallHeld;
    private bool dodge;
    private bool normalAttackHeld = false;
    private bool normalAttackDown = false;
    private bool specialAttackHeld = false;
    private bool specialAttackDown = false;
    private bool chargeAttackHeld = false;
    private bool chargeAttackDown = false;
    private bool pause = false;
    private bool inventory = false;
    private Vector3 cameraStickVector;

    //Animator
    readonly int a_IsWalking = Animator.StringToHash("IsWalking");
    readonly int a_IsRunning = Animator.StringToHash("IsRunning");
    readonly int a_IsCrouching = Animator.StringToHash("IsCrouching");
    readonly int a_IsLanding = Animator.StringToHash("IsLanding");
    readonly int a_IsTurningAround = Animator.StringToHash("IsTurningAround");
    readonly int a_AngleDifference = Animator.StringToHash("AngleDifference");
    readonly int a_IsInJumpSquat = Animator.StringToHash("IsInJumpSquat");
    readonly int a_IsAscendingJumpOne = Animator.StringToHash("IsAscendingJump1");
    readonly int a_IsWallJumping = Animator.StringToHash("IsWallJumping");
    readonly int a_IsGrounded = Animator.StringToHash("IsGrounded");
    readonly int a_IsDodging = Animator.StringToHash("IsDodging");
    readonly int a_IsWavedashing = Animator.StringToHash("IsWavedashing");

    //Dictionaries
    public static Dictionary<State, State[]> transitionDictionary = new Dictionary<State, State[]>();
    public static Dictionary<State, State[]> stateCancelDictionary = new Dictionary<State, State[]>();

    void Start()
    {
        physics = GetComponent<EntityPhysics>();
        combatModule = GetComponent<CombatModule>();
        //animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        runDustParticles.Stop();
        landDustParticles.Stop();
        jumpDustParticles.Stop();
        turnaroundDustParticles.Stop();
        wallJumpDustParticles.Stop();
        currentNumberOfJumps = m_NumberOfJumpsInAir;
        currentNumberOfWallJumps = m_NumberOfWallJumps;
        currentNumberOfAirDodges = m_NumberOfAirDodges;
        wallJumpMask = LayerMask.GetMask("Environment","CombatEnvironment");

        p = ReInput.players.GetPlayer(playerNumber);
        //InitializeInputStructs();

        SetupDictionaries();

        LoadFrameData();
    }

    void FixedUpdate()
    {
        //isMoving = false;
        switchBreakerCount = 0;

        UpdateInputs();

        if (inventory)
            combatModule.LoadAttacks();

        //Detect if there are any transitions (once the current state has ended)
        if (currentLagFrames == 0)
            currentState = Transition(true);
        //Detect if there are any cancel transitions
        currentState = Transition(false);

        switch (currentState)
        {

            case State.Idle:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (physics.rb.velocity.y >= -.5 && physics.rb.velocity.y <= .5)
                    physics.LockRotation(true);
                else
                    physics.LockRotation(false);

                if (IsGrounded && fastFallDown)
                    physics.Platformdrop(m_PlatdropFrames);

                break;

            case State.Walk:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                //isMoving = true;

                if (IsGrounded && fastFallDown)
                    physics.Platformdrop(m_PlatdropFrames);

                physics.Walk(playerInputMovementVector);
                physics.LockRotation(false);

                break;

            case State.InitialRun:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                //isMoving = true;

                if (angleDiff > m_TurnaroundAngleThreshold)
                {
                    currentLagFrames = m_InitialRunFrames;
                }
                if (currentLagFrames == m_InitialRunFrames)
                {
                    runDustParticles.Play();
                }
                if (playerInputMovementVector != Vector3.zero)
                {
                    physics.Run(playerInputMovementVector);
                }

                if (IsGrounded && fastFallDown)
                    physics.Platformdrop(m_PlatdropFrames);

                currentLagFrames--;
                physics.LockRotation(false);

                break;

            case State.Run:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                //isMoving = true;

                if (playerInputMovementVector == Vector3.zero && IsGrounded)
                {
                    currentLagFrames--;
                }
                else
                {
                    currentLagFrames = m_MinInactiveFramesIoIdle;
                    physics.Run(playerInputMovementVector);
                }

                if (IsGrounded && fastFallDown)
                    physics.Platformdrop(m_PlatdropFrames);

                physics.LockRotation(false);

                break;

            case State.Crouching:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (IsGrounded && fastFallDown)
                    physics.Platformdrop(m_PlatdropFrames);

                //physics.Crouching(playerInputMovementVector, cameraForward, joystickAngle);
                physics.LockRotation(true);

                break;

            case State.Turnaround:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (playerInputMovementVector != Vector3.zero && currentLagFrames <= m_TurnaroundFrames - m_TurnaroundDeaccelFrames)
                {
                    physics.Run(playerInputMovementVector);
                }
                if (currentLagFrames == m_TurnaroundFrames)
                {
                    turnaroundDustParticles.Play();
                    physics.LockRotation(playerInputMovementVector);
                }

                if (IsGrounded && fastFallDown)
                    physics.Platformdrop(m_PlatdropFrames);

                currentLagFrames--;

                break;

            case State.Jumpsquat:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (currentLagFrames == m_JumpSquatFrames)
                {
                    physics.LockRotation(true);
                }
                if (playerInputMovementVector != Vector3.zero)
                {
                    physics.AirMove(playerInputMovementVector);
                }

                currentLagFrames--;

                break;

            case State.Dodge:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (currentLagFrames == m_TotalDodgeDuration - m_DodgeStartupFrames)
                {
                    IsVulnerable = false;
                    temporaryVector = playerInputMovementVector.normalized;
                    temporaryJoystickAngle = joystickAngle;
                    physics.LockRotation(playerInputMovementVector);
                }
                if (currentLagFrames <= m_TotalDodgeDuration - m_DodgeStartupFrames && currentLagFrames > m_DodgeEndLagFrames)
                {
                    physics.GroundDodge(temporaryVector);
                }
                if (currentLagFrames == m_DodgeEndLagFrames)
                {
                    IsVulnerable = true;
                }

                currentLagFrames--;

                break;

            case State.Wavedash:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (currentLagFrames == m_WavedashFrames)
                {
                    physics.Wavedash(temporaryVector);
                    turnaroundDustParticles.Play();
                }

                if (IsGrounded && fastFallDown)
                    physics.Platformdrop(m_PlatdropFrames);

                currentLagFrames--;

                break;

            case State.Attack:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (currentLagFrames == -1)
                {
                    //Detect what type of attack is being used
                    CheckAttack();
                    combatModule.InitializeAttack(currentAttack, out currentLagFrames);
                    Debug.Log(currentAttack);
                    if (playerInputMovementVector != Vector3.zero)
                        physics.LockRotation(playerInputMovementVector);
                    //Still need start lag
                    //Still need both end lags
                }
                else
                {
                    combatModule.Attack(currentAttack, currentLagFrames);
                }

                currentLagFrames--;

                break;

            case State.Landing:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (currentLagFrames == m_LandLagFrames)
                {
                    physics.LockRotation(true);
                    currentNumberOfJumps = m_NumberOfJumpsInAir;
                    currentNumberOfWallJumps = m_NumberOfWallJumps;
                    currentNumberOfAirDodges = m_NumberOfAirDodges;
                }

                //Wait for transform to update to ground normal
                if (currentLagFrames == m_LandLagFrames - 2)
                    landDustParticles.Play();

                if (playerInputMovementVector != Vector3.zero)
                {
                    physics.AirMove(playerInputMovementVector);
                }

                currentLagFrames--;

                break;

            case State.LandingAttackLag:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                break;

            case State.LandingHitstun:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                break;

            case State.AscentJump1:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (currentLagFrames == m_MaxJumpHeightExtendFrames)
                {
                    physics.LockRotation(true);
                    physics.Jump();
                    physics.Jump();
                    physics.Jump();
                }
                else if (currentLagFrames != 0 && currentLagFrames%2 == 0)
                {
                    physics.Jump();
                }

                if (!jumpButtonHeld)
                    currentLagFrames = 1;

                if (playerInputMovementVector != Vector3.zero)
                {
                    physics.AirMove(playerInputMovementVector);
                }

                currentLagFrames--;

                break;

            case State.InAir:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (jumpButtonDown && currentNumberOfJumps > 0)
                {
                    jumpDustParticles.Play();
                    physics.AirJump();
                    currentNumberOfJumps--;
                }

                if (playerInputMovementVector != Vector3.zero)
                {
                    physics.AirMove(playerInputMovementVector);
                }

                if (!IsGrounded && fastFallDown && physics.rb.velocity.y < m_MinVelocityToFastfall)
                    physics.Fastfall();

                break;

            case State.WallJump:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (currentLagFrames == (m_WallJumpFrames - m_FramesToJumpOfWallJump))
                {
                    physics.UpdateRotation();

                    wallJumpDustParticles.Play();

                    physics.WallJump(temporaryVector);
                }

                currentLagFrames--;

                break;

            case State.AirAttack:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }
                break;

            case State.AirDodge:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }

                if (currentLagFrames >= m_TotalDodgeDuration - m_DodgeStartupFrames)
                {
                    physics.AirDodge(Vector3.zero);
                    IsVulnerable = false;
                    temporaryVector = playerInputMovementVector.normalized;
                    temporaryJoystickAngle = joystickAngle;
                    if (playerInputMovementVector != Vector3.zero)
                        physics.LockRotation(playerInputMovementVector);
                    if (angleUp)
                        temporaryVector.y = 1f;
                    if (angleDown)
                        temporaryVector.y = -1f;
                    temporaryVector.Normalize();
                    currentNumberOfAirDodges--;
                }
                if (currentLagFrames <= m_TotalDodgeDuration - m_DodgeStartupFrames && currentLagFrames > m_DodgeEndLagFrames)
                {
                    physics.AirDodge(temporaryVector);
                }
                if (currentLagFrames == m_DodgeEndLagFrames)
                {
                    IsVulnerable = true;
                }

                currentLagFrames--;

                break;

            case State.Hitstun:
                switchBreakerCount++;
                if (switchBreakerCount > 10)
                { Debug.Log("Stuck! Broke at: " + currentState); break; }
                break;

            default:
                Debug.Log("U H .  I don't know how we reached the default state.");
                break;
        }

        animator.SetBool(a_IsWalking, (currentState == State.Walk) ? true : false);
        animator.SetBool(a_IsRunning, (currentState == State.InitialRun || currentState == State.Run || currentState == State.Turnaround) ? true : false);
        animator.SetBool(a_IsCrouching, (currentState == State.Crouching) ? true : false);
        animator.SetBool(a_IsLanding, (currentState == State.Landing) ? true : false);
        animator.SetBool(a_IsGrounded, (IsGrounded) ? true : false);
        animator.SetFloat(a_AngleDifference, angleDiff);
        animator.SetBool(a_IsInJumpSquat, (currentState == State.Jumpsquat) ? true : false);
        animator.SetBool(a_IsAscendingJumpOne, (currentState == State.AscentJump1) ? true : false);
        animator.SetBool(a_IsWallJumping, (currentState == State.WallJump) ? true : false);
        animator.SetBool(a_IsDodging, (currentState == State.Dodge) ? true : false);
        animator.SetBool(a_IsWavedashing, (currentState == State.Wavedash) ? true : false);

        //Debug.Log(currentState + " " + currentLagFrames);
    }

    public void GoToWavedash()
    {
        currentState = State.Wavedash;
        physics.LockRotation(true);
        currentNumberOfJumps = m_NumberOfJumpsInAir;
        currentNumberOfWallJumps = m_NumberOfWallJumps;
        currentNumberOfAirDodges = m_NumberOfAirDodges;
        temporaryVector.y = 0f;
        temporaryVector *= (float)currentLagFrames / (float)m_TotalDodgeDuration;
        currentLagFrames = m_WavedashFrames;
    }

    private void UpdateInputs()
    {
        IsGrounded = physics.IsGrounded;
        playerInputMovementVector = playerInput.GetMovementVector();
        cameraForward = playerInput.GetCameraForward();
        joystickAngle = playerInput.GetJoystickAngle();
        joystickVelocity = playerInput.GetJoystickVelocity();
        angleDown = playerInput.GetAngleDownButton();
        angleUp = playerInput.GetAngleUpButton();
        angleDiff = physics.GetAngleDiff(playerInputMovementVector);
        jumpButtonHeld = playerInput.GetJumpButtonHeld();
        jumpButtonDown = playerInput.GetJumpButtonDown();
        fastFallHeld = playerInput.GetFastFallButtonHeld();
        fastFallDown = playerInput.GetFastFallButtonDown();
        dodge = playerInput.GetDodgeButtonDown();
        normalAttackHeld = playerInput.GetNormalAttackButtonHeld();
        normalAttackDown = playerInput.GetNormalAttackButtonDown();
        specialAttackHeld = playerInput.GetSpecialAttackButtonHeld();
        specialAttackDown = playerInput.GetSpecialAttackButtonDown();
        chargeAttackHeld = playerInput.GetChargeAttackButtonHeld();
        chargeAttackDown = playerInput.GetChargeAttackButtonDown();
        pause = playerInput.GetPauseButtonDown();
        inventory = playerInput.GetInventoryButtonDown();
        cameraStickVector = playerInput.GetCameraStickVector();
    }

    private void CheckAttack()
    {
        if (chargeAttackDown && IsGrounded)
        {
            //Ucharge
            if (angleUp)
            {
                currentAttack = AttackType.Ucharge;
            }
            //Dcharge
            else if (angleDown)
            {
                currentAttack = AttackType.Dcharge;
            }
            //Fcharge
            else
            {
                currentAttack = AttackType.Fcharge;
            }
        }
        else if (normalAttackDown)
        {
            if (IsGrounded)
            {
                //Utilt
                if (angleUp)
                {
                    currentAttack = AttackType.Utilt;
                }
                //Dtilt
                else if (angleDown)
                {
                    currentAttack = AttackType.Dtilt;
                }
                //Ftilt
                else if (playerInputMovementVector != Vector3.zero)
                {
                    currentAttack = AttackType.Ftilt;
                }
                //Jab
                else
                {
                    currentAttack = AttackType.Jab;
                }
            }
            else
            {
                //Uair
                if (angleUp)
                {
                    currentAttack = AttackType.Uair;
                }
                //Dair
                else if (angleDown)
                {
                    currentAttack = AttackType.Dair;
                }
                //Fair/Bair
                else if (playerInputMovementVector != Vector3.zero)
                {

                }
                //Nair
                else
                {
                    currentAttack = AttackType.Nair;
                }
            }
        }
        else if (specialAttackDown)
        {
            //Check IsGrounded status?

            //Uspecial
            if (angleUp)
            {
                currentAttack = AttackType.Uspecial;
            }
            //Dspecial
            else if (angleDown)
            {
                currentAttack = AttackType.Dspecial;
            }
            //Fspecial
            else if (playerInputMovementVector != Vector3.zero)
            {
                currentAttack = AttackType.Fspecial;
            }
            //Nspecial
            else
            {
                currentAttack = AttackType.Nspecial;
            }
        }
    }

    private State Transition(bool normalORCancel)
    {
        //Make an array of potential states that can be transisted to CURRENTLY
        State[] possibleTransitions;
        
        //Transition() has two modes.  
        if (normalORCancel)
            //Normal mode will be states that can be transitioned to AFTER the current state is over. (Ex. finished dodging)
            StateController.transitionDictionary.TryGetValue(currentState, out possibleTransitions);
        else
            //Cancel mode will be states that can be transitioned to DURING the current state. (Ex. Cancel ascending jump into attack)
            StateController.stateCancelDictionary.TryGetValue(currentState, out possibleTransitions);

        //Nextstate will be the state that this current entity will transition to AFTER Transition() finishes. (See bottom of method)
        State nextState = currentState;
        //Signals if the state was changed during this method
        bool changedState = false;

        //ALWAYS check for IsGrounded status
        foreach (State state in possibleTransitions)
        {
            switch (state)
            {
                case State.Idle:

                    if (playerInputMovementVector == Vector3.zero && IsGrounded)
                    {
                        currentLagFrames = 0;
                        nextState = State.Idle;
                        changedState = true;
                    }
                    break;

                case State.Walk:

                    if (playerInputMovementVector != Vector3.zero && IsGrounded && joystickVelocity <= m_JoystickVelocityToRun)
                    {
                        currentLagFrames = 0;
                        nextState = State.Walk;
                        changedState = true;
                    }
                    break;

                case State.InitialRun:

                    if (playerInputMovementVector != Vector3.zero && IsGrounded && joystickVelocity > m_JoystickVelocityToRun)
                    {
                        currentLagFrames = m_InitialRunFrames;
                        nextState = State.InitialRun;
                        changedState = true;
                    }
                    break;

                case State.Run:

                    if (playerInputMovementVector != Vector3.zero && IsGrounded)
                    {
                        currentLagFrames = m_MinInactiveFramesIoIdle;
                        nextState = State.Run;
                        changedState = true;
                    }
                    break;

                case State.Crouching:

                    if (angleDown && IsGrounded)
                    {
                        currentLagFrames = 0;
                        nextState = State.Crouching;
                        changedState = true;
                    }
                    break;

                case State.Turnaround:

                    if (IsGrounded && angleDiff > m_TurnaroundAngleThreshold)
                    {
                        currentLagFrames = m_TurnaroundFrames;
                        nextState = State.Turnaround;
                        changedState = true;
                    }
                    break;

                case State.Dodge:

                    if (IsGrounded && dodge)
                    {
                        physics.LockRotation(true);
                        currentLagFrames = m_TotalDodgeDuration;
                        nextState = State.Dodge;
                        changedState = true;
                    }
                    break;

                case State.Attack:

                    if (IsGrounded && (normalAttackDown || specialAttackDown || chargeAttackDown))
                    {
                        currentLagFrames = -1;
                        physics.LockRotation(true);
                        nextState = State.Attack;
                        changedState = true;
                    }
                    break;

                case State.Jumpsquat:

                    if (IsGrounded && jumpButtonDown)
                    {
                        if (currentState == State.Turnaround)
                            physics.LockRotation(true);
                        else
                            physics.LockRotation(playerInputMovementVector);

                        currentLagFrames = m_JumpSquatFrames;
                        nextState = State.Jumpsquat;
                        changedState = true;
                    }
                    break;

                case State.Landing:

                    if (IsGrounded)
                    {
                        currentLagFrames = m_LandLagFrames;
                        nextState = State.Landing;
                        changedState = true;
                    }
                    break;

                case State.InAir:

                    if (!IsGrounded)
                    {
                        currentLagFrames = 0;
                        physics.LockRotation(true);
                        nextState = State.InAir;
                        changedState = true;
                    }
                    break;

                case State.AscentJump1:

                    if (currentLagFrames == 0)
                    {
                        jumpDustParticles.Play();
                        currentLagFrames = m_MaxJumpHeightExtendFrames;
                        nextState = State.AscentJump1;
                        changedState = true;
                    }
                    break;

                case State.WallJump:

                    if (jumpButtonDown && playerInputMovementVector != Vector3.zero && currentNumberOfWallJumps > 0 && Physics.Raycast(transform.position, playerInputMovementVector, out hitInfo, m_DistanceToWallForWallJump, wallJumpMask))
                    {
                        if (hitInfo.normal.y < .2f && hitInfo.normal.y > -.2f)
                        {
                            wallPresent = true;
                        }

                        if (wallPresent)
                        {
                            temporaryVector = Vector3.Reflect(playerInputMovementVector, hitInfo.normal);
                            physics.LockRotation(hitInfo.normal);
                            currentNumberOfWallJumps--;
                            currentLagFrames = m_WallJumpFrames;

                            nextState = State.WallJump;
                            changedState = true;
                        }

                        wallPresent = false;
                    }
                    break;

                case State.Hitstun:
                    break;

                case State.AirAttack:
                    break;

                case State.AirDodge:

                    if (!IsGrounded && dodge && currentNumberOfAirDodges > 0)
                    {
                        physics.LockRotation(true);
                        currentLagFrames = m_TotalDodgeDuration;
                        nextState = State.AirDodge;
                        changedState = true;
                    }
                    break;

                case State.LandingAttackLag:
                    break;

                case State.LandingHitstun:
                    break;
            }

            if (changedState) break;
        }

        //Return the state that this entity will transition to
        return nextState;
    }

    /*private void InitializeInputStructs()
    {
        //Gets the length of the InputType enum
        int length = Enum.GetValues(typeof(InputType)).Length;

        //Makes an array long enough to hold all elements of InputType enum
        action = new InputAction[length];

        //Loops through each element in the "actions" array
        for (int i = 0; i < action.Length; i++)
        {
            //For each element, initialize the struct by using the default constructor. We cast the current index value to the type of the enum.
            action[i] = new InputAction((InputType)i);
        }
    }*/

    public void LoadFrameData(EntityData newData)
    {
        data = newData;
        LoadFrameData();
    }

    public void LoadFrameData()
    {
        m_LandLagFrames = data.m_LandLagFrames;
        m_TurnaroundFrames = data.m_TurnaroundFrames;
        m_InitialRunFrames = data.m_InitialRunFrames;
        m_TurnaroundAngleThreshold = data.m_TurnaroundAngleThreshold;
        m_MinInactiveFramesIoIdle = data.m_MinInactiveFramesIoIdle;
        m_JumpSquatFrames = data.m_JumpSquatFrames;
        m_TurnaroundDeaccelFrames = data.m_TurnaroundDeaccelFrames;
        m_JoystickVelocityToRun = data.m_JoystickVelocityToRun;
        m_MaxJumpHeightExtendFrames = data.m_MaxJumpHeightExtendFrames;
        m_NumberOfJumpsInAir = data.m_NumberOfJumpsInAir;
        m_NumberOfWallJumps = data.m_NumberOfWallJumps;
        m_NumberOfAirDodges = data.m_NumberOfAirDodges;
        m_DistanceToWallForWallJump = data.m_DistanceToWallForWallJump;
        m_WallJumpFrames = data.m_WallJumpFrames;
        m_FramesToJumpOfWallJump = data.m_FramesToJumpOfWallJump;
        m_PlatdropFrames = data.m_PlatdropFrames;
        m_MinVelocityToFastfall = data.m_MinVelocityToFastfall;
        m_TotalDodgeDuration = data.m_TotalDodgeDuration;
        m_DodgeStartupFrames = data.m_DodgeStartupFrames;
        m_DodgeEndLagFrames = data.m_DodgeEndLagFrames;
        m_WavedashFrames = data.m_WavedashFrames;

        physics.LoadPhysicsData();
    }

    private static void SetupDictionaries()
    {
        transitionDictionary.Clear();
        stateCancelDictionary.Clear();

        //Transitions that occur as the state's duration has ended (dodge has ended, jump squat time has ended, etc) or for states that don't have a set duration (Idle, Walk, etc)
        transitionDictionary.Add(State.Idle, new State[7] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Walk, State.Crouching });
        transitionDictionary.Add(State.Walk, new State[7] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Crouching, State.Idle });
        transitionDictionary.Add(State.InitialRun, new State[7] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.Run, State.Crouching, State.Idle });
        transitionDictionary.Add(State.Run, new State[7] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.Turnaround, State.Crouching, State.Idle });
        transitionDictionary.Add(State.Crouching, new State[7] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Walk, State.Idle });
        transitionDictionary.Add(State.Turnaround, new State[7] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.Run, State.Crouching, State.Idle });
        transitionDictionary.Add(State.Dodge, new State[7] { State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Walk, State.Crouching, State.Idle });
        transitionDictionary.Add(State.Wavedash, new State[8] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Walk, State.Crouching, State.Idle });
        transitionDictionary.Add(State.Attack, new State[8] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Walk, State.Crouching, State.Idle });
        transitionDictionary.Add(State.Jumpsquat, new State[2] { State.AirDodge, State.AscentJump1 });
        transitionDictionary.Add(State.Landing, new State[8] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Walk, State.Crouching, State.Idle });
        transitionDictionary.Add(State.LandingAttackLag, new State[8] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Walk, State.Crouching, State.Idle });
        transitionDictionary.Add(State.LandingHitstun, new State[8] { State.InAir, State.Attack, State.Jumpsquat, State.Dodge, State.InitialRun, State.Walk, State.Crouching, State.Idle });
        transitionDictionary.Add(State.InAir, new State[4] { State.Landing, State.AirAttack, State.AirDodge, State.WallJump });
        transitionDictionary.Add(State.AscentJump1, new State[5] { State.Landing, State.AirAttack, State.AirDodge, State.WallJump, State.InAir });
        transitionDictionary.Add(State.WallJump, new State[5] { State.Landing, State.AirAttack, State.AirDodge, State.WallJump, State.InAir });
        transitionDictionary.Add(State.Hitstun, new State[5] { State.Landing, State.AirAttack, State.AirDodge, State.WallJump, State.InAir });
        transitionDictionary.Add(State.AirAttack, new State[5] { State.Landing, State.AirAttack, State.AirDodge, State.WallJump, State.InAir });
        transitionDictionary.Add(State.AirDodge, new State[5] { State.Landing, State.AirAttack, State.AirDodge, State.WallJump, State.InAir });


        //Transitions that can interrupt other states
        stateCancelDictionary.Add(State.Idle, new State[2] { State.Hitstun, State.InAir });
        stateCancelDictionary.Add(State.Walk, new State[2] { State.Hitstun, State.InAir });
        stateCancelDictionary.Add(State.InitialRun, new State[3] { State.Hitstun, State.InAir, State.Jumpsquat });
        stateCancelDictionary.Add(State.Run, new State[7] { State.Hitstun, State.InAir, State.Jumpsquat, State.Dodge, State.Turnaround, State.Attack, State.Crouching });
        stateCancelDictionary.Add(State.Crouching, new State[4] { State.Hitstun, State.InAir, State.Jumpsquat, State.Dodge });
        stateCancelDictionary.Add(State.Turnaround, new State[3] { State.Hitstun, State.InAir, State.Jumpsquat });
        stateCancelDictionary.Add(State.Dodge, new State[2] { State.Hitstun, State.InAir });
        stateCancelDictionary.Add(State.Wavedash, new State[6] { State.Hitstun, State.AirDodge, State.AirAttack, State.Attack, State.WallJump, State.Jumpsquat });
        stateCancelDictionary.Add(State.Attack, new State[2] { State.Hitstun, State.InAir });
        stateCancelDictionary.Add(State.Jumpsquat, new State[1] { State.Hitstun });
        stateCancelDictionary.Add(State.Landing, new State[2] { State.Hitstun, State.InAir });
        stateCancelDictionary.Add(State.LandingAttackLag, new State[2] { State.Hitstun, State.InAir });
        stateCancelDictionary.Add(State.LandingHitstun, new State[2] { State.Hitstun, State.InAir });
        stateCancelDictionary.Add(State.InAir, new State[2] { State.Hitstun, State.Landing });
        stateCancelDictionary.Add(State.AscentJump1, new State[3] { State.Hitstun, State.AirAttack, State.AirDodge });
        stateCancelDictionary.Add(State.WallJump, new State[4] { State.Hitstun, State.AirAttack, State.AirDodge, State.Landing });
        stateCancelDictionary.Add(State.Hitstun, new State[2] { State.Hitstun, State.LandingHitstun });
        stateCancelDictionary.Add(State.AirAttack, new State[2] { State.Hitstun, State.LandingAttackLag });
        stateCancelDictionary.Add(State.AirDodge, new State[1] { State.Hitstun });

    }
}
