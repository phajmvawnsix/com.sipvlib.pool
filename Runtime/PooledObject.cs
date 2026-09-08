using System.Threading;
using Alchemy.Inspector;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace SiPVLib.Pool
{
    public class PooledObject : MonoBehaviour, IPoolable
    {
        [FoldoutGroup("Pooling")]
        [Tooltip("Whether the object should automatically return to the pool after a disable or after a delay")]
        [SerializeField] protected bool _autoDespawn;

        [FoldoutGroup("Pooling")]
        [ShowIf(nameof(_autoDespawn))]
        [Tooltip("Delay in seconds before automatically returning to the pool if autoDespawn is enabled")]
        [SerializeField] protected float _autoDespawnDelay;

        [FoldoutGroup("Pooling")]
        [ShowIf(nameof(_autoDespawn))]
        [SerializeField] private UnityEvent<PooledObject> _onSpawnEvent;

        [FoldoutGroup("Pooling")]
        [ShowIf(nameof(_autoDespawn))]
        [SerializeField] private UnityEvent<PooledObject> _onDespawnEvent;

        public UnityEvent<PooledObject> OnSpawnEvent  => _onSpawnEvent;
        public UnityEvent<PooledObject> OnDespawnEvent => _onDespawnEvent;

        public bool IsSpawned { get; private set; }

        protected UniTask _autoDespawnTask = UniTask.CompletedTask;
        protected CancellationTokenSource _autoDespawnCancelToken;

        public virtual void OnSpawn()
        {
            IsSpawned = true;
            _onSpawnEvent.Invoke(this);

            if (!Equals(_autoDespawnTask, UniTask.CompletedTask))
            {
                _autoDespawnCancelToken.Cancel();
                _autoDespawnCancelToken.Dispose();
                _autoDespawnCancelToken = null;
            }

            if (_autoDespawn)
            {
                _autoDespawnCancelToken = new CancellationTokenSource();
                _autoDespawnTask = AutoDespawnAsync(_autoDespawnCancelToken.Token).AttachExternalCancellation(_autoDespawnCancelToken.Token);
            }
        }

        private async UniTask AutoDespawnAsync(CancellationToken token)
        {
            if (_autoDespawnDelay > 0)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(_autoDespawnDelay), cancellationToken: token);
            }

            _autoDespawnTask = UniTask.CompletedTask;
            _autoDespawnCancelToken?.Dispose();
            _autoDespawnCancelToken = null;

            if (IsSpawned)
            {
                Despawn();
            }
        }

        public virtual void OnDespawn()
        {
            IsSpawned = false;
            _onDespawnEvent.Invoke(this);

            if (!Equals(_autoDespawnTask, UniTask.CompletedTask))
            {
                _autoDespawnCancelToken.Cancel();
                _autoDespawnCancelToken.Dispose();
                _autoDespawnCancelToken = null;
            }
        }

        public void Despawn()
        {
            if (IsSpawned)
            {
                PoolManager.Despawn(this);
            }
        }
    }
}
