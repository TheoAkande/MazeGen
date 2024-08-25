using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;


public class Controller : MonoBehaviour
{
    public int scale = 10;
    public int nodeSize = 8;
    public int offset;
    public GameObject nodePrefab;    
    public Transform nodesParent; 

    public GameObject pathPrefab;  
    public Transform pathsParent;  

    public int gridSizeX = 20;     
    public int gridSizeY = 20;

    private Node[,] nodes;
    private List<Path> paths;

    public int finalPathLength = -1;
    private Node pathEnd;
    private Color pathColour = Color.red;
    private bool solved = false;
    private bool rand = true;

    private Node start;
    private Node end;
    System.Random random = new System.Random();

    void Start()
    {
        init(PrimsMaze);
    }

    delegate void MazeGenFunc();

    void init(MazeGenFunc genFunc)
    {
        DestroyAllChildren();
        this.solved = false;
        this.nodes = new Node[gridSizeY, gridSizeX];
        this.paths = new List<Path>();
        offset = (scale - nodeSize) / 2;
        InstantiateNodes();
        this.start = this.nodes[random.Next(0, gridSizeY - 1), 0];
        this.pathEnd = this.start;
        this.end = this.nodes[random.Next(0, gridSizeY - 1), gridSizeX - 1];
        this.start.ChangeColour(Color.red);
        this.end.ChangeColour(Color.green);
        connect(new Vector2Int(0, 0), new Vector2Int(1, 0));
        connect(new Vector2Int(0, 0), new Vector2Int(0, 1));
        genFunc();
    }

