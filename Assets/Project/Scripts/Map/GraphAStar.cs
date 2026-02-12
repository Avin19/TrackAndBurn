using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GraphAStar
{
    public static List<AINode> FindPath(AINode start, AINode goal)
    {
        if (start == null || goal == null) return null;

        var open = new List<AINode>();
        var closed = new HashSet<AINode>();

        open.Add(start);
        start.gCost = 0;
        start.fCost = start.DistanceTo(goal);
        start.parent = null;

        while (open.Count > 0)
        {
            var current = open.OrderBy(n => n.fCost).First();

            if (current == goal)
                return ReconstructPath(goal);

            open.Remove(current);
            closed.Add(current);

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor == null || !neighbor.isActive || closed.Contains(neighbor))
                    continue;

                float tentativeG = current.gCost + current.DistanceTo(neighbor) + neighbor.traversalCost;

                if (!open.Contains(neighbor))
                {
                    neighbor.parent = current;
                    neighbor.gCost = tentativeG;
                    neighbor.fCost = tentativeG + neighbor.DistanceTo(goal);
                    // open.Add(neighbor);
                }
                else if (tentativeG < neighbor.gCost)
                {
                    neighbor.parent = current;
                    neighbor.gCost = tentativeG;
                    neighbor.fCost = tentativeG + neighbor.DistanceTo(goal);
                }
            }
        }

        return null;
    }

    private static List<AINode> ReconstructPath(AINode goal)
    {
        var path = new List<AINode>();
        AINode current = goal;
        while (current != null)
        {
            path.Add(current);
            //  current = current.parent;
        }
        path.Reverse();
        return path;
    }
}
