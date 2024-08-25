using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;

public class Node : MonoBehaviour
{

    public Vector2Int pos;
    public bool connected = false;
    public List<Node> neighbours = new List<Node>();
    public int x;
    public int y;
    public Color colour = Color.white;
    public Node primPrev;
    public Controller maze;

    public void Initialise(Vector2Int position, Controller c)
    {
        this.pos = position;
        x = pos.x;
        y = pos.y;
        primPrev = null;
        this.maze = c;
    }

    public void ChangeColour(Color newColour)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = newColour;
            this.colour = newColour;
        }
    }

    public double h(Vector2Int pos)
    {
        double x_diff = (double) this.x - pos.x;
        double y_diff = (double) this.y - pos.y;
        return Math.Sqrt(x_diff * x_diff + y_diff * y_diff);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                maze.Toggle(GetComponent<Node>());
            }
        }
    }
}
