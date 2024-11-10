using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
[RequireComponent(typeof(CustomGravity))]
public class InteractableObject : MonoBehaviour
{
    #region Variables
    [SerializeField] public objectType objectTag;
    [SerializeField] public bool orbiting;
    [SerializeField] public Transform target;
    [SerializeField] public ConstantForce myGravity;
    [SerializeField] public float gravityForceUnit;
    [SerializeField] public GravityField myAttractor;
    [SerializeField] public CustomGravity gravity;
    
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
        if (orbiting && target != null)
        {
            //fix later
            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, target.position, Time.fixedDeltaTime);
        }
    }

    #region InteractionEvents

    public void ToggleOrbit(bool enable , Transform incomingTarget)
    {
        if (enable)
        {
            orbiting = true;
            target = incomingTarget;
            gravity.SetZeroGravity(3f);
        }
        else
        {
            //myAttractor.RemoveItem(gameObject.GetComponent<InteractableObject>());
            orbiting = false;
            target = null;
            gravity.RevertGravity(false,0f);
            //myGravity.force = new Vector3(0, -gravityForceUnit, 0);
        }
    }
    #endregion
}
