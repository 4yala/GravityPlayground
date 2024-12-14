using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class UIDebug : MonoBehaviour
{
    [SerializeField] private List<InteractableObject> playerInventory;
    [SerializeField] private TextMeshProUGUI playerInventoryText;
    [SerializeField] private InteractableObject objectAimed;
    [SerializeField] private TextMeshProUGUI objectAimedContent;
    [SerializeField] private PlayerController playerInScene;
    
    
    // Start is called before the first frame update
    void Start()
    {
        playerInScene = FindObjectOfType<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!CompareList())
        {
            UpdateList();
        }

        if (!CompareHeld())
        {
            UpdateHeldItem();
        }
    }

    bool CompareHeld()
    {
        return objectAimed == playerInScene.gravityField.objectToShoot;
    }
    bool CompareList()
    {
        bool isListMatched = playerInventory.SequenceEqual(playerInScene.gravityField.objectsInOrbit);
        return isListMatched;
    }
    void UpdateList()
    {
        playerInventory = playerInScene.gravityField.objectsInOrbit.ToList();
        playerInventoryText.text = "";
        foreach (InteractableObject invObject in playerInventory)
        {
            playerInventoryText.text += invObject.ReturnUniqueName()+ "\n";
        }
    }

    void UpdateHeldItem()
    {
        if (playerInScene.gravityField.objectToShoot != null)
        {
            objectAimed = playerInScene.gravityField.objectToShoot;
            objectAimedContent.text = "Tag: " + objectAimed.ReturnUniqueName() +"\n" 
                + "Type: " + objectAimed.objectTag +"\n"
                + "Gravitational force: "+ objectAimed.gravity.gravityForceUnit + "\n"
                + "Mass: " +objectAimed.gravity.rb.mass + "\n"
                + "Maximum velocity: " +objectAimed.terminalVelocity+ "\n"
                ;
        }
        else
        {
            objectAimed = null;
            objectAimedContent.text = "";
        }
    }
}
