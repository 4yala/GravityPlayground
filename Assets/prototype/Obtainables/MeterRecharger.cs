using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MeterRecharger : MonoBehaviour
{
    [SerializeField] float amount;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerControllerDebug>())
        {
            PlayerControllerDebug obtainer = other.gameObject.GetComponent<PlayerControllerDebug>();
            
            obtainer.currentGravityMeter += amount;
            obtainer.gravityMeter.fillAmount = obtainer.currentGravityMeter / obtainer.myProfile.maxGravityMeter;
            gameObject.SetActive(false);
        }
    }
}
