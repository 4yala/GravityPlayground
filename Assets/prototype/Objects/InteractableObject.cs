using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CustomGravity))]
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
    
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        gameObject.tag = "Interactable";
        gravity = GetComponent<CustomGravity>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        if (orbiting && holdsterTarget != null && launchPoint == null)
        {
            //fix later
            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, holdsterTarget.position, Time.fixedDeltaTime);
        }
        else if (orbiting && holdsterTarget != null && launchPoint != null)
        {
            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, launchPoint.position, Time.fixedDeltaTime);
        }
    }

    #region InteractionEvents

    public void ToggleOrbit(bool enable , Transform incomingTarget)
    {
        if (enable)
        {
            orbiting = true;
            holdsterTarget = incomingTarget;
            gravity.SetZeroGravity(3f);
        }
        else
        {
            //myAttractor.RemoveItem(gameObject.GetComponent<InteractableObject>());
            orbiting = false;
            holdsterTarget = null;
            gravity.RevertGravity(false,0f);
        }
    }
    public void ReadyObject(bool enable, Transform pointToGo)
    {
        if (enable)
        {
            launchPoint = pointToGo;
        }
        else
        {
            launchPoint = null;
        }
    }
    #endregion
}
