using System.Collections.Generic;
using UnityEngine;
public static class AStar
{
    public static Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end)
    {
        Vector3 line_direction = line_end - line_start;
        float line_length = line_direction.magnitude;
        line_direction.Normalize();
        float project_length = Mathf.Clamp(Vector3.Dot(point - line_start, line_direction), 0f, line_length);
        return line_start + line_direction * project_length;
    }

    public static List<Node> FindPath(Vector3 start, Vector3 target, List<Node> nodes)
    {
        Node startNode = FindClosestNode(start, nodes);
        Node targetNode = FindClosestNode(target, nodes);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        Dictionary<Node, int> gScore = new Dictionary<Node, int>();
        Dictionary<Node, int> fScore = new Dictionary<Node, int>();

        openSet.Add(startNode);
        gScore[startNode] = 0;
        fScore[startNode] = GetDistance(startNode, targetNode);

        while (openSet.Count > 0)
        {
            Node currentNode = GetLowestFScore(openSet, fScore);

            if (currentNode == targetNode)
            {
                return ReconstructPath(cameFrom, currentNode);
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (Node neighbor in currentNode.Neighbors)
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                int tentativeGScore = gScore[currentNode] + GetDistance(currentNode, neighbor);

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= gScore[neighbor])
                {
                    continue;
                }

                cameFrom[neighbor] = currentNode;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + GetDistance(neighbor, targetNode);
            }
        }

        return null;
    }

    private static Node FindClosestNode(Vector3 position, List<Node> nodes)
    {
        Node closestNode = null;
        float closestDistance = float.MaxValue;

        foreach (Node node in nodes)
        {
            float distance = Vector3.Distance(position, node.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = node;
            }
        }

        return closestNode;
    }

    private static int GetDistance(Node nodeA, Node nodeB)
    {
        return Mathf.RoundToInt(Vector3.Distance(nodeA.transform.position, nodeB.transform.position));
    }

    private static Node GetLowestFScore(List<Node> openSet, Dictionary<Node, int> fScore)
    {
        Node lowestNode = openSet[0];
        int lowestScore = fScore[lowestNode];

        for (int i = 1; i < openSet.Count; i++)
        {
            if (fScore[openSet[i]] < lowestScore)
            {
                lowestNode = openSet[i];
                lowestScore = fScore[lowestNode];
            }
        }

        return lowestNode;
    }

    private static List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node currentNode)
    {
        List<Node> path = new List<Node>();
        path.Add(currentNode);

        while (cameFrom.ContainsKey(currentNode))
        {
            currentNode = cameFrom[currentNode];
            path.Add(currentNode);
        }

        path.Reverse();
        return path;
    }
}