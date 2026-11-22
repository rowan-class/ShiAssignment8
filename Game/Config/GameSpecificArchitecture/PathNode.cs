using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBase;
public class PathNode
{
    //A* 
    public Point loc;
    public bool walkable;
    public PathNode parent;
    public int gCost;
    public int hCost;
    public PathNode(Point loc, bool walkable)
    {
        this.loc = loc;
        this.walkable = walkable;
        this.parent = null;
        this.gCost = int.MaxValue;
        this.hCost = 0;
    }
    public int fCost
    {
        get { return gCost + hCost; }
    }
}