    void DestroyAllChildren()
    {
        int childCount = transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    Vector3 GridToScene(Vector3 origin)
    {
        return new Vector3(origin.x * scale + offset + nodeSize / 2, origin.y * scale + offset + nodeSize / 2, origin.z);
    }

    Node GetNode(GameObject o)
    {
        return o.GetComponent<Node>();
    }

    void InstantiateNodes()
    {
        for (int i = 0; i < gridSizeY; i++)
        {
            for (int j = 0; j < gridSizeX; j++)
            {
                Vector3 spawnPosition = GridToScene(new Vector3(j, i, 0));
                Node newNode = Instantiate(nodePrefab, spawnPosition, Quaternion.identity, nodesParent).GetComponent<Node>();
                newNode.Initialise(new Vector2Int(j, i), GetComponent<Controller>());
                this.nodes[i, j] = newNode;
            }
        }
    }

    bool validate(Vector2Int pos)
    {
        return 0 <= pos.x && pos.x < this.gridSizeX && 0 <= pos.y && pos.y < this.gridSizeY;
    }

    bool validateConnection(Vector2Int pos1, Vector2Int pos2)
    {
        return (Math.Abs(pos1.x - pos2.x) == 0 && Math.Abs(pos1.y - pos2.y) == 1)
            || (Math.Abs(pos1.x - pos2.x) == 1 && Math.Abs(pos1.y - pos2.y) == 0);
    }

    delegate bool CompFunc<T, S>(T x, S y);

    bool PLVCmp(Path p, List<Vector2Int> vs)
    {
        return (p.pos1 == vs[0] && p.pos2 == vs[1]) ||
                (p.pos2 == vs[0] && p.pos1 == vs[1]);
    }

    int find<T, S>(List<T> l, S s, CompFunc<T, S> cf)
    {
        int ret = 0;
        foreach (T t in l)
        {
            if (cf(t, s))
            {
                return ret; 
            }
            ret++;
        }
        return -1;
    }

    void disconnect(Vector2Int pos1, Vector2Int pos2)
    {
        disconnectHelp(pos1, pos2);
        disconnectHelp(pos2, pos1);
    }

    void disconnectHelp(Vector2Int pos1, Vector2Int pos2)
    {
        if (!(validate(pos1) && validate(pos2)) || !validateConnection(pos1, pos2))
        {
            UnityEngine.Debug.Log("Invalid");
            return;
        }
        Node n1 = this.nodes[pos1.y, pos1.x];
        Node n2 = this.nodes[pos2.y, pos2.x];
        if (n1.neighbours.Contains(n2))
        {
            n1.neighbours.Remove(n2);
        }
        if (n2.neighbours.Contains(n1))
        {
            n2.neighbours.Remove(n1);
        }
        int i = find(paths, new List<Vector2Int>() { pos1, pos2 }, PLVCmp);
        if (i != -1)
        {
            Path p = this.paths[i];
            this.paths.RemoveAt(i);
            p.DestroySelf();
        }
        
    }

        void connect(Vector2Int pos1, Vector2Int pos2)
    {
        if (!(validate(pos1) && validate(pos2)) || !validateConnection(pos1, pos2))
        {
            Console.WriteLine("Invalid");
            return;
        }
        Node n1 = this.nodes[pos1.y, pos1.x];
        Node n2 = this.nodes[pos2.y, pos2.x];
        if (!n1.neighbours.Contains(n2))
        {
            n1.neighbours.Add(n2);
            n2.connected = true;
        }
        if (!n2.neighbours.Contains(n1))
        {
            n2.neighbours.Add(n1);
            n1.connected = true;
        }
        int minx = Math.Min(n1.x, n2.x);
        int maxx = Math.Max(n1.x, n2.x);
        int miny = Math.Min(n1.y, n2.y);
        int maxy = Math.Max(n1.y, n2.y);

        Vector3 spawnPosition;
        bool sideways = false;

        if (minx == maxx)
        {
            spawnPosition = new Vector3(minx * scale + offset + nodeSize / 2, miny * scale + 2 * offset + nodeSize, 0);
        } else
        {
            spawnPosition = new Vector3(minx * scale + 2 * offset + nodeSize, miny * scale + offset + nodeSize / 2, 0);
            sideways = true;
        }

        Path newPath = Instantiate(pathPrefab, spawnPosition, Quaternion.identity, pathsParent).GetComponent<Path>();
        newPath.Initialise(pos1, pos2);
        if (sideways)
        {
            newPath.RotateBy90Degrees();
        }
        this.paths.Add(newPath);
    }

    Vector2Int GetNewPos(Vector2Int current, int direction)
    {
        switch (direction)
        {
            case 0:
                return new Vector2Int(current.x - 1, current.y);
            case 1:
                return new Vector2Int(current.x + 1, current.y);
            case 2:
                return new Vector2Int(current.x, current.y - 1);
            case 3:
                return new Vector2Int(current.x, current.y + 1);
            default:
                return new Vector2Int(-10, -10);
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    List<Node> GetNeighbours(Node n)
    {
        List<Node> ret = new List<Node>();
        List<Vector2Int> poss = new List<Vector2Int>
        {
            new Vector2Int(n.x + 1, n.y),
            new Vector2Int(n.x - 1, n.y),
            new Vector2Int(n.x, n.y + 1),
            new Vector2Int(n.x, n.y - 1)
        };
        foreach (Vector2Int p in poss)
        {
            if (validate(p))
            {
                Node nei = this.nodes[p.y, p.x];
                if (!nei.connected)
                {
                    ret.Add(nei);
                }
            }    
        }
        return ret;
    }

    void ConnectAll()
    {
        for (int i = 0; i < gridSizeX; i++)
        {
            for (int j = 0; j < gridSizeY; j++)
            {
                if (i < gridSizeX - 1)
                {
                    connect(new Vector2Int(i, j), new Vector2Int(i + 1, j));
                }
                if (j < gridSizeY - 1)
                {
                    connect(new Vector2Int(i, j), new Vector2Int(i, j + 1));
                }
            }
        }
    }

    void VSplit(Vector2Int tl, Vector2Int br, int x)
    {
        for (int i = br.y; i <= tl.y; i++)
        {
            disconnect(new Vector2Int(x, i), new Vector2Int(x + 1, i));
        }
    }

    void HSplit(Vector2Int tl, Vector2Int br, int y)
    {
        for (int i = tl.x; i <= br.x; i++)
        {
            disconnect(new Vector2Int(i, y), new Vector2Int(i, y + 1));
        }
    }

    void RecDiv(Vector2Int topLeft, Vector2Int bottomRight)
    {
        int w = bottomRight.x - topLeft.x;
        int h = topLeft.y - bottomRight.y;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        int xsplit;
        int ysplit;

        if (rand)
        {
            xsplit = random.Next(topLeft.x, bottomRight.x - 1);
            ysplit = random.Next(bottomRight.y, topLeft.y - 1);
        } else
        {
            xsplit = (topLeft.x + bottomRight.x) / 2;
            ysplit = (bottomRight.y + topLeft.y) / 2;
        }

        VSplit(topLeft, bottomRight, xsplit);
        HSplit(topLeft, bottomRight, ysplit);

        List<int> randoms = new List<int>() {
            random.Next(topLeft.x, xsplit),
            random.Next(xsplit + 1, bottomRight.x),
            random.Next(bottomRight.y, ysplit),
            random.Next(ysplit + 1, topLeft.y)
        };

        List<List<Vector2Int>> doors = new List<List<Vector2Int>>()
        {
            new List<Vector2Int>() {new Vector2Int(randoms[0], ysplit), new Vector2Int(randoms[0], ysplit + 1)},
            new List<Vector2Int>() {new Vector2Int(randoms[1], ysplit), new Vector2Int(randoms[1], ysplit + 1)},
            new List<Vector2Int>() {new Vector2Int(xsplit, randoms[2]), new Vector2Int(xsplit + 1, randoms[2])},
            new List<Vector2Int>() {new Vector2Int(xsplit, randoms[3]), new Vector2Int(xsplit + 1, randoms[3])}
        };

        Shuffle(doors);
        for (int i = 0; i < 3; i++)
        {
            connect(doors[i][0], doors[i][1]);
        }

        RecDiv(topLeft, new Vector2Int(xsplit, ysplit + 1));
        RecDiv(new Vector2Int(xsplit + 1, topLeft.y), new Vector2Int(bottomRight.x, ysplit + 1));
        RecDiv(new Vector2Int(topLeft.x, ysplit), new Vector2Int(xsplit, bottomRight.y));
        RecDiv(new Vector2Int(xsplit + 1, ysplit), bottomRight);
    }

    void RecBackHelp(Node current)
    {
        List<int> directions = new List<int> { 0, 1, 2, 3 };
        Shuffle(directions);
        foreach (int direction in directions)
        {
            Vector2Int newPos = GetNewPos(current.pos, direction);

            if (validate(newPos) && !(this.nodes[newPos.y, newPos.x].connected))
            {
                Node n = this.nodes[newPos.y, newPos.x];
                connect(current.pos, n.pos);
                RecBackHelp(n);
            }

        }
    }

    void RecBackMaze()
    {
        RecBackHelp(this.start);
    }

    void RecDivMaze()
    {
        ConnectAll();
        RecDiv(new Vector2Int(0, gridSizeY - 1), new Vector2Int(gridSizeX - 1, 0));
    }

    void PrimsMaze()
    {
        GameObject temp = Instantiate(nodePrefab, new Vector3(-10, -10, 0), Quaternion.identity, nodesParent);
        Node startPrev = GetNode(temp);
        Node cur = this.start;
        cur.primPrev = startPrev;
        Dictionary<String, int> fringeDict = new Dictionary<string, int>();
        for (int i = 0; i < this.gridSizeX; i++)
        {
            for (int j = 0; j < this.gridSizeY; j++)
            {
                fringeDict[i.ToString() + "+" + j.ToString()] = 0;
            }
        }
        List<List<Node>> fringe = new List<List<Node>> { new List<Node> { cur, startPrev } };
        fringeDict[cur.x.ToString() + "+" + cur.y.ToString()] = 1;

        while (fringe.Count > 0)
        {
            int randomIndex = random.Next(0, fringe.Count);
            List<Node> popped = fringe[randomIndex];
            fringe.RemoveAt(randomIndex);
            popped[0].primPrev = popped[1];
            if (popped[1] != startPrev)
            {
                connect(popped[0].pos, popped[1].pos);
            }
            cur = popped[0];
            fringeDict[cur.x.ToString() + "+" + cur.y.ToString()] = 0;
            List<Node> neighbours = GetNeighbours(cur);
            foreach (Node n in neighbours)
            {
                if (fringeDict[n.x.ToString() + "+" + n.y.ToString()] == 0)
                {
                    fringe.Add(new List<Node> { n, cur });
                    fringeDict[n.x.ToString() + "+" + n.y.ToString()] = 1;
                } else if (random.Next(0, 2) == 0)
                {
                    for (int i = 0; i < fringe.Count; i++)
                    {
                        if (fringe[i][0].pos == n.pos)
                        {
                            fringe[i][1] = cur;
                        }
                    }
                }
            }

        }
    }

    bool FringeToNodeComp(FringeNode n1, Node n2)
    {
        return n1.node == n2;
    }

    void RecColour(FringeNode n, Color c)
    {
        if (n is not null)
        {
            n.node.ChangeColour(c);
            RecColour(n.prev, c);
        }
        return;
    }

    void Solve()
    {
        FringeNode last;
        Node e = this.end;
        List<FringeNode> fringe = new List<FringeNode>()
        {
            new FringeNode(this.start, null, 0, this.start.h(e.pos))
        };
        List<FringeNode> closed = new List<FringeNode>()
        {
            new FringeNode(e, null, 0, 0)
        };
        while (fringe.Count > 0)
        {
            IEnumerable<FringeNode> sortedFringe = fringe.OrderByDescending(x => x.score);
            fringe = sortedFringe.ToList();
            FringeNode cur = fringe[fringe.Count - 1];
            fringe.RemoveAt(fringe.Count - 1);
            closed.Add(cur);
            foreach (Node n in cur.node.neighbours)
            {
                if (n == e)
                {
                    finalPathLength = cur.dist + 1;
                    int i = find(closed, e, FringeToNodeComp);
                    last = closed[i];
                    last.prev = cur;
                    RecColour(last, Color.blue);
                    solved = true;
                    return;
                }
                if (find(closed, n, FringeToNodeComp) == -1)
                {
                    int i = find(fringe, n, FringeToNodeComp);
                    if (i != -1)
                    {
                        FringeNode fn = fringe[i];
                        if (fn.dist > cur.dist + 1)
                        {
                            fn.dist = cur.dist + 1;
                            fn.score = fn.dist + fn.node.h(e.pos);
                            fn.prev = cur;
                        }
                    } else
                    {
                        fringe.Add(new FringeNode(n, cur, cur.dist + 1, cur.dist + 1 + n.h(e.pos)));
                    }
                }
            }
        }
        return;
    }

    void RecRemove(Node start, Node term)
    {
        while (start != term)
        {
            start.ChangeColour(Color.white);
            foreach (Node n in start.neighbours)
            {
                if (n.colour != Color.white)
                {
                    start = n;
                }
            }
        }
    }

    public void Toggle(Node square)
    {
        if (solved)
        {
            return;
        }
        if (square.colour == this.pathColour)
        {
            RecRemove(this.pathEnd, square);
            pathEnd = square;
        } else
        {
            GameObject temp = Instantiate(nodePrefab, new Vector3(-10, -10, 0), Quaternion.identity, nodesParent);
            Node term = GetNode(temp);
            Node v = term;
            foreach (Node n in square.neighbours)
            {
                if (n.colour != Color.white)
                {
                    v = n;
                }
            }
            if (v != term)
            {
                if (square == this.end)
                {
                    init(PrimsMaze);
                }
                RecRemove(this.pathEnd, v);
                square.ChangeColour(this.pathColour);
                this.pathEnd = square;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Path p in paths)
        {
            p.ChangeColour(nodes[p.pos1.y, p.pos1.x].colour);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Solve();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            init(RecBackMaze);
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            rand = false;
            init(RecDivMaze);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            rand = true;
            init(RecDivMaze);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            init(PrimsMaze);
        }
    }
}
