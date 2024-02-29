using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
using System.Linq;
using System;

public class Node : MonoBehaviour
{
#if UNITY_EDITOR
    [ListDrawerSettings(
        CustomAddFunction = nameof(AddNodeNeighbour),
        HideAddButton = false,
        CustomRemoveElementFunction = nameof(OnNeighbourRemoved))]
#endif
    [FormerlySerializedAs("neighbors")] public List<Node> Neighbors = new();

    private void OnValidate()
    {
        Neighbors.Remove(this);
        Neighbors = Neighbors.Distinct().ToList();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (Node neighbor in Neighbors)
        {
            if (!neighbor.Neighbors.Contains(this))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
                continue;
            }
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }

#if UNITY_EDITOR

    private void OnNeighbourRemoved(Node n)
    {
        Neighbors.Remove(n);
        if (n != null) n.Neighbors.Remove(this);
    }
    private void AddNodeNeighbour(Node n)
    {
        if (Neighbors.Contains(n)) return;

        Neighbors.Add(n);

        if (n.Neighbors.Contains(this)) return;

        n.Neighbors.Add(this);
    }

    [Button]
    private void ConnectOnBothSides()
    {
        foreach (var node in Neighbors)
        {
            if (node.Neighbors.Contains(this)) continue;

            node.Neighbors.Add(this);
        }
    }

#endif

}