using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class KeyAssembler : MonoBehaviour
{
    [SerializeField] public List<KeyReactor> myLocks;
    [SerializeField] GameObject StaminaCharges;
    // Start is called before the first frame update
    void Start()
    {
        StaminaCharges.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RefreshLocks()
    {
        bool allSolved = true;
        foreach (KeyReactor locks in myLocks)
        {
            if (!locks.solved)
            {
                allSolved = false;
                return;
            }
            
        }
        if (allSolved)
        {
            Debug.Log("Finished");
            StaminaCharges.SetActive(true);
        }
    }
}
