using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CustomGravity))]
public class InteractableObject : MonoBehaviour, ICollisionReactable
{
    #region Variables
    [Header("Settings")]
    [Tooltip("The type of object it is")]
    [SerializeField] public objectType objectTag;
    [Tooltip("Custom gravity component")]
    [SerializeField] public CustomGravity gravity;
    [Tooltip("Estimate terminal velocity")]
    [SerializeField] public float terminalVelocity = 20f;
    [Tooltip("A drag that will naturally clamp the velocity to match the terminal velocity(Automated)")]
    [SerializeField] float dragResistance = 0f;
    [Tooltip("A unique name used for certain reactions (Normally unused)")]
    [SerializeField] string uniqueName = "None";
    
    [Header("Attractor information (Only for visualising)")]
    [Tooltip("The main slot the object is attracted to")]
    [SerializeField] public Transform attractionPoint;
    [Tooltip("The launch position that the object is attracted to")]
    [SerializeField] public Transform launchPoint;
    [Tooltip("The field component")]
    [SerializeField] public GravityField myAttractor;
    
    [Header("Attraction settings")] 
    [SerializeField] public ConfigurableJoint myJoint;
    [Tooltip("Maximum distance from the attraction points until breakage")]
    [SerializeField] float maxDistance = 2f;
    [Tooltip("Maxmimum applied force until breakage")]
    [SerializeField] float breakForce = 100f;
    [Tooltip("The speed of the object when it translates between attraction points")]
    [SerializeField] float pullSpeed = 5f;

    [Header("States (Only for visualising")] 
    [SerializeField] public bool usable = true;
    [SerializeField] public bool orbiting;
    [SerializeField] public bool launched;
    [SerializeField] bool terminalVelocityReached;
    [Tooltip("For precise translation between points")]
    [SerializeField] bool queuedMovement;
    
    
    [Header("Debug settings (Only for visualising")] 
    [SerializeField] Material brokenState;
    
