using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Pathfinding
{
    /// <summary>
    /// A* (A-Star) 核心寻路算法引擎 (无状态静态类)。
    /// 职责：提供纯数学的路径解算服务，不依赖任何生命周期，按需调用。
    /// </summary>
    public static class AStarAlgorithm
    {
        /// <summary>
        /// 基于当前全局网格，计算出两点之间的最短避障路径。
        /// </summary>
        /// <param name="startPos">寻路者的起始 3D 世界坐标</param>
        /// <param name="targetPos">目标的 3D 世界坐标</param>
        /// <returns>
        /// 寻路成功：返回有序的节点列表 (List)，索引 0 为下一步要走的节点，末尾为目标节点。
        /// 寻路失败（如目标在死胡同内、被障碍物完全包裹）：返回 null。
        /// </returns>
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
                    if (openSet[i].fCost < currentNode.fCost ||
                        openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
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

                    int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                    if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
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

        private static int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

            if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

        /// <summary>
        /// 清理本次搜索访问过的节点，避免上一只怪物的 gCost、hCost 和 parent
        /// 污染下一次寻路。Node 网格是全局共享的，不能保留单次搜索状态。
        /// </summary>
        private static void ResetSearchData(IEnumerable<Node> first, IEnumerable<Node> second)
        {
            var reset = new HashSet<Node>();
            ResetNodes(first, reset);
            ResetNodes(second, reset);
        }

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

    }
}
