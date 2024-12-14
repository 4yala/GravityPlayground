using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyReactor : MonoBehaviour, ICollisionReactable
{
    [SerializeField] private ObjectDemander myParent;
    [SerializeField] private KeyAssembler myManager;
    [SerializeField] public bool solved;

    private void Awake()
    {
        myParent = gameObject.GetComponent<ObjectDemander>();
        myManager = FindObjectOfType<KeyAssembler>();
        myManager.myLocks.Add(gameObject.GetComponent<KeyReactor>());
        myParent.uniqueActionToExecute += UnlockSlot;
    }
    
    void UnlockSlot()
    {
        solved = true;
        myManager.RefreshLocks();
    }
}
