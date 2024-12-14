using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBody : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (player.gravityField.objectsInOrbit.Count > 0f)
            {
                foreach (InteractableObject potentialTorch in player.gravityField.objectsInOrbit)
                {
                    if (!potentialTorch.GetComponent<Torch>())
                    {
                        continue;
                    }
                    potentialTorch.GetComponent<Torch>().wet = true;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (player.gravityField.objectsInOrbit.Count > 0f)
            {
                foreach (InteractableObject potentialTorch in player.gravityField.objectsInOrbit)
                {
                    if (!potentialTorch.GetComponent<Torch>())
                    {
                        continue;
                    }
                    potentialTorch.GetComponent<Torch>().wet = false;
                }
            }
        }
    }
}
