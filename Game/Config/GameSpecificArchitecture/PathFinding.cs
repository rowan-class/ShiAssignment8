using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBase;

public static class PathFinding
{
    public static List<PathNode> FindPath(Point startNode, Point endNode, Func<Point, bool> isWalkable, int mapSize) { 
        if(startNode == endNode)
        {
            return new List<PathNode>();
        }
        if (!isWalkable(endNode))
        {
            return new List<PathNode>();
        }
        PathNode[,] nodes = new PathNode[mapSize, mapSize];
        bool[,] visited = new bool[mapSize, mapSize];
        List<PathNode> path = new List<PathNode>();

        for(int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                nodes[x, y] = new PathNode(new Point(x, y), isWalkable(new Point(x, y)));
            }
        }
        PriorityQueue<PathNode, float> openList = new();
        PathNode start = nodes[startNode.X, startNode.Y];

        start.gCost = 0;
        start.hCost =Math.Abs(startNode.X - endNode.X) +Math.Abs(startNode.Y - endNode.Y);

        openList.Enqueue(start, start.fCost);
        bool pathFound = false;

        while (openList.Count > 0 && !pathFound)
        {
            PathNode current = openList.Dequeue();
            if (visited[current.loc.X, current.loc.Y])
            {
                continue;
            }
            visited[current.loc.X, current.loc.Y] = true;

            if (current.loc == endNode)
            {
                pathFound = true;
                PathNode node = current;
                while (node != null)
                {
                    path.Add(node);
                    node = node.parent;
                }
                path.Reverse();
            }
            else
            {

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) != 1)
                        {
                            continue;
                        }

                        Point neighborLoc = new Point(current.loc.X + dx, current.loc.Y + dy);
                        if (neighborLoc.X >= 0 && neighborLoc.X < mapSize && neighborLoc.Y >= 0 && neighborLoc.Y < mapSize)
                        {
                            if (!visited[neighborLoc.X, neighborLoc.Y] && isWalkable(neighborLoc))
                            {
                                PathNode neighbor = nodes[neighborLoc.X, neighborLoc.Y];
                                int tentativeGCost = current.gCost + 1;

                                if (tentativeGCost < neighbor.gCost)
                                {
                                    neighbor.parent = current;
                                    neighbor.gCost = tentativeGCost;
                                    neighbor.hCost = Math.Abs(neighborLoc.X - endNode.X) + Math.Abs(neighborLoc.Y - endNode.Y);
                                    openList.Enqueue(neighbor, neighbor.fCost);
                                }
                            }
                        }
                    }
                }
            }
        }

        return path;
    }
}
