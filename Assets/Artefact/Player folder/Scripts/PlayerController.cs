using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
[RequireComponent(typeof(CustomGravity))]

public class PlayerController : MonoBehaviour
{
    #region Variables

    public PlayableCharacterProfile myProfile;
    [Header ("Player components")]
    [Tooltip("Player physical body (Automated)")]
    [SerializeField] Rigidbody rb;
    [Tooltip("Character animator (Automated)")]
    [SerializeField] Animator playerAni;
    [Tooltip("Animation states (set up accordingly with animator")]
    [SerializeField] List<string> animationBools;
    [Tooltip("Custom gravity script (Automated)")]
    [SerializeField] CustomGravity gravity;
    [Tooltip("Field which interacts with objects (Should be externally placed in scene and referenced)")]
    [SerializeField] public GravityField gravityField;
    [Tooltip("UI that the shows the pause menu and other buttons")]
    [SerializeField] GameObject pauseMenu;
    
    [Header(("Camera components \n" +
             "(Should be externally placed in scene and referenced)"))]
    [Tooltip("The physical camera used to look at the player")]
    [SerializeField] public Camera myCamera;
    [Tooltip("The camera's camera's matching perspective")]
    [SerializeField] GameObject myCameraOrientation;
    [Tooltip("The main camera's Cinemachine component (FreeLook only)")]
    [SerializeField] public CinemachineFreeLook defaultCameraCm;
    [Tooltip("The dive camera's Cinemachine component (Virtual only)")]
    [SerializeField] public CinemachineVirtualCamera diveCameraCm;
    
    [Tooltip("Default camera target (no rotation)")]
    [SerializeField] Transform defaultCameraTarget;
    [Tooltip("Dive camera target (no rotation)")]
    [SerializeField] Transform diveCameraTarget;

    [Header("Gravity Values")] 
    [SerializeField] public Image gravityMeter;
    [SerializeField] public float currentGravityMeter;
    [SerializeField] float currentUpdate;
    [SerializeField] bool regenCooldown;
    [SerializeField] float cooldownTimer;
    
    [Header("Cached values (Only for visualising)")]
    [SerializeField] Vector2 moveInput;
    [SerializeField] Vector2 cameraInput;
    [SerializeField] float rotateInput;
    [SerializeField] Vector3 cameraRight;
    [Tooltip("Checker that prevents character orientation fixing from overloading")]
    [SerializeField] bool landInitialised;
    [SerializeField] bool landOnce;
    Coroutine jumpingCoroutine;
    Coroutine diveCheckCoroutine;
    
    [Header("Player States (Only for visualising)")] 
    [SerializeField] bool gamePaused;
    [SerializeField] public bool zerograv;
    [SerializeField] bool shifted;
    [SerializeField] bool immobile;
    [SerializeField] public bool grounded;
    [SerializeField] public bool diving;
    [SerializeField] bool noGroundDetected;
    [SerializeField] bool shiftDiving; //to be used only when toggling out from zero gravity, in order to smooth landing.
    [SerializeField] bool cameraTransitioned;
    [SerializeField] bool fieldEnabled;
    [SerializeField] public bool aimedDownSights;
    [SerializeField] bool cameraCentering;
    #endregion
    
    // Start is called before the first frame update
    void Start()
    {
        //lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        //declare variables
        rb = GetComponent<Rigidbody>();
        gravity = GetComponent<CustomGravity>();
        playerAni = GetComponent<Animator>();
        
        //customise components for initial behaviour
        gravityField.gameObject.SetActive(fieldEnabled);
        gravityField.owner = gameObject.GetComponent<PlayerController>();
        defaultCameraCm.m_Orbits[1].m_Radius = myProfile.defaultDistance;
        currentGravityMeter = myProfile.maxGravityMeter;
        pauseMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //make the targets match player position always
        defaultCameraTarget.position = gameObject.transform.position;
        diveCameraTarget.position = gameObject.transform.position;
        
        //run constant checks
        AnimationStates();
        if (!zerograv)
        {
            CheckGroundedNew();
            
            //check if should begin diving, might need exceptions as a similar behaviour to dive should be attempted when gravity changes in low heights, etc
            if (!grounded && !diving)
            {
                CheckDive();
            }
        }
        UpdateGravityMeter();
        
    }
    
