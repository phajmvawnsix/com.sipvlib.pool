using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SiPVLib.Config;
using SiPVLib.Debugging;
using SiPVLib.Pool.Config;
using SiPVLib.Utilities;
using UnityEngine;

namespace SiPVLib.Pool
{
    public sealed class PoolManager : Singleton<PoolManager>
    {
        private static GameObject _rootGameObject;

        private static GameObject RootGameObject
        {
            get
            {
                if (_rootGameObject == null)
                {
                    _rootGameObject = new GameObject("_PoolManager");
                    Object.DontDestroyOnLoad(_rootGameObject);
                }
                
                return _rootGameObject;
            }
        }
        
        private Dictionary<string, Pool> _poolsId = new();
        private Dictionary<int, Pool>  _poolsInstanceId = new();
        private Dictionary<GameObject, Pool> _spawnedObjs = new();
        
        private Pool CreatePool(PoolConfig config)
        {
            if (config == null)
            {
                CustomLog.LogError("PoolManager: PoolConfig is null.");
                return null;
            }
            
            if (_poolsId.ContainsKey(config.Id))
            {
                CustomLog.LogWarning($"PoolManager: Pool with id '{config.Id}' already exists. Skipping creation.");
                return null;
            }

            var pool = new Pool(config, RootGameObject.transform, obj => _spawnedObjs.Remove(obj.gameObject));
            _poolsId[config.Id] = pool;
            _poolsInstanceId[config.Asset.GetInstanceID()] = pool;

            return pool;
        }

        private Pool CreatePool(string poolId)
        {
            var config = ConfigManager.Get<PoolConfig>(poolId);

            if (config == null)
            {
                CustomLog.LogError($"PoolManager: No PoolConfig found for poolId '{poolId}'.");
                return null;
            }
            
            return CreatePool(config);
        }
        
        private Pool CreatePool(GameObject prefab)
        {
            if (prefab == null)
            {
                CustomLog.LogError("PoolManager: Prefab is null.");
                return null;
            }
            
            var config = PoolConfig.CreateRuntimeInstance(prefab);
            return CreatePool(config);
        }
        
        private static Pool GetPool(string poolId)
        {
            if (!Instance._poolsId.TryGetValue(poolId, out var pool))
            {
                pool = Instance.CreatePool(poolId);
                if (pool == null)
                {
                    CustomLog.LogError($"PoolManager: Failed to create pool with id '{poolId}'.");
                    return null;
                }
            }

            return pool;
        }
        
        private static Pool GetPool(GameObject prefab)
        {
            if (!prefab)
            {
                CustomLog.LogError("PoolManager: Prefab is null.");
                return null;
            }
            
            if (!Instance._poolsInstanceId.TryGetValue(prefab.GetInstanceID(), out var pool))
            {
                pool = Instance.CreatePool(prefab);
                if (pool == null)
                {
                    CustomLog.LogError($"PoolManager: Failed to create pool with prefab '{prefab.name}'.");
                    return null;
                }
            }

            return pool;
        }
        
        public static PooledObject Spawn(string poolId)
        {
            return Spawn<PooledObject>(poolId);
        }
        
        public static T Spawn<T>(string poolId, SpawnParams spawnParams = null) where T : Component
        {
            var pool = GetPool(poolId);
            
            if (pool == null)
            {
                return null;
            }

            var result = pool.Spawn(spawnParams);
            
            if (result == null) return null;
            
            Instance._spawnedObjs[result.gameObject] = pool;
            
            return result.GetComponent<T>();
        }
        
        public static T Spawn<T>(GameObject prefab, SpawnParams spawnParams = null) where T : Component
        {
            var pool = GetPool(prefab);
            
            if (pool == null)
            {
                return null;
            }

            var result = pool.Spawn(spawnParams);
            
            if (result == null) return null;
            
            Instance._spawnedObjs[result.gameObject] = pool;
            
            return result.GetComponent<T>();
        }
        
        public static void Despawn(PooledObject pooledObject)
        {
            if (!pooledObject)
            {
                CustomLog.LogError("PoolManager: Attempting to despawn a null PooledObject.");
                return;
            }

            var pool = Instance._spawnedObjs.GetValueOrDefault(pooledObject.gameObject);
            if (pool == null)
            {
                CustomLog.LogError("PoolManager: Attempting to despawn an object that was not spawned by any pool.");
                return;
            }

            pool.Despawn(pooledObject);
        }

        public static void Despawn(GameObject gameObject)
        {
            var pooledObject = gameObject.GetComponent<PooledObject>();
            if (!pooledObject)
            {
                CustomLog.LogError($"PoolManager: GameObject '{gameObject.name}' has no PooledObject component.");
                return;
            }

            Despawn(pooledObject);
        }

        public static void DespawnAll(string poolId)
        {
            var pool = GetPool(poolId);
            pool?.DespawnAllActive();
        }

        public static UniTask PrewarmAsync(string poolId)
        {
            var pool = GetPool(poolId);
            return pool != null ? pool.PrewarmAsync() : UniTask.CompletedTask;
        }

        public static int GetActiveCount(string poolId)
        {
            return Instance._poolsId.TryGetValue(poolId, out var pool) ? pool.ActiveCount : 0;
        }

        public static int GetAvailableCount(string poolId)
        {
            return Instance._poolsId.TryGetValue(poolId, out var pool) ? pool.AvailableCount : 0;
        }
    }
}