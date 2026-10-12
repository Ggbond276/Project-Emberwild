using Emberwild.Character;
using Emberwild.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Character
{
    public class CharacterEntity : MonoBehaviour
    {
        // 序列化出来方便在 Inspector 里观察当前数值
        [SerializeField]
        public CharacterData RuntimeData = new CharacterData();

        // 接收配置表并初始化
        public void Initialize(CharacterDefine define)
        {
            RuntimeData.InitFromDefine(define);
            Debug.Log($"<color=cyan>角色 [{define.Name}] 数据注入成功！MaxHP: {RuntimeData.MaxHp}</color>");
        }
    }
}
