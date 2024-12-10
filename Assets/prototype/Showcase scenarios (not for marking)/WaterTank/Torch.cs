using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] GameObject flame;
    [SerializeField] public bool fireLit;
    [SerializeField] public bool wet;
    [SerializeField] InteractableObject myInteractionComponent;
    [SerializeField] public bool canBelit;
     
    // Start is called before the first frame update
    private void Start()
    {
        myInteractionComponent = gameObject.GetComponent<InteractableObject>();
    }

    // Update is called once per frame
    void Update()
    {
        //update fire
        if (!fireLit && flame.activeInHierarchy)
        {
            flame.SetActive(false);
        }
        else if (fireLit & !flame.activeInHierarchy)
        {
            flame.SetActive(true);
        }
        
        //update status
        canBelit = (!wet && myInteractionComponent.orbiting);
        if (!canBelit && fireLit)
        {
            fireLit = false;
        }
        if(fireLit && myInteractionComponent.myAttractor.owner.diving)
        {
            fireLit = false;
        }
    }
}