    // Recurring but not frame dependent functions
    void FixedUpdate()
    {
        if (immobile)
        {
            return;
        }
        
        //relatively grounded movement (including jumping motion)
        if (!diving)
        {
            if (!aimedDownSights)
            {
                //gather input information
                Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            
                //execute when input exists
                if (inputDirection.magnitude > 0.1f)
                {
                    //calculate directions based on definition of up by the perspective of the camera
                    Vector3 cameraForward = Vector3.ProjectOnPlane(myCamera.transform.forward, myCameraOrientation.transform.up);
                    Vector3 cameraRight = Vector3.ProjectOnPlane(myCamera.transform.right, myCameraOrientation.transform.up);
                
                    //translate a character direction based on input
                    //flat direction
                    Vector3 targetDirection = (cameraForward * moveInput.y  + cameraRight * moveInput.x).normalized;
                    
                    //an external local value stored for slopes
                    Vector3 finalDirection = targetDirection;
                    
                    //slope detection
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, -transform.up, out hit, 1.1f))
                    {
                        Vector3  slopeNormal = hit.normal;
                        float slopeAngle = Vector3.Angle(slopeNormal, myCameraOrientation.transform.up);
                        bool upwardSlope = Vector3.Dot(transform.forward, slopeNormal) < 0;
                        
                        if (upwardSlope)
                        {
                            if (slopeAngle > myProfile.minSlopeAngle && slopeAngle <= myProfile.maxSlopeAngleAscent)
                            {
                                finalDirection = Vector3.ProjectOnPlane(targetDirection, slopeNormal).normalized;
                            }
                            //if the slope exceeds the limit, dont move
                            else if (slopeAngle > myProfile.maxSlopeAngleAscent && upwardSlope)
                            {
                                finalDirection = Vector3.zero;
                            }
                        }
                        else
                        {
                            //if the slope goes down and doesnt exceed the limit, go along the slope
                            if (slopeAngle > myProfile.minSlopeAngle && slopeAngle <= myProfile.maxSlopeAngleDescent)
                            {
                                finalDirection = Vector3.ProjectOnPlane(targetDirection, slopeNormal).normalized;
                            }
                            //otherwise just go forward
                        }
                    }
                    //calculate and make the player face the direction if the slope allows it to
                    if (finalDirection != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, myCameraOrientation.transform.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, myProfile.rotationSpeed * Time.fixedDeltaTime);
                    }
                    
                    //make sure the character faces the direction that it's moving towards
                    float directionAligntment = Vector3.Dot(transform.forward, targetDirection);
                    bool aligned = directionAligntment >= 0.9f;
                    
                    //move off if character matches direction
                    if (aligned)
                    {
                        //finalise with character forward movement only, no strafing
                        if (grounded)
                        {
                            rb.AddForce(finalDirection * myProfile.walkAccelerationSpeed, ForceMode.Acceleration);
                        }
                        else
                        {
                            rb.AddForce(transform.forward * myProfile.jumpForwardAccelerationSpeed, ForceMode.Acceleration);
                        }
                    }
                    //calculate components for drag application (grounded)
                    Vector3 forwardVelocity = Vector3.Project(rb.velocity, transform.forward);
                    Vector3 nonForwardVelocity = rb.velocity - forwardVelocity;
                    Vector3 nonForwardDragForce = -nonForwardVelocity * myProfile.groundedDrag; //custom drag

                    //the same but for aerial
                    Vector3 upwardVelocity = Vector3.Project(rb.velocity, -gravity.gravitationalDirection);
                    Vector3 nonForwardVelocityAerial = nonForwardVelocity - upwardVelocity;
                    Vector3 nonForwardDragForceAir = -nonForwardVelocityAerial* myProfile.groundedDrag / 2;
                    
                    //add opposite forces to player
                    if (grounded)
                    {
                        rb.AddForce(nonForwardDragForce, ForceMode.Acceleration);
                    }
                    else
                    {
                        rb.AddForce(nonForwardDragForceAir, ForceMode.Acceleration) ;
                    }

                }
                //finally, if no input is made brake the player if it's grounded
                else
                {
                    if (grounded)
                    {
                        rb.AddForce(rb.velocity * -myProfile.decelerationSpeed, ForceMode.Force);
                    }
                }
                // prevent from building infinite speed
                if (rb.velocity.magnitude > myProfile.maxSpeedWalk && grounded)
                {
                    rb.velocity = rb.velocity.normalized * myProfile.maxSpeedWalk;
                }
                else if (rb.velocity.magnitude > myProfile.maxSpeedJump && !grounded)
                {
                    rb.velocity = rb.velocity.normalized * myProfile.maxSpeedJump;
                }
            }
            else if(grounded && aimedDownSights)
            {
                //match the camera's forward direction on a horizontal plane
                Vector3 flattenedForward = Vector3.ProjectOnPlane(myCamera.transform.forward, transform.up);
                Quaternion targetRotation = Quaternion.LookRotation(flattenedForward, transform.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, myProfile.rotationSpeed * Time.fixedDeltaTime);
                
                //movement in all directions (allow strafing)
                if (moveInput.magnitude > 0.1f)
                {
                    Vector3 targetMovement = transform.forward * moveInput.y + transform.right * moveInput.x;
                    rb.AddForce(targetMovement * myProfile.aimedAccelerationSpeed, ForceMode.Acceleration);
                    
                    //calculate components for drag calculation
                    Vector3 directionalVelocity = Vector3.Project(rb.velocity, targetMovement);
                    Vector3 nonMatchingVelocity = rb.velocity - directionalVelocity;
                    Vector3 nonMatchingDragForce = -nonMatchingVelocity * 3f; //custom drag
                    
                    //add opposite forces to player
                    rb.AddForce(nonMatchingDragForce, ForceMode.Acceleration);
                }
                
                //add braking to the movement
                else if (grounded)
                {
                    rb.AddForce(rb.velocity * -myProfile.decelerationSpeed, ForceMode.Force);
                }
                // prevent from building infinite speed
                if (rb.velocity.magnitude > myProfile.maxSpeedAim && grounded)
                {
                    rb.velocity = rb.velocity.normalized * myProfile.maxSpeedAim;
                }
            }
        }
        //aerial movement (diving in the air)
        else
        {
            //movement is enabled when character is in proper position (head first)
            //there may be inaccuracies, so distance is calculated instead of a precise comparison
            if (Vector3.Distance(transform.up, gravity.gravitationalDirection) < 0.01f)
            {
                //check if there's active input for character rotation
                if (rotateInput != 0)
                {
                    float rotationAmount = myProfile.diveRotationSpeed * rotateInput * Time.deltaTime;
                    transform.Rotate(0, rotationAmount, 0);
                }
                
                //if camera centering is enabled, make the target match player orientation
                if (cameraCentering)
                {
                    Quaternion cameraTargetLocation = Quaternion.LookRotation(transform.up, -transform.forward);
                    diveCameraTarget.rotation = Quaternion.RotateTowards(diveCameraTarget.rotation, cameraTargetLocation, 60f * Time.fixedDeltaTime);
                }
                
                //calculate movement
                Vector3 targetMovement = -transform.forward * moveInput.y + transform.right * moveInput.x;
                
                //drag must affect these directions
                Vector3 lateralVelocity = rb.velocity - Vector3.Project(rb.velocity, gravity.gravitationalDirection);
                if (targetMovement.magnitude > 0.1f)
                {
                    rb.AddForce(targetMovement * myProfile.lateralAirDiveSpeed, ForceMode.Acceleration);

                    Vector3 inputDirection = -transform.forward * moveInput.y + transform.right * moveInput.x;
                    inputDirection = inputDirection.normalized;
                    
                    //brake the player if it doesn't match the input direction
                    float alignment = Vector3.Dot(lateralVelocity.normalized, inputDirection);
                    if(Math.Abs(alignment) < 0.9f)
                    {
                        rb.AddForce(-lateralVelocity.normalized * myProfile.divingDrag, ForceMode.Acceleration);
                    }
                }
                
                //decelerate the player when no input
                else
                {
                    rb.AddForce(-lateralVelocity * myProfile.divingDrag, ForceMode.Acceleration);
                }
                
                //calculate the velocity along the lateral plane from the gravitational direction
                float lateralSpeed =  lateralVelocity.magnitude;
                if (lateralSpeed > myProfile.maxLateralSpeed)
                {
                    //calculate a max lateral speed
                    Vector3 clampedLateralSpeed = lateralVelocity.normalized * myProfile.maxLateralSpeed;
                    
                    //grab the current velocity and just add the lateral speed as appropriate
                    rb.velocity = Vector3.Project(rb.velocity, gravity.gravitationalDirection) + clampedLateralSpeed;
                }
            }
            
            //adjust the character to fall head first
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, gravity.gravitationalDirection) * transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 50f * Time.fixedDeltaTime);
            
            
            //calculate velocity along the gravitational direction
            Vector3 fallingSpeed = Vector3.Project(rb.velocity, gravity.gravitationalDirection);
            
            //if the falling speed does not exceed the maximum dive speed, increase by acceleration
            if (fallingSpeed.magnitude < myProfile.terminalVelocity)
            {
                rb.AddForce(gravity.gravitationalDirection * myProfile.diveAccelerationSpeed, ForceMode.Acceleration);
            }
            
            if (fallingSpeed.magnitude > myProfile.terminalVelocity)
            {
                //grab current velocity
                Vector3 currentVelocity = rb.velocity;
                //calculate velocity proportionate to falling direction
                Vector3 velocityAlongFall = Vector3.Project(currentVelocity, gravity.gravitationalDirection);
                //hard set velocity to maximum falling speed + current velocity STRIPPED of the falling velocity, leaving any other forces (for example lateral velocity)
                rb.velocity = Vector3.Project(rb.velocity, gravity.gravitationalDirection).normalized * myProfile.terminalVelocity + (currentVelocity - velocityAlongFall);
            }
            
        }   
    }
    
    //Events that are called upon to make changes
    #region Reaction events

    //material changers
    void UpdatePlayerSkin(Material newMaterial)
    {
        //start the algorithm
        FindChildren(transform,newMaterial);
    }
    void FindChildren(Transform part, Material chosenMaterial)
    {
        //go through every child and change the material chosen
        foreach (Transform child in part)
        {
            Renderer partRenderer = child.GetComponent<Renderer>();

            if (partRenderer != null)
            {
                partRenderer.material = chosenMaterial;
            }
            //do this for each until there are no more children
            FindChildren(child, chosenMaterial);
        }
    }
    
    
    void CancelGravity()
    {
        if (!shifted)
        {
            return;
        }
        //interrupt jump if in progress
        if (jumpingCoroutine != null)
        {
            StopCoroutine(jumpingCoroutine);
            jumpingCoroutine = null;
        }
        
        //revert material change
        UpdatePlayerSkin(myProfile.defaultSkin);  
        
        //configure states to correct player status no matter what scenario
        gravity.RevertGravity(true,0f);
        zerograv = false;
        immobile = false;
        shifted = false;
        shiftDiving = false;
        playerAni.SetBool("Zero Grav", false);
        
        //land down player if they have ground as soon as they revert
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.1f))
        {
            if (hit.collider.gameObject.tag != "Player" &&
                hit.collider.gameObject.GetComponent<InteractableObject>() != null || 
                hit.collider.gameObject.tag == "Non-Steppable")
            {
                StartCoroutine(LandPlayer(new RaycastHit(), .5f, false));
            }
        }
        
        //allow for landing again if necessary (orientation matching)
        landOnce = false;
    }
    
    //flips the player upright when landing after a free fall / dive / gravity shift
    IEnumerator LandPlayer(RaycastHit hit, float duration, bool smoothGravity)
    {
        //set state
        landInitialised = true;
        
        
        //set up to calculate the way the character will flip
        Quaternion targetRotation = Quaternion.identity;
        
        //draw a similarity comparison of how close the new direction or surface matches the default  up and down
        bool closeToNormalDown = (Vector3.Dot(gravity.gravitationalDirection.normalized, Vector3.down) > .9f || 
                                  Vector3.Dot(hit.normal.normalized, Vector3.up) > .9f);
        
        //if gravity smoothing is on and the comparison passes as relatively similar, revert to normality
        if (closeToNormalDown && smoothGravity)
        {
            gravity.SoftSetGravity(Vector3.down);
            shifted = false;
            UpdatePlayerSkin(myProfile.defaultSkin);
        }
        
        //smooth with surface
        else if (smoothGravity)
        {
            targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            gravity.SoftSetGravity(-hit.normal);
        }
        
        //retain current direction
        else if(!smoothGravity)
        {
            targetRotation = Quaternion.FromToRotation(transform.up, -gravity.gravitationalDirection) * transform.rotation;
        }
        
        // start slerp based on desired length
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            float t = Mathf.Clamp01(elapsedTime / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        //double checkers
        transform.rotation = targetRotation;
        
        //set status
        grounded = true;
        if (smoothGravity)
        {
            gravity.SetNewGravity(gravity.gravitationalDirection, true,0f);
            shiftDiving = false;
        }
        //now move on to do camera
        LockCamera(true);
        myCameraOrientation.transform.up = transform.up;

        //set states to avoid overload
        landInitialised = false;
        
        //this will prevent it from being called again, as it sometimes is due to float precision
        landOnce = true;
        yield return null;
    }
    
    //dive cool down
    IEnumerator QueueDive()
    {
        //enable state, then wait to apply changes
        noGroundDetected = true;
        yield return new WaitForSeconds(myProfile.diveWaitTime);
        //if there is no interruption, carry on
        diving = true;
        playerAni.SetBool("Diving", true);
        LockCamera(false);
        diveCheckCoroutine = null;
    }
    
    //camera changer
    void LockCamera(bool locked)
    {
        if (locked)
        {
            SyncFreeLookCamera();
        }
        SyncDiveCamera();
        defaultCameraCm.gameObject.SetActive(locked);
        diveCameraCm.gameObject.SetActive(!locked);
        
    }
    
    //a manual transitions that synchronizes the camera's position/rotation during transitions, 
    
    //match rotation when going to dive
    void SyncDiveCamera()
    {
        if (defaultCameraCm.gameObject.activeInHierarchy)
        {
            diveCameraTarget.rotation = myCamera.transform.rotation;
        }
    }

    //match y-axis position when going back to normal
    //Cinemachine may be able to do this, but I wasn't able to figure it out
    void SyncFreeLookCamera()
    {
        diveCameraTarget.transform.rotation = Quaternion.LookRotation(diveCameraTarget.transform.forward, transform.up);
        float newYAxis = Vector3.Dot(gravity.gravitationalDirection, myCamera.transform.forward);
        newYAxis = (newYAxis - -1) / (1 - -1) * (1 - 0) + 0;
        defaultCameraCm.m_YAxis.Value = newYAxis;
    }

    //fix animation controller to one state when necessary
    void SetSingleAnimation(string stateName)
    {
        foreach (string state in animationBools)
        {
            playerAni.SetBool(state, false);
        }
        playerAni.SetBool(stateName, true);
    }
    
    #endregion

    //Recurring events that expect changes
    #region Status checks
    //check aerial states
    void CheckGroundedNew()
    {
        RaycastHit hit  = new RaycastHit();
        //default length
        float raylength = 1.1f;
        const float offsets = .5f;
        
        //to cover more area multiple rays are created from the edges of the player's side
        
        //starting points
        Vector3[] origins =
        {
            transform.position,
            transform.position + transform.right * offsets,
            transform.position - transform.right * offsets,
            transform.position + transform.forward * offsets,
            transform.position - transform.forward * offsets
        };
        
        //store an output to take away from loop
        bool result = false;
        foreach (Vector3 ray in origins)
        {
            if (Physics.Raycast(ray, gravity.gravitationalDirection, out hit, raylength))
            {
                //exceptions
                if (hit.collider.gameObject.tag == "Player" || 
                    hit.collider.gameObject.GetComponent<InteractableObject>() != null || 
                    hit.collider.gameObject.tag == "Non-Steppable")  
                {
                    continue;
                }
                
                //check for appropriate reaction
                if (diving)
                {
                    diving = false;
                    SetSingleAnimation("Grounded");
                    StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
                }
                
                //fail safes for when the player doesn't match the right direction
                
                //aerial check
                else if (!diving && transform.up != -gravity.gravitationalDirection && !grounded)
                {
                    StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
                }
                
                //grounded check
                else if (transform.up != -gravity.gravitationalDirection && !landInitialised && !landOnce)
                {
                    StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
                }
                
                //if all is well, then the results are positive
                result = true;
                noGroundDetected = false;
                if (!defaultCameraCm.gameObject.activeInHierarchy)
                {
                    LockCamera(true);
                }
                
                //exit loop to avoid redundant checks
                break;
            }
        }

        if (!result)
        {
            if (aimedDownSights)
            {
                gravityField.TriggerAim(false);
            }
        }
        grounded = result;
    }
    void CheckDive()
    {
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, gravity.gravitationalDirection, out hit, myProfile.diveLength))
        {
            if (!noGroundDetected)
            {
                //keep checking until no ground is detected, after just ignore
                if (shiftDiving)
                {
                    //skip cooldown if gravity shifted
                    diving = true;
                    playerAni.SetBool("Diving", true);
                    LockCamera(false);
                }
                //when there's no dive cooldown, start it
                else if(diveCheckCoroutine == null)
                {
                    diveCheckCoroutine = StartCoroutine(QueueDive());
                }
            }
        }
        else
        {
            //if a cooldown has started, restart it to resume ground proximity checking again
            playerAni.SetBool("Diving", false);
            if (noGroundDetected)
            {
                StopCoroutine(diveCheckCoroutine);
                noGroundDetected = false;
            }
        }
    }
    
    //check corresponding animation states
    void AnimationStates()
    {
        //check movement state
        if (moveInput.magnitude > 0 && rb.velocity.magnitude > 0 && grounded)
        {
            playerAni.SetBool("Movement", true);
        }
        else if (moveInput.magnitude == 0 && grounded)
        {
            playerAni.SetBool("Movement", false);
        }
        //check grounded state
        //if there's no movement then state will revert to grounded by default on animation behaviour
        playerAni.SetBool("Grounded", grounded);
    }
    
    //check gravity meter
    void UpdateGravityMeter()
    {
        //the sign of the current rate (will return  if its positive or negative
        float cachedUse = Mathf.Sign(currentUpdate);
        
        //set states
        if (shifted)
        {
            if (zerograv)
            {
                currentUpdate = myProfile.zeroGravRate;
            }
            else if (shiftDiving || shifted && diving)
            {
                currentUpdate = myProfile.shiftDiveRate;
            }
            else if (grounded)
            {
                currentUpdate = myProfile.groundedRate;
            }   
            //just in case, change to negative otherwise leave normal
            if (currentUpdate > 0)
            {
                currentUpdate *= -1f;
            }
        }
        else
        {
            currentUpdate = myProfile.regenRate;
        }
        
        //if previously negative and the current rate is positive, start a cooldown
        if (cachedUse == -1 && currentUpdate > 0)
        {
            regenCooldown = true;
            cooldownTimer = myProfile.regenCooldownLength;
        }
        
        //update accordingly!
        if (!regenCooldown && currentUpdate > 0 || currentUpdate < 0)
        {
            currentGravityMeter += currentUpdate * Time.deltaTime;
            if (currentGravityMeter > myProfile.maxGravityMeter)
            {
                currentGravityMeter = myProfile.maxGravityMeter;
            }
            //exhaustion
            else if (currentGravityMeter < 0)
            {
                currentGravityMeter = 0;
                CancelGravity();
            }
        }
        
        //run cooldown while its active
        if (regenCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                regenCooldown = false;
            }
        }
        
        //update visual meter
        gravityMeter.fillAmount = currentGravityMeter / myProfile.maxGravityMeter;
    }
    #endregion
    
    //All events called by the input system
    #region Input events
    //General movement
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    //jumping bundled here because it should only occur on input
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            //jump if not ongoing already
            if (grounded && jumpingCoroutine == null)
            {
                jumpingCoroutine = StartCoroutine(SmoothJump());
            }
        }
    }
    
    //a coroutine was decided of a simple impulse because they originally lacked consistency,
    //this coroutine assures that the right amount of jump height is always met
    IEnumerator SmoothJump()
    {
        //prepare checkers
        bool liftoff = false;
        float jumpDuration = .1f;
        float elapsedTime = 0;

        //duration loop
        while (elapsedTime < jumpDuration)
        {
            if (!liftoff && !grounded)
            {
                liftoff = true;
            }
            
            //when player touches ground again after lifting off, stop jumping
            if (liftoff && grounded)
            {
                yield break;
            }
            
            //push player accordinly for the duration and distance established
            float forcePerframe = myProfile.jumpForce / jumpDuration * Time.fixedDeltaTime;
            rb.AddForce(transform.up * forcePerframe, ForceMode.Acceleration);
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        //disable the coroutine
        jumpingCoroutine = null;
    }
    
    public void AerialRotate(InputAction.CallbackContext context)
    {
        rotateInput = context.ReadValue<float>();
    }
    public void CameraRotate(InputAction.CallbackContext context)
    {
        //match rotation on mouse input when the dive camera is activated
        if (!defaultCameraCm.gameObject.activeInHierarchy && !cameraCentering)
        {
            cameraInput = context.ReadValue<Vector2>();
            diveCameraTarget.Rotate( -cameraInput.y* 5f * Time.deltaTime, cameraInput.x *5f * Time.deltaTime, 0f);
        }
    }
    
    //Gravity functions
    public void RevertGravity(InputAction.CallbackContext context)
    {
        //do only once
        if (context.started)
        {
            CancelGravity();
        }
    }
    public void ToggleGravity(InputAction.CallbackContext context)
    {
        //do only once
        if (context.started)
        {
            //change player skin
            if (!shifted)
            {
                UpdatePlayerSkin(myProfile.shiftedSkin);
            }
            shifted = true;
            
            if (!zerograv)
            {
                //disable gravitational forces
                //use new gravity component
                gravity.SetZeroGravity(myProfile.zeroGravDrag);
                
                
                //immobilise player
                immobile = true;
                zerograv = true;
                grounded = false;
                diving = false;
                shiftDiving = false;
                noGroundDetected = false;
                cameraCentering = false;

                SetSingleAnimation("Zero Grav");
                //fix camera with coroutine
                LockCamera(false);
            }
            else
            {
                //calculate new gravity
                Vector3 cameraDirection = myCamera.transform.forward;
                gravity.SetNewGravity(cameraDirection, true, 0f);
                
                //unlock player
                immobile = false;
                zerograv = false;
                shiftDiving = true;
                noGroundDetected = false;
                playerAni.SetBool("Zero Grav", false);
            }
            
            //stop jumping rise if ongoing
            if (jumpingCoroutine != null)
            {
                StopCoroutine(jumpingCoroutine);
                jumpingCoroutine = null;
            }
        }
    }
    
    //Object interaction functions
    public void EnableField(InputAction.CallbackContext context)
    {
        //do only once
        if (context.started)
        {
            //two-way switch behaviour
            fieldEnabled = !fieldEnabled;
            //if the switch is off, disable the field
            if (!fieldEnabled)
            {
                gravityField.DisableField();
            }
            //enable and disable accordingly
            gravityField.gameObject.SetActive(fieldEnabled);
        }
    }
    public void AimObject(InputAction.CallbackContext context)
    {
        //aiming is not allowed under this circumstance
        if (!grounded && !zerograv && !diving)
        {
            return;
        }
        
        //alternative behaviour, if diving recentre the camera
        if (diving)
        {
            if (context.started)
            {
                cameraCentering = true;
            }
            else if (context.canceled)
            {
                cameraCentering = false;
            }
        }
        
        //normal behaviour
        else
        {
            //start aiming when initialised 
            //check that there are objects to shoot with
            if (context.started && fieldEnabled && gravityField.objectsInOrbit.Count > 0)
            {
                gravityField.TriggerAim(true);
            }
        
            //cancel when let go of input
            //only cancel if it has been aimed to avoid breakage
            else if (context.canceled && aimedDownSights)
            {
                gravityField.TriggerAim(false);
            }
        }
    }
    public void ScrollObjects(InputAction.CallbackContext context)
    {
        //for some reason delta scroll is read as a vector 2, given that only the y-axis is used a float is preferred
        float inputScroll = context.ReadValue<Vector2>().y;
        if (context.started)
        {
            //only scroll through objects if they are aimed
            if (aimedDownSights)
            {
                gravityField.ScrollOrbit(inputScroll);
            }
        }
    }
    public void ShootObject(InputAction.CallbackContext context)
    {
        //do once 
        if (context.started)
        {
            if (aimedDownSights)
            {
                //simply change gravity in similar fashion to the player
                gravityField.ShootObject(myCamera.transform.forward);
            }
        }
    }
    
    //other
    public void PauseGame(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            //switch behaviour
            gamePaused = !gamePaused;
            pauseMenu.SetActive(gamePaused);
            if (gamePaused)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    #endregion
}
