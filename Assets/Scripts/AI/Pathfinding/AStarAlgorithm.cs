using System.Collections.Generic;
using UnityEngine;

namespace Emberwild.AI.Pathfinding
{
    public static class AStarAlgorithm
    {
        #region A* 路径计算 (A* Path Calculation)
        /// <summary>计算起点到目标点的 A* 路径。</summary>
        public static List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            GridManager grid = GridManager.Instance;
            if (grid == null) return null;

            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node targetNode = grid.NodeFromWorldPoint(targetPos);

            if (!targetNode.walkable) return null;

            List<Node> openSet = new List<Node>();
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost
                        || openSet[i].fCost == currentNode.fCost
                        && openSet[i].hCost < currentNode.hCost)
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    List<Node> path = RetracePath(startNode, targetNode);
                    ResetSearchData(closedSet, openSet);
                    return path;
                }

                foreach (Node neighbor in grid.GetNeighbors(currentNode))
                {
                    if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;

                    int newMovementCostToNeighbor =
                        currentNode.gCost + GetDistance(currentNode, neighbor);
                    if (newMovementCostToNeighbor < neighbor.gCost
                        || !openSet.Contains(neighbor))
                    {
                        neighbor.gCost = newMovementCostToNeighbor;
                        neighbor.hCost = GetDistance(neighbor, targetNode);
                        neighbor.parent = currentNode;

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            ResetSearchData(closedSet, openSet);
            return null;
        }

        /// <summary>从目标节点回溯出正向路径。</summary>
        private static List<Node> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();
            return path;
        }

        /// <summary>计算两个网格节点之间的移动代价。</summary>
        private static int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

            if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

        /// <summary>清理本次搜索写进共享节点的数据。</summary>
        private static void ResetSearchData(IEnumerable<Node> first, IEnumerable<Node> second)
        {
            var reset = new HashSet<Node>();
            ResetNodes(first, reset);
            ResetNodes(second, reset);
        }

        /// <summary>重置一组节点的寻路代价和父节点。</summary>
        private static void ResetNodes(IEnumerable<Node> nodes, HashSet<Node> reset)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node == null || !reset.Add(node)) continue;
                node.gCost = 0;
                node.hCost = 0;
                node.parent = null;
            }
        }
        #endregion
    }
}
