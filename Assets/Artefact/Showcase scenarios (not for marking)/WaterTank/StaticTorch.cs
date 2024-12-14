using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticTorch : MonoBehaviour
{
    [SerializeField] GameObject flame;
    [SerializeField] public bool fireLit;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!fireLit && flame.activeInHierarchy)
        {
            flame.SetActive(false);
        }
        else if (fireLit & !flame.activeInHierarchy)
        {
            flame.SetActive(true);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (player.gravityField.enabled && player.gravityField.objectsInOrbit.Count > 0f)
            {
                foreach (InteractableObject potentialTorch in player.gravityField.objectsInOrbit)
                {
                    if (!potentialTorch.GetComponent<Torch>())
                    {
                        continue;
                    }
                    if (fireLit)
                    {
                        if(potentialTorch.GetComponent<Torch>().canBelit)
                        {
                            potentialTorch.GetComponent<Torch>().fireLit = true;
                        }
                    }
                    else
                    {
                        if (potentialTorch.GetComponent<Torch>().fireLit)
                        {
                            fireLit = true;
                        }
                    }
                }
            }
        }
    }
}
