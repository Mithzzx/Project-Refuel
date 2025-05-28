using System;
using EPOOutline;
using UnityEngine;

public class PlayerIntract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private LayerMask interactableLayer;
    private Outlinable highlight;
    
    
    [Header("Settings")]
    [SerializeField] private float interactionRadius = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        input = GetComponent<InputHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        Collider[] colliderArray = Physics.OverlapSphere(transform.position, 1f, interactableLayer);
        foreach (Collider collider in colliderArray)
        {
            highlight = collider.gameObject.GetComponent<Outlinable>();
            if (highlight)
            {
                highlight.enabled = true;
            }
        }
        
    }
}