    //unique events
    Coroutine lockingCoroutine;
    public Action uniqueCollisionReaction;
    public Action uniqueBreakReaction;
    
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        //set needed components for interactable objects at the beginning
        gravity = GetComponent<CustomGravity>();
        dragResistance = gravity.gravityForceUnit / terminalVelocity;
        gameObject.tag = "Interactable";
        
        
        //if there's no unique reaction initiated beforehand in Awake
        if (uniqueBreakReaction == null)
        {
            //apply a default reaction (can change later)
            uniqueBreakReaction += BreakItem;
        }
    }
    void FixedUpdate()
    {
        //apply drag for falling resistance
        if (gravity.rb.velocity.magnitude > terminalVelocity && !terminalVelocityReached)
        {
            terminalVelocityReached = true;
            gravity.rb.drag = dragResistance;
        }
        
        //cancel out when speed slows down?  
        //the actual tweak is up for manipulation
        else if (gravity.rb.velocity.magnitude < terminalVelocity/2 && terminalVelocityReached)
        {
            terminalVelocityReached = false;
            gravity.rb.drag = 0f;
        }
        
        
        //if unusable, dont run recognition behaviour
        if (!usable)
        {
            return;
        }
        //if a new slot is assigned, translate towards it
        if (queuedMovement && orbiting)
        {
            //if there's a point to launch towards, go to it with priority
            if (launchPoint)
            {
                //transform.position = launchPoint.transform.position;
                transform.position = Vector3.MoveTowards(transform.position, launchPoint.TransformPoint(Vector3.zero),  pullSpeed * Time.fixedDeltaTime);
            }
            //otherwise if its orbiting normally, there should be a point to go to
            else
            {
                //transform.position = attractionPoint.transform.position;
                transform.position = Vector3.MoveTowards(transform.position, attractionPoint.TransformPoint(Vector3.zero),  pullSpeed * Time.fixedDeltaTime);
            }
            
            //when the object reaches the central position after transition, stop manual movement
            if (gameObject.transform.localPosition == Vector3.zero && lockingCoroutine == null)
            {
                //finally enable the joint, as the object won't be translating anymore
                //lockingCoroutine = StartCoroutine(LockItem());
                queuedMovement = false;
                LockJoint(true);
            }
        }

    }

    
    IEnumerator LockItem()
    {
        yield return new WaitForSeconds(2f);
        queuedMovement = false;
        LockJoint(true);
        lockingCoroutine = null;
    }
    #region InteractionEvents
    
    //player interactions
    
    //enter object into orbit
    public void ToggleOrbit(bool enable , Transform incomingTarget = null, bool retainGravity = false, GravityField attractor = null)
    {
        //failsafe, if unusable, exit out immediately
        if (!usable)
        {
            return;
        }
        if (enable)
        {
            //set up all relevant information from the field it's getting grabbed from
            myAttractor = attractor;
            orbiting = true;
            attractionPoint = incomingTarget;
            
            //a parent transform allows for the object to be translated removing the need for the object to have to follow the player via forces (for example during high speeds)
            //which strain the joint, the joint just allows for collisions to interact and break the link
            gameObject.transform.SetParent(attractionPoint);
            //gameObject.layer = LayerMask.NameToLayer("Held");
            
            //prepare movement to new position
            queuedMovement = true;
            
            //get the joint ready with zero gravity and high drag
            ProfileAttraction(attractionPoint.GetComponent<Rigidbody>());
            gravity.SetZeroGravity(3f);
        }
        else
        {
            
            //remove all relevant information
            myAttractor = attractor;
            orbiting = false;
            attractionPoint = incomingTarget;
            gameObject.transform.SetParent(null);
            //gameObject.layer = LayerMask.NameToLayer("Default");
            
            //destroy joint
            if (myJoint)
            {
                myJoint.breakForce = 0f;
                Destroy(myJoint);
            }
            
            //revert gravity?
            if (!retainGravity)
            {
                gravity.RevertGravity(false,0f);
            }
            
        }
    }
    
    //prepare object to launch
    public void ReadyObject(bool enable, Transform pointToGo)
    {
        //failsafe again
        if (!usable)
        {
            return;
        }
        
        //the same logic but instead goes to a different transform
        if (enable)
        {
            gameObject.transform.SetParent(pointToGo);
            queuedMovement = true;
            launchPoint = pointToGo;
            LockJoint(false);
            myJoint.connectedBody = launchPoint.GetComponent<Rigidbody>();
            
        }
        else
        {
            //else just return to previous point or drop
            launchPoint = null;
            if (attractionPoint)
            {
                myJoint.connectedBody = attractionPoint.GetComponent<Rigidbody>();
                LockJoint(false);
                gameObject.transform.SetParent(attractionPoint);
                queuedMovement = true;
            }

            else
            {
                gameObject.transform.SetParent(null);
            }
            
        }
    }
    
    //load the settings for the joint
    void ProfileAttraction(Rigidbody attractionPoint)
    {
        myJoint = gameObject.AddComponent<ConfigurableJoint>();
        myJoint.connectedBody = attractionPoint;
        myJoint.autoConfigureConnectedAnchor = false;
        myJoint.anchor = Vector3.zero;
        myJoint.connectedAnchor = Vector3.zero;
        myJoint.linearLimit = new SoftJointLimit{limit = maxDistance};
        LockJoint(false);
        myJoint.breakForce = breakForce;
    }
    
    //lock in/confine the joint
    void LockJoint(bool toggle)
    {
        if (toggle)
        {
            gameObject.transform.localPosition = Vector3.zero;
            myJoint.xMotion = ConfigurableJointMotion.Limited;
            myJoint.yMotion = ConfigurableJointMotion.Limited;
            myJoint.zMotion = ConfigurableJointMotion.Limited;
        }
        else
        {
            myJoint.xMotion = ConfigurableJointMotion.Free;
            myJoint.yMotion = ConfigurableJointMotion.Free;
            myJoint.zMotion = ConfigurableJointMotion.Free;
        }

    }
    
    
    //environment interactions
    
    //remove the object from list when joint breaks
    void OnJointBreak(float breakForce)
    {
        myAttractor.RemoveItem(gameObject.GetComponent<InteractableObject>());
    }
    
    //check for unique interactions when collided
    void OnCollisionEnter(Collision other)
    {
        if (terminalVelocityReached)
        {
            //check for a reaction with the colliding object
            if(other.gameObject.GetComponent<ICollisionReactable>() != null)
            {
                other.gameObject.GetComponent<ICollisionReactable>().OnHighSpeedCollision(gameObject.GetComponent<InteractableObject>());
            }
            //if there is none, just apply reaction with itself (in the event of true, this will be called regardless at the end of the other function)
            else
            {
                OnHighSpeedCollision();
            }
        }
        
        //to prevent continuous check, for now only check unique reactions when its launched
        if (launched)
        {
            if (other.gameObject.GetComponent<ICollisionReactable>() != null)
            {
                other.gameObject.GetComponent<ICollisionReactable>().SoftCollision(gameObject);
            }
        }
        
    }
    
    //defaults (not meant for use)
    void BreakItem()
    {
        if (!usable)
        {
            return;
        }
        
        //change material for interpretation, normally the object would get deleted or deactivated
        gameObject.GetComponent<Renderer>().material = brokenState;
        usable = false;
    }

    #endregion

    #region Collision interface
    
    public void OnHighSpeedCollision(InteractableObject otherbody = null)
    {
        //brake the object
        terminalVelocityReached = false;
        gravity.rb.drag = 0f;
        //run a check for what type of object it is
        switch (objectTag)
        {
            case objectType.LightItem:
                uniqueBreakReaction?.Invoke();
                break;
            case objectType.HeavyItem:
                Debug.Log("clash!");
                break;
            case objectType.StaticItem:
                Debug.Log("nothing!");
                break;
        }
        
        //then check the same for another object if possible
        if (otherbody != null)
        {
            switch (otherbody.objectTag)
            {
                case objectType.LightItem:
                    otherbody.uniqueBreakReaction?.Invoke();
                    break;
                case objectType.HeavyItem:
                    Debug.Log("clash!");
                    break;
                case objectType.StaticItem:
                    Debug.Log("nothing!");
                    break;
            }
        }
    }
    
    public void SoftCollision(GameObject otherbody = null)
    {
        //optional, not all objects have a reaction
        uniqueCollisionReaction?.Invoke();
    }
    
    public string ReturnUniqueName()
    {
        return uniqueName;
    }
    #endregion
}
