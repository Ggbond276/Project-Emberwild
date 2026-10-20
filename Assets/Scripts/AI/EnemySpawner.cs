using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emberwild.AI.Enemy;
using Emberwild.Manager;
using UnityEngine;

namespace Emberwild.AI
{
    /// <summary>
    /// 定时刷怪组件。按 TID 从 CharacterDefine 定位 Prefab，并初始化怪物外联表定义。
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("生成设置")]
        [Tooltip("要生成的怪物 TID。Prefab 路径从 CharacterDefine 读取。")]
        public int spawnTid = 2001;

        [Tooltip("生成时间间隔(秒)")]
        public float spawnInterval = 3f;

        [Tooltip("最大同时存在的怪物数量")]
        public int maxEnemies = 10;

        [Tooltip("怪物生成的随机半径范围")]
        public float spawnRadius = 5f;

        private float _timer;
        private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();


        void Update()
        {
            for(int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (_spawnedEnemies[i] == null) _spawnedEnemies.RemoveAt(i);
            }

            if (_spawnedEnemies.Count >= maxEnemies) return;

            _timer += Time.deltaTime;
            if(_timer >= spawnInterval)
            {
                SpawnEnemy();
                _timer = 0f;
            }
        }

        /// <summary>在刷怪器附近生成一只怪物，并按 TID 初始化其运行时定义。</summary>
        private void SpawnEnemy()
        {
            GameObject prefab = ResolvePrefab();
            if (prefab == null) return;

            // 在刷怪器周围随机找一个点
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
            EnemyBrain brain = go.GetComponent<EnemyBrain>();
            if (brain != null)
            {
                brain.InitializeFromTid(spawnTid);
            }
            else
                Debug.LogError($"[EnemySpawner] Prefab '{prefab.name}' has no EnemyBrain component.");
            _spawnedEnemies.Add(go);
        }

        /// <summary>解析本帧应实例化的 Prefab。</summary>
        private GameObject ResolvePrefab()
        {
            if (spawnTid <= 0)
            {
                Debug.LogError("[EnemySpawner] Spawn Tid must be greater than 0.");
                return null;
            }

            var characters = DataManager.Instance.Characters;
            if (characters == null || !characters.TryGetValue(spawnTid, out var define))
            {
                Debug.LogError($"[EnemySpawner] Monster TID {spawnTid} was not found in CharacterDefine.");
                return null;
            }

            GameObject prefab = Resources.Load<GameObject>(define.Resource);
            if (prefab == null)
                Debug.LogError($"[EnemySpawner] Resource '{define.Resource}' for TID {spawnTid} was not found.");
            return prefab;
        }

        void OnDrawGizmos()
        {
            // 画出刷怪器的范围，用半透明绿色表示
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
    }
}
