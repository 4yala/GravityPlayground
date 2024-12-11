using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(CustomGravity))]
public class InteractableObject : MonoBehaviour, ICollisionReactable
{
    #region Variables
    [Header("Settings")]
    [Tooltip("The type of object it is")]
    [SerializeField] public objectType objectTag;
    [Tooltip("Custom gravity component")]
    [SerializeField] public CustomGravity gravity;
    [SerializeField] float terminalVelocity = 20f;
    [SerializeField] float dragResistance = 0.5f;
    [SerializeField] string uniqueName = "None";
    
    [Header("Attractor information (Only for visualising)")]
    [Tooltip("The main slot the object is attracted to")]
    [SerializeField] public Transform attractionPoint;
    [Tooltip("The launch position that the object is attracted to")]
    [SerializeField] public Transform launchPoint;
    [Tooltip("The field component")]
    [SerializeField] public GravityField myAttractor;
    
    [Header("Attraction settings")] 
    [SerializeField] ConfigurableJoint myJoint;
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
    public Action uniqueCollisionReaction;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        gameObject.tag = "Interactable";
        gravity = GetComponent<CustomGravity>();
        //uhhhh = new test();
    }
    
    void FixedUpdate()
    {
        if (!usable)
        {
            return;
        }
        if (queuedMovement && orbiting)
        {
            if (launchPoint)
            {
                transform.position = Vector3.MoveTowards(transform.position, launchPoint.TransformPoint(Vector3.zero),  pullSpeed * Time.fixedDeltaTime);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, attractionPoint.TransformPoint(Vector3.zero),  pullSpeed * Time.fixedDeltaTime);
            }
            
            if (gameObject.transform.localPosition == Vector3.zero)
            {
                Debug.Log("arrived at  destination");
                LockJoint(true);
                queuedMovement = false;
            }
        }
        if (gravity.rb.velocity.magnitude > terminalVelocity && !terminalVelocityReached)
        {
            terminalVelocityReached = true;
            gravity.rb.drag = dragResistance;
        }
        else if (gravity.rb.velocity.magnitude < terminalVelocity/2 && terminalVelocityReached)
        {
            terminalVelocityReached = false;
            gravity.rb.drag = 0f;
        }
    }

    #region InteractionEvents
    //player interactions
    public void ToggleOrbit(bool enable , Transform incomingTarget = null, bool retainGravity = false, GravityField attractor = null)
    {
        if (!usable)
        {
            return;
        }
        if (enable)
        {
            myAttractor = attractor;
            orbiting = true;
            attractionPoint = incomingTarget;
            gameObject.transform.SetParent(attractionPoint);
            queuedMovement = true;
            //gameObject.transform.localPosition = Vector3.zero;
            ProfileAttraction(attractionPoint.GetComponent<Rigidbody>());
            gravity.SetZeroGravity(3f);
        }
        else
        {
            myAttractor = attractor;
            Debug.Log("breaking connection" + gameObject.name);
            orbiting = false;
            attractionPoint = incomingTarget;
            gameObject.transform.SetParent(null);
            if (myJoint)
            {
                myJoint.breakForce = 0f;
                Destroy(myJoint);
            }
            
            if (!retainGravity)
            {
                gravity.RevertGravity(false,0f);
            }
            
        }
    }
    public void ReadyObject(bool enable, Transform pointToGo)
    {
        if (!usable)
        {
            return;
        }
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
    void ProfileAttraction(Rigidbody attractionPoint)
    {
        myJoint = gameObject.AddComponent<ConfigurableJoint>();
        myJoint.connectedBody = attractionPoint;
        myJoint.autoConfigureConnectedAnchor = false;
        myJoint.anchor = Vector3.zero;
        myJoint.linearLimit = new SoftJointLimit{limit = maxDistance};
        LockJoint(false);
        myJoint.breakForce = breakForce;
    }
    void LockJoint(bool toggle)
    {
        if (toggle)
        {

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
    void OnJointBreak(float breakForce)
    {
        Debug.Log("Leaving orbit! " + gameObject.name);
        myAttractor.RemoveItem(gameObject.GetComponent<InteractableObject>());
    }
    void OnCollisionEnter(Collision other)
    {
        if (terminalVelocityReached)
        {
            if(other.gameObject.GetComponent<ICollisionReactable>() != null)
            {
                other.gameObject.GetComponent<ICollisionReactable>().OnHighSpeedCollision(gameObject.GetComponent<InteractableObject>());
            }
            else
            {
                OnHighSpeedCollision();
            }
        }
        if (launched)
        {
            Debug.Log(other.gameObject.GetComponent<ICollisionReactable>());
            if (other.gameObject.GetComponent<ICollisionReactable>() != null)
            {
                other.gameObject.GetComponent<ICollisionReactable>().SoftCollision(gameObject);
            }
        }
        
    }
    

    //reactions
    void BreakItem()
    {
        if (!usable)
        {
            return;
        }
        gameObject.GetComponent<Renderer>().material = brokenState;
        usable = false;
    }

    #endregion
    
    
    public void OnHighSpeedCollision(InteractableObject otherbody = null)
    {
        terminalVelocityReached = false;
        gravity.rb.drag = 0f;
        switch (objectTag)
        {
            case objectType.LightItem:
                BreakItem();
                break;
            case objectType.HeavyItem:
                Debug.Log("clash!");
                break;
            case objectType.StaticItem:
                Debug.Log("nothing!");
                break;
        }
        if (otherbody != null)
        {
            switch (otherbody.objectTag)
            {
                case objectType.LightItem:
                    BreakItem();
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
        uniqueCollisionReaction?.Invoke();
    }

    public string ReturnUniqueName()
    {
        return uniqueName;
    }
}
