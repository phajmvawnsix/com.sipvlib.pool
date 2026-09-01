using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SiPVLib.Event;
using SiPVLib.Debugging;
using SiPVLib.Pool.Config;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SiPVLib.Pool
{
    public class SpawnParams
    {
        public Transform parent;

        public int? siblingIndex;
        
        // World space transform
        public Vector3? worldPosition;
        public Quaternion? worldRotation;
        public Vector3? worldScale;
        
        // Local space transform
        public Vector3? localPosition;
        public Quaternion? localRotation;
        public Vector3? localScale;
        
        // RectTransform
        public Vector2? anchorMin;
        public Vector2? anchorMax;
        public Vector2? anchoredPosition;
        public Vector2? sizeDelta;
        public Vector2? pivot;

        public virtual void Apply(PooledObject obj)
        {
            obj.transform.SetParent(parent);
            
            // World space transform setup
            if (worldPosition.HasValue)
            {
                obj.transform.position = worldPosition.Value;
            }
            if (worldRotation.HasValue)
            {
                obj.transform.rotation = worldRotation.Value;
            }
            if (worldScale.HasValue)
            {
                var parentLossyScale = obj.transform.parent != null ? obj.transform.parent.lossyScale : Vector3.one;
                var targetLocalScale = new Vector3(
                    worldScale.Value.x / parentLossyScale.x,
                    worldScale.Value.y / parentLossyScale.y,
                    worldScale.Value.z / parentLossyScale.z);
                obj.transform.localScale = targetLocalScale;
            }
            
            // Local space transform setup
            if (localPosition.HasValue)
            {
                obj.transform.localPosition = localPosition.Value;
            }
            if (localRotation.HasValue)
            {
                obj.transform.localRotation = localRotation.Value;
            }
            if (localScale.HasValue)
            {
                obj.transform.localScale = localScale.Value;
            }
            
            // RectTransform setup
            var rectTransform = obj.GetComponent<RectTransform>();
            
            if (!rectTransform) return;
            
            if (anchorMin.HasValue)
            {
                rectTransform.anchorMin = anchorMin.Value;
            }
            if (anchorMax.HasValue)
            {
                rectTransform.anchorMax = anchorMax.Value;
            }
            if (pivot.HasValue)
            {
                rectTransform.pivot = pivot.Value;
            }
            if (sizeDelta.HasValue)
            {
                rectTransform.sizeDelta = sizeDelta.Value;
            }
            if (anchoredPosition.HasValue)
            {
                rectTransform.anchoredPosition = anchoredPosition.Value;
            }
        }
    }
    
    public sealed class Pool
    {
        private readonly PoolConfig _config;
        private int _spawnCount;
        private bool _isPrewarmed;
        private bool _isWaitingDespawnObjs;
        private readonly Queue<PooledObject> _availableObjs = new();
        private readonly HashSet<PooledObject> _usingObjs = new();
        private readonly Queue<PooledObject> _pendingDespawnObjs = new();
        private readonly Transform _parent;
        private readonly ObjAwakeStats _objAwakeStats;
        private readonly Action<PooledObject> _onObjectDestroyed;

        public int ActiveCount => _usingObjs.Count;
        public int AvailableCount => _availableObjs.Count;

        public Pool(PoolConfig config, Transform parent, Action<PooledObject> onObjectDestroyed = null)
        {
            if (config.Asset == null)
            {
                throw new NullReferenceException($"Prefab is null for pool with ID '{config.Id}'.");
            }
            _config = config;
            _parent = parent;
            _onObjectDestroyed = onObjectDestroyed;
            _objAwakeStats = new ObjAwakeStats(config.Asset);

            if (_config.Prewarm && !_config.ManualPrewarm)
            {
                PrewarmAsync().Forget();
            }

            if (_config.AutoCleanup)
            {
                EventManager.Add(EventManager.EVENT_MONO_LATE_UPDATE, AutoCleanup);
            }
        }

        public void Destroy()
        {
            if (_config.AutoCleanup)
            {
                EventManager.Remove(EventManager.EVENT_MONO_LATE_UPDATE, AutoCleanup);
            }

            if (_isWaitingDespawnObjs)
            {
                EventManager.Remove(EventManager.EVENT_MONO_LATE_UPDATE, DespawnPendingObjs);
                _isWaitingDespawnObjs = false;
            }

            while (_availableObjs.Count > 0)
            {
                var obj = _availableObjs.Dequeue();
                if (!obj) continue;
                _onObjectDestroyed?.Invoke(obj);
                Object.Destroy(obj.gameObject);
            }
        }
        
        public async UniTask PrewarmAsync()
        {
            if (_isPrewarmed) return;
            
            var prewarmSize = _config.PrewarmSize;
            for (var i = 0; i < prewarmSize; i++)
            {
                if (_config.AsyncPrewarm)
                {
                    await CreateAsync();
                }
                else
                {
                    Create();
                }
            }
            _isPrewarmed = true;
        }

        private PooledObject Create()
        {
            if (_config.MaxSize > 0 && _spawnCount >= _config.MaxSize)
            {
                CustomLog.LogError($"Pool with ID '{_config.Id}' has reached its max size.");
                return null;
            }
            
            var newObj = Object.Instantiate(_config.Asset, _parent);
            var pooledObj = newObj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = newObj.AddComponent<PooledObject>();
            }
            newObj.SetActive(false);
            _availableObjs.Enqueue(pooledObj);
            _spawnCount++;
            return pooledObj;
        }

        private async UniTask<PooledObject> CreateAsync()
        {
            if (_config.MaxSize > 0 && _spawnCount >= _config.MaxSize)
            {
                CustomLog.LogError($"Pool with ID '{_config.Id}' has reached its max size.");
                return null;
            }
            
            var operation = await Object.InstantiateAsync(_config.Asset, _parent);
            var newObj = operation.GetValue(0) as GameObject;
            if  (newObj == null) return null;
            var pooledObj = newObj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = newObj.AddComponent<PooledObject>();
            }
            newObj.SetActive(false);
            _availableObjs.Enqueue(pooledObj);
            _spawnCount++;
            return pooledObj;
        }

        public PooledObject Spawn(SpawnParams spawnParams)
        {
            PooledObject pooledObj = null;
            if (_availableObjs.Count == 0)
            {
                // Find in _pendingDespawnObjs
                if (_pendingDespawnObjs.Count > 0)
                {
                    pooledObj = _pendingDespawnObjs.Dequeue();
                    if (_pendingDespawnObjs.Count == 0)
                    {
                        EventManager.Remove(EventManager.EVENT_MONO_LATE_UPDATE, DespawnPendingObjs);
                        _isWaitingDespawnObjs = false;
                    }
                }
                if (pooledObj == null)
                {
                    var createSuccess = Create();
                    if (!createSuccess) return null;

                    pooledObj = _availableObjs.Dequeue();
                }
            }
            else
            {
                pooledObj = _availableObjs.Dequeue();
            }

            if (!_usingObjs.Add(pooledObj)) return null;

            spawnParams?.Apply(pooledObj);
            pooledObj.gameObject.SetActive(true);
            pooledObj.OnSpawn();
            return pooledObj;

        }

        public async UniTask<PooledObject> SpawnAsync(SpawnParams spawnParams)
        {
            PooledObject pooledObj = null;
            if (_availableObjs.Count == 0)
            {
                // Find in _pendingDespawnObjs
                if (_pendingDespawnObjs.Count > 0)
                {
                    pooledObj = _pendingDespawnObjs.Dequeue();
                    if (_pendingDespawnObjs.Count == 0)
                    {
                        EventManager.Remove(EventManager.EVENT_MONO_LATE_UPDATE, DespawnPendingObjs);
                        _isWaitingDespawnObjs = false;
                    }
                }
                if (pooledObj == null)
                {
                    var createSuccess = await CreateAsync();
                    if (!createSuccess) return null;

                    pooledObj = _availableObjs.Dequeue();
                }
            }
            else
            {
                pooledObj = _availableObjs.Dequeue();
            }

            if (!_usingObjs.Add(pooledObj)) return null;

            spawnParams?.Apply(pooledObj);
            pooledObj.gameObject.SetActive(true);
            pooledObj.OnSpawn();
            return pooledObj;

        }

        public void Despawn(PooledObject obj, bool force = false)
        {
            if (!obj) return;

            var parent = obj.transform.parent;
            var isParentActivating = !parent || parent.gameObject.activeInHierarchy;

            if (!_usingObjs.Remove(obj)) return;

            if (isParentActivating || force)
            {
                obj.OnDespawn();
                _availableObjs.Enqueue(obj);
                obj.transform.SetParent(_parent);
                obj.gameObject.SetActive(false);
                _objAwakeStats.Apply(obj);
            }
            else
            {
                _pendingDespawnObjs.Enqueue(obj);
                if (!_isWaitingDespawnObjs)
                {
                    EventManager.Add(EventManager.EVENT_MONO_LATE_UPDATE, DespawnPendingObjs);
                    _isWaitingDespawnObjs = true;
                }
            }
        }

        public void DespawnAllActive()
        {
            if (_usingObjs.Count == 0) return;

            var activeSnapshot = new List<PooledObject>(_usingObjs);
            foreach (var obj in activeSnapshot)
            {
                Despawn(obj, force: true);
            }
        }

        private void AutoCleanup()
        {
            if (_spawnCount == 0) return;

            var activeFraction = (float)_usingObjs.Count / _spawnCount;
            if (activeFraction >= _config.CleanupThreshold) return;

            var minSize = _config.Prewarm ? _config.PrewarmSize : 0;

            while (_availableObjs.Count > 0 && _spawnCount > minSize)
            {
                var obj = _availableObjs.Dequeue();
                if (obj)
                {
                    _onObjectDestroyed?.Invoke(obj);
                    Object.Destroy(obj.gameObject);
                }
                _spawnCount--;

                if (_spawnCount == 0) break;

                activeFraction = (float)_usingObjs.Count / _spawnCount;
                if (activeFraction >= _config.CleanupThreshold) break;
            }
        }

        private void DespawnPendingObjs()
        {
            EventManager.Remove(EventManager.EVENT_MONO_LATE_UPDATE, DespawnPendingObjs);
            if  (_pendingDespawnObjs.Count == 0) return;
            
            var count  = _pendingDespawnObjs.Count;
            for (var i = 0; i < count; i++)
            {
                Despawn(_pendingDespawnObjs.Dequeue(), true);
            }
        }
    }
}