using UnityEngine;

namespace Emberwild.AI.Pathfinding
{
    public class Node
    {
        #region 节点数据 (Node Data)
        // 网格位置与 A* 搜索数据
        public bool walkable;
        public Vector3 worldPos;
        public int gridX;
        public int gridY;

        public int gCost;    // 距离起点的代价
        public int hCost;    // 距离终点的预估代价
        public Node parent;  // 当前路径的父节点
        #endregion

        #region 计算属性 (Computed Properties)
        public int fCost => gCost + hCost;
        #endregion

        #region 构造方法 (Constructor)
        /// <summary>创建节点并写入位置和搜索初始值。</summary>
        public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY)
        {
            walkable = _walkable;
            worldPos = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
        }
        #endregion
    }
}
