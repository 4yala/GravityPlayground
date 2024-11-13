using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Attraction settings")] 
    [SerializeField] HingeJoint myJoint;
    [SerializeField] float min, max, breakForce;
    
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        gameObject.tag = "Interactable";
        gravity = GetComponent<CustomGravity>();
    }
    

    void FixedUpdate()
    {
        if (orbiting && holdsterTarget != null && launchPoint == null)
        {
            //fix later
            //gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, holdsterTarget.position, Time.fixedDeltaTime);
        }
        else if (orbiting && holdsterTarget != null && launchPoint != null)
        {
            //gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, launchPoint.position, Time.fixedDeltaTime);
            //Debug.Log("readying");
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
            ProfileAttraction(holdsterTarget.GetComponent<Rigidbody>());
            gravity.SetZeroGravity(0f);
        }
        else
        {
            myAttractor = attractor;
            Debug.Log("breaking connection" + gameObject.name);
            orbiting = false;
            holdsterTarget = incomingTarget;
            if (myJoint)
            {
                myJoint.breakForce = 0f;
                Destroy(myJoint);
                myJoint = null;
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
            launchPoint = pointToGo;
            myJoint.connectedBody = launchPoint.GetComponent<Rigidbody>();
        }
        else
        {
            launchPoint = null;
            if (holdsterTarget)
            {
                myJoint.connectedBody = holdsterTarget.GetComponent<Rigidbody>();
            }
            Debug.Log("missing function");
            //lerp position back to a designated place
            
        }
    }
    void ProfileAttraction(Rigidbody attractionPoint)
    {
        
        myJoint = gameObject.AddComponent<HingeJoint>();
        myJoint.connectedBody = attractionPoint;
        myJoint.autoConfigureConnectedAnchor = false;
        myJoint.anchor = Vector3.zero;
        //myJoint.minDistance = min;
        //myJoint.maxDistance = max;
        myJoint.breakForce = breakForce;
    }
    private void OnJointBreak(float breakForce)
    {
        Debug.Log("Leaving orbit!" + gameObject.name);
        myAttractor.RemoveItem(gameObject.GetComponent<InteractableObject>());
    }

    #endregion
}
