using UnityEngine;

public class Points : MonoBehaviour
{
    public bool isStop;
    public new Transform transform;
    public Collider pointCollider;
    
    public enum State
    {
        Free,
        Busy,
    }
    
    [SerializeField] private State currentState;
    private Renderer objectRenderer;
    private MaterialPropertyBlock propertyBlock;
    
    // Property with auto-update on change
    public State CurrentState
    {
        get { return currentState; }
        set 
        { 
            if (currentState != value)
            {
                currentState = value;
                UpdateMaterialColor();
            }
        }
    }

    void Start()
    {
        // Initialize components
        transform = GetComponent<Transform>();
        pointCollider = GetComponent<Collider>();
        objectRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        
        // Set initial state and update color
        CurrentState = State.Free;
        UpdateMaterialColor();
    }
    
    private void UpdateMaterialColor()
    {
        if (objectRenderer == null) return;
        
        objectRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", currentState == State.Free ? Color.green : Color.red);
        objectRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnTriggerEnter(Collider other)
    {
        CurrentState = State.Busy;
        UpdateMaterialColor();
    }
    
    private void OnTriggerExit(Collider other)
    {
        CurrentState = State.Free;
        UpdateMaterialColor();
    }
}