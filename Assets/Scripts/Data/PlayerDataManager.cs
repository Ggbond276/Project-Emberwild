using Emberwild.Character;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using Emberwild.Core.Singleton;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Data
{
    /// <summary>
    /// 玩家存档大管家（全局单例）。
    /// 职责：管理当前游戏会话的 CharacterData（U盘），负责本地 JSON 存档的读取与写入。
    /// 绝对不做：扣血、伤害计算、战斗事件分发（这些全归 CombatAttribute 管）。
    /// </summary>
    public class PlayerDataManager : Singleton<PlayerDataManager>
    {
        private CharacterData _currentSaveData;

        /// <summary>
        /// 提供给全局获取当前存档快照
        /// </summary>
        public CharacterData CurrentSaveData => _currentSaveData;

        /// <summary>
        /// 假设这个是持久化目录
        /// </summary>
        private string SaveFilePath => Application.persistentDataPath + "/PlayerSave.json";

        public PlayerDataManager()
        {
            AppLog.Info("SaveSystem", "PlayerDataManager 启动，准备接管存档系统。");
        }
        public void Init() { }

        public void LoadGame()
        {
            if(File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                _currentSaveData = JsonConvert.DeserializeObject<CharacterData>(json);
                AppLog.Info("PlayerDataManager", $"读档成功！上次记录 HP={_currentSaveData.CurrentHP}");
            } else
            {
                AppLog.Info("PlayerDataManager", "未找到存档，创建新游戏存档...");
                CreateNewGame();
            }
        }

        public void CreateNewGame()
        {
            _currentSaveData = new CharacterData
            {
                TID = 19,
                CurrentHP = 100f,
                IsDead = false
            };
        }

        public void SaveGame(CharacterEntity activePlayer)
        {
            if(activePlayer == null)
            {
                AppLog.Error("PlayerDataManager", "存档失败：找不到活动的玩家实体！");
                return;
            }

            activePlayer.SyncDataBeforeSave();

            string json = JsonConvert.SerializeObject(_currentSaveData, Formatting.Indented);
            File.WriteAllText(SaveFilePath, json);

            AppLog.Info("PlayerDataManager", $"存档写入成功！路径: {SaveFilePath}");
        }



    }
}
