using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class WallWithHealthDebug : MonoBehaviour
{
    [SerializeField] HighSpeedReactor myParent;
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] int health = 5;
    [SerializeField] int maxHealth = 5;
    [SerializeField] float reSpawn;
    
    void Awake()
    {
        health = maxHealth;
        myParent = gameObject.GetComponentInParent<HighSpeedReactor>();
        myParent.slamSequence += HarmWall;
        healthText.text = "Health: " + health;
    }

    void HarmWall()
    {
        health--;
        healthText.text = "Health: " + health;
        if (health == 0)
        {
            StartCoroutine(Respawn());
        }
    }

    IEnumerator Respawn()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = false;
        gameObject.GetComponent<BoxCollider>().enabled = false;
        healthText.gameObject.SetActive(false);
        yield return new WaitForSeconds(reSpawn);
        gameObject.GetComponent<MeshRenderer>().enabled = true;
        gameObject.GetComponent<BoxCollider>().enabled = true;
        healthText.gameObject.SetActive(true);
        health = maxHealth;
        healthText.text = "Health: " + health;

    }
}
