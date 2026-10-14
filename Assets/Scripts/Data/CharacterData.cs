using Emberwild.Define;
using UnityEngine;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色存档数据 (DTO) — 绝对纯净，只存动态变化的值。
    /// </summary>
    [System.Serializable]
    public class CharacterData
    {
        public int TID;
        public float CurrentHP;
        public bool IsDead;
    }
}
