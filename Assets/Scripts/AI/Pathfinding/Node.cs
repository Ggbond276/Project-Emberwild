using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Pathfinding
{
    public class Node
    {
        public bool walkable;
        public UnityEngine.Vector3 worldPos;
        public int gridX;
        public int gridY;


        // --- A* 算法新增的寻路变量 ---
        public int gCost; // 距离起点的代价
        public int hCost; // 距离终点的预估代价
        public Node parent; // 记录是从哪个格子走到这里的 (寻路核心：父节点)

        // C# 的属性语法：F值不需要专门赋值，只要读取它，它就会自动计算 G + H
        public int fCost
        {
            get { return gCost + hCost; }
        }


        // 构造函数：当我们创建一块新瓷砖时，把这些信息填进去
        public Node(bool _walkable, UnityEngine.Vector3 _worldPos, int _gridX, int _gridY)
       {
            walkable = _walkable;
            worldPos = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
        }
    }

    
}
