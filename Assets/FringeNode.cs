using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FringeNode
{

    public Node node;
    public FringeNode prev;
    public int dist;
    public double score;

    public FringeNode(Node node, FringeNode prev, int dist, double score)
    {
        this.node = node;
        this.prev = prev;
        this.dist = dist;
        this.score = score;
    }

}
