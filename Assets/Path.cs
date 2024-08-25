using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Path : MonoBehaviour
{

    public Vector2Int pos1;
    public Vector2Int pos2;

    public void Initialise(Vector2Int pos1, Vector2Int pos2)
    {
        this.pos1 = pos1;
        this.pos2 = pos2;
    }

    public void RotateBy90Degrees()
    {
        transform.Rotate(Vector3.forward, 90f); // Rotate around the Z-axis by 90 degrees
    }

    public void ChangeColour(Color newColour)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = newColour;
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
