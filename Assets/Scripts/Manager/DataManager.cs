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

        public Dictionary<int, CharacterDefine> Character = null;

         public DataManager()
        {
            this.DataPath = "Data/Data/";
            AppLog.Info("DataManager", "DataManager Init()");
        }

        public void Load()
        {
            string json = File.ReadAllText(this.DataPath + "CharacterDefine.txt");
            this.Character = JsonConvert.DeserializeObject<Dictionary<int, CharacterDefine>>(json);

        }

        public IEnumerator LoadData()
        {
            string json = File.ReadAllText(this.DataPath + "CharacterDefine.txt");
            this.Character = JsonConvert.DeserializeObject<Dictionary<int, CharacterDefine>>(json);
            yield return null;
        }
    }
}
