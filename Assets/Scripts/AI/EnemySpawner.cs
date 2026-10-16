using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("生成设置")]
        [Tooltip("要生成的怪物预制体(必须挂有 CustomNavAgent 和 EnemyAI)")]
        public GameObject enemyPrefab;

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

        private void SpawnEnemy()
        {
            if (enemyPrefab == null) return;

            // 在刷怪器周围随机找一个点
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            GameObject go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            _spawnedEnemies.Add(go);
        }

        void OnDrawGizmos()
        {
            // 画出刷怪器的范围，用半透明绿色表示
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
    }
}
