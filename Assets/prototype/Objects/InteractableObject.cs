using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(CustomGravity))]
public class InteractableObject : MonoBehaviour
{
    #region Variables
    [Header("Settings")]
    [Tooltip("The type of object it is")]
    [SerializeField] public objectType objectTag;
    [Tooltip("Custom gravity component")]
    [SerializeField] public CustomGravity gravity;
    
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
    [SerializeField] public bool orbiting;
    [SerializeField] public bool launched;
    [Tooltip("For precise translation between points")]
    [SerializeField] bool queuedMovement;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        gameObject.tag = "Interactable";
        gravity = GetComponent<CustomGravity>();
    }
    
    void FixedUpdate()
    {
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
    }

    #region InteractionEvents

    public void ToggleOrbit(bool enable , Transform incomingTarget = null, bool retainGravity = false, GravityField attractor = null)
    {
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
    void OnJointBreak(float breakForce)
    {
        Debug.Log("Leaving orbit! " + gameObject.name);
        myAttractor.RemoveItem(gameObject.GetComponent<InteractableObject>());
    }

    #endregion
}
