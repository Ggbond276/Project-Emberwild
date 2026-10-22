using System.Collections.Generic;
using Emberwild.Core.AppLog;
using Emberwild.Core.Singleton;
using UnityEngine;

namespace Emberwild.AI.Pathfinding
{
    public class GridManager : Singleton<GridManager>
    {
        #region 网格内部状态 (Grid State)
        private float _nodeDiameter;
        private float _clearanceRadius;
        private int _gridSizeX;
        private int _gridSizeY;
        private LayerMask _obstacleMask;
        #endregion

        #region 网格数据 (Grid Data)
        public Node[,] Grid { get; private set; }
        public Vector2 GridWorldSize { get; private set; }
        public float NodeRadius { get; private set; }
        public float ClearanceRadius => _clearanceRadius;
        public Vector3 GridOrigin { get; private set; }
        #endregion

        #region 网格初始化 (Grid Initialization)
        /// <summary>初始化寻路网格尺寸并生成节点。</summary>
        public void Init(
            Vector3 origin,
            Vector2 worldSize,
            float radius,
            float clearanceRadius,
            LayerMask mask)
        {
            GridOrigin = origin;
            GridWorldSize = worldSize;
            NodeRadius = Mathf.Max(0.01f, radius);
            _clearanceRadius = Mathf.Max(0.01f, clearanceRadius);
            _obstacleMask = mask;

            _nodeDiameter = NodeRadius * 2;
            _gridSizeX = Mathf.Max(
                1, Mathf.RoundToInt(GridWorldSize.x / _nodeDiameter));
            _gridSizeY = Mathf.Max(
                1, Mathf.RoundToInt(GridWorldSize.y / _nodeDiameter));

            CreateGrid();

            AppLog.Info(
                "Pathfinding",
                $"网格扫描器初始化完成! 尺寸: {_gridSizeX}x{_gridSizeY} "
                + $"节点半径: {NodeRadius:0.###} "
                + $"避障半径: {_clearanceRadius:0.###}");
        }

        /// <summary>按配置尺寸生成全部寻路节点。</summary>
        private void CreateGrid()
        {
            Grid = new Node[_gridSizeX, _gridSizeY];
            Vector3 worldBottomLeft = GridOrigin;

            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft
                        + Vector3.right * (x * _nodeDiameter + NodeRadius)
                        + Vector3.forward * (y * _nodeDiameter + NodeRadius);
                    bool walkable = !Physics.CheckSphere(
                        worldPoint,
                        _clearanceRadius,
                        _obstacleMask);
                    Grid[x, y] = new Node(walkable, worldPoint, x, y);
                }
            }
        }
        #endregion

        #region 网格坐标与邻接查询 (Grid Queries)
        /// <summary>把世界坐标转换为对应网格节点。</summary>
        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            float percentX = (worldPosition.x - GridOrigin.x) / GridWorldSize.x;
            float percentY = (worldPosition.z - GridOrigin.z) / GridWorldSize.y;

            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);
            return Grid[x, y];
        }

        /// <summary>获取指定节点周围的有效相邻节点。</summary>
        public List<Node> GetNeighbors(Node node)
        {
            List<Node> neighbors = new List<Node>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;
                    if (checkX >= 0 && checkX < _gridSizeX
                        && checkY >= 0 && checkY < _gridSizeY)
                    {
                        neighbors.Add(Grid[checkX, checkY]);
                    }
                }
            }
            return neighbors;
        }
        #endregion
    }
}
