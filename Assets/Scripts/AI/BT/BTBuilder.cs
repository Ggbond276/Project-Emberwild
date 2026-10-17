using Emberwild.AI.Enemy.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.BT
{
    public static class BTBuilder
    {
        public static BTNode Build(List<BTNodeDef> children)
        {
            var selector = new Selector();
            foreach (var def in children) selector.Add(BuildNode(def));
            return selector;
        }

        private static BTNode BuildNode(BTNodeDef def)
        {
            switch (def.type)
            {
                case BTNodeType.Selector:
                    var sel = new Selector();
                    foreach (var c in def.children) sel.Add(BuildNode(c));
                    return sel;
                case BTNodeType.Sequence:
                    var seq = new Sequence();
                    foreach (var c in def.children) seq.Add(BuildNode(c));
                    return seq;
                case BTNodeType.Condition:
                    return new Condition(ConditionFactory.Build(def));
                case BTNodeType.Action:
                    return new ActionNode(def.action);
                default:
                    return new ActionNode(EnemyStateType.Idle);
            }
        }
    }
}
