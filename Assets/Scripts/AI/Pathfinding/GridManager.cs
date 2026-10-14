using Emberwild.Core.AppLog;
using Emberwild.Core.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Pathfinding
{
    /// <summary>
    /// 全局寻路网格管理器 (单例)。
    /// 职责：在游戏初始化时扫描场景物理环境，将 3D 地形降维存储为 2D 节点数组，供 A* 算法随时查询。
    /// </summary>
    public class GridManager : Singleton<GridManager>
    {
        public Node[,] Grid { get; private set; }
        public Vector2 GridWorldSize { get; private set; }
        public float NodeRadius { get; private set; }
        public Vector3 GridOrigin { get; private set; }

        private float _nodeDiameter;
        private int _gridSizeX, _gridSizeY;
        private LayerMask _obstacleMask;

        /// <summary>
        /// 初始化寻路网格系统。必须在游戏启动时（如 TestRunner 的 Start 中）调用一次。
        /// </summary>
        /// <param name="origin">网格生成的绝对世界坐标起点（通常为地形的左下角，如 Vector3.zero）</param>
        /// <param name="worldSize">网格覆盖的总长宽尺寸（X 和 Z 轴的跨度）</param>
        /// <param name="radius">单个寻路节点（瓷砖）的物理检测半径</param>
        /// <param name="mask">障碍物图层，网格在此图层上生成物理球时会被标记为不可行走</param>
        public void Init(Vector3 origin, Vector2 worldSize, float radius, LayerMask mask)
        {
            GridOrigin = origin;
            GridWorldSize = worldSize;
            NodeRadius = radius;
            _obstacleMask = mask;

            _nodeDiameter = NodeRadius * 2;
            _gridSizeX = Mathf.RoundToInt(GridWorldSize.x / _nodeDiameter);
            _gridSizeY = Mathf.RoundToInt(GridWorldSize.y / _nodeDiameter);

            CreateGrid();

            AppLog.Info("Pathfinding", $"网格扫描器初始化完成! 尺寸: {_gridSizeX}x{_gridSizeY}");
        }

        private void CreateGrid()
        {
            Grid = new Node[_gridSizeX, _gridSizeY];
            Vector3 worldBottomLeft = GridOrigin;

            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * _nodeDiameter + NodeRadius) + Vector3.forward * (y * _nodeDiameter + NodeRadius);
                    bool walkable = !Physics.CheckSphere(worldPoint, NodeRadius - 0.1f, _obstacleMask);
                    Grid[x, y] = new Node(walkable, worldPoint, x, y);
                }
            }
        }

        /// <summary>
        /// 将 3D 世界的真实坐标，精准映射并转换为底层的 2D 网格节点。
        /// </summary>
        /// <param name="worldPosition">场景中的任意 3D 世界坐标</param>
        /// <returns>该坐标所处的 Node（节点）对象</returns>
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

        /// <summary>
        /// 获取指定节点周围一圈（最多 8 个）的相邻节点，专供 A* 算法扩散搜索时使用。
        /// </summary>
        /// <param name="node">中心节点</param>
        /// <returns>包含相邻节点的列表，自动排除了超出地图边界的非法节点</returns>
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

                    if (checkX >= 0 && checkX < _gridSizeX && checkY >= 0 && checkY < _gridSizeY)
                        neighbors.Add(Grid[checkX, checkY]);
                }
            }
            return neighbors;
        }
    }
}
