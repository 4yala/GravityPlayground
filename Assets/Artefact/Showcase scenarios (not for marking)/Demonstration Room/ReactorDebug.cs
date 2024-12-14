using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ReactorDebug : MonoBehaviour
{
    [SerializeField] ObjectDemander myParent;
    [SerializeField] GameObject myLight;
    [SerializeField] Material greenLight;
    [SerializeField] Material redLight;
    [SerializeField] bool turnedOn;
    [SerializeField] int switched;
    [SerializeField] TextMeshProUGUI switchSign;
    [SerializeField] TextMeshProUGUI demandText;

    void Awake()
    {
        myParent = gameObject.GetComponentInParent<ObjectDemander>();
        myParent.uniqueActionToExecute += GreenLight;
        demandText.text = myParent.objectRequired;
    }

    void GreenLight()
    {
        turnedOn = !turnedOn;
        switched++;
        switchSign.text = "Switched - " + switched.ToString();
        if (turnedOn)
        {
            myLight.GetComponent<Renderer>().material = greenLight;
        }
        else
        {
            myLight.GetComponent<Renderer>().material = redLight;
        }
        
    }
}
