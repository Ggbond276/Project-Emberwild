using Emberwild.Core.AppLog;
using Emberwild.Core.Singleton;
using Emberwild.Define;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Emberwild.Manager
{
    public class DataManager : Singleton<DataManager>
    {
        public string DataPath;

        public Dictionary<int, CharacterDefine> Characters = null;
        public Dictionary<int, AttackDefine> Attacks = null;
        public Dictionary<string, Dictionary<int, AttackDefine>> ComboGroupDict = new Dictionary<string, Dictionary<int, AttackDefine>>();

         public DataManager()
        {
            this.DataPath = "Data/Data/";
        }

        public void Init()
        {
            AppLog.Info("DataManager", "DataManager Init");
        }

        public void Load()
        {
            string json = File.ReadAllText(this.DataPath + "CharacterDefine.txt");
            this.Characters = JsonConvert.DeserializeObject<Dictionary<int, CharacterDefine>>(json);

            json = File.ReadAllText(this.DataPath + "AttackDefine.txt");
            this.Attacks = JsonConvert.DeserializeObject<Dictionary<int, AttackDefine>>(json);

            ComboGroupDict.Clear();
            foreach(var kvp in Attacks)
            {
                AttackDefine atk = kvp.Value;
                if(!ComboGroupDict.ContainsKey(atk.ComboGroup))
                {
                    ComboGroupDict[atk.ComboGroup] = new Dictionary<int, AttackDefine>();
                }

                // 把这把武器的第 N 刀，塞进对应的坑位里
                ComboGroupDict[atk.ComboGroup][atk.ComboIndex] = atk;
            }
        }

        public IEnumerator LoadData()
        {
            string json = File.ReadAllText(this.DataPath + "CharacterDefine.txt");
            this.Characters = JsonConvert.DeserializeObject<Dictionary<int, CharacterDefine>>(json);
            yield return null;

            json = File.ReadAllText(this.DataPath + "AttackDefine.txt");
            this.Attacks = JsonConvert.DeserializeObject<Dictionary<int, AttackDefine>>(json);
            yield return null;
        }
    }
}
