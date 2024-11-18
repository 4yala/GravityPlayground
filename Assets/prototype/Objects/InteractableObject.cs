using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(CustomGravity))]
//[RequireComponent(typeof(SpringJoint))]
public class InteractableObject : MonoBehaviour
{
    #region Variables
    [SerializeField] public objectType objectTag;
    [SerializeField] public bool orbiting;
    [SerializeField] public Transform holdsterTarget;
    [SerializeField] public Transform launchPoint;
    [SerializeField] public GravityField myAttractor;
    [SerializeField] public CustomGravity gravity;
    [SerializeField] public bool launched;
    [SerializeField] bool queuedMovement;

    [Header("Attraction settings")] 
    [SerializeField] ConfigurableJoint myJoint;
    //default values
    [SerializeField] float min = 1f, max = 2f, breakForce = 100f, pullSpeed = 5f;
    
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
                transform.position = Vector3.MoveTowards(transform.position, holdsterTarget.TransformPoint(Vector3.zero),  pullSpeed * Time.fixedDeltaTime);
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
            holdsterTarget = incomingTarget;
            gameObject.transform.SetParent(holdsterTarget);
            queuedMovement = true;
            //gameObject.transform.localPosition = Vector3.zero;
            ProfileAttraction(holdsterTarget.GetComponent<Rigidbody>());
            gravity.SetZeroGravity(3f);
        }
        else
        {
            myAttractor = attractor;
            Debug.Log("breaking connection" + gameObject.name);
            orbiting = false;
            holdsterTarget = incomingTarget;
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
            if (holdsterTarget)
            {
                myJoint.connectedBody = holdsterTarget.GetComponent<Rigidbody>();
                LockJoint(false);
                gameObject.transform.SetParent(holdsterTarget);
                queuedMovement = true;
            }

            else
            {
                gameObject.transform.SetParent(null);
            }
            Debug.Log("missing function");
            //lerp position back to a designated place
            
        }
    }
    void ProfileAttraction(Rigidbody attractionPoint)
    {
        
        myJoint = gameObject.AddComponent<ConfigurableJoint>();
        myJoint.connectedBody = attractionPoint;
        myJoint.autoConfigureConnectedAnchor = false;
        myJoint.anchor = Vector3.zero;
        myJoint.linearLimit = new SoftJointLimit{limit = max};
        LockJoint(false);
        
        /*
        myJoint.xMotion = ConfigurableJointMotion.Limited;
        myJoint.yMotion = ConfigurableJointMotion.Limited;
        myJoint.zMotion = ConfigurableJointMotion.Limited;
        */
        //myJoint.maxDistance = max;
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
    private void OnJointBreak(float breakForce)
    {
        Debug.Log("Leaving orbit! " + gameObject.name);
        myAttractor.RemoveItem(gameObject.GetComponent<InteractableObject>());
    }

    #endregion
}
