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
    [SerializeField] private PlayerControllerDebug playerInScene;
    
    
    // Start is called before the first frame update
    void Start()
    {
        playerInScene = FindObjectOfType<PlayerControllerDebug>();
        //playerInventory = playerInScene.gravityField.objectsInOrbit.ToList();
    }

    // Update is called once per frame
    void Update()
    {
        if (!CompareList())
        {
            UpdateList();
        }
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
}
