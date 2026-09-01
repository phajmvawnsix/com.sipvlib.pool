using System;
using System.Collections.Generic;
using PipaPlanet.PipaPlanet.Scripts.Utilities;
using SiPVLib.Config.Configs;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Pool.Config
{
    public class PoolConfig : PrefabConfig
    {
        [Tooltip("Whether to prewarm the pool on creation." +
                 "If true, the pool will create a number of instances equal to PrewarmSize when it is created.")]
        [SerializeField] protected bool _prewarm;

        [Tooltip("Number of instances to prewarm if prewarming is enabled.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(_prewarm))]
#endif
        [SerializeField] protected int _prewarmSize;

        [Tooltip("Whether to manually trigger prewarming." +
                 "If false, prewarming will be triggered automatically when the pool is created.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(_prewarm))]
#endif
        [SerializeField] protected bool _manualPrewarm;

        [Tooltip("Whether to prewarm the pool asynchronously." +
                 "If true, prewarming will be done over multiple frames to avoid performance spikes." +
                 "If false, all prewarming will be done in a single frame.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(_prewarm))]
#endif
        [SerializeField] protected bool _asyncPrewarm;

        [Tooltip("Maximum number of instances allowed in the pool." +
                 "If the pool reaches this limit, it will not create new instances and will return null when trying to spawn." +
                 "Set to 0 or less for unlimited.")]
        [SerializeField] protected int _maxSize;

        [Tooltip("Whether to automatically clean up inactive objects in the pool." +
                 "If true, the pool will periodically check the percentage of active objects and clean up inactive ones if it falls below the CleanupThreshold.")]
        [SerializeField] protected bool _autoCleanup = true;

        [Tooltip("Threshold (0-1) for auto cleanup." +
                 "When the percentage of active objects in the pool below this threshold, the pool will automatically clean up inactive objects." +
                 "Cleanup will remains total objects in the pool not less than prewarm size.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(_autoCleanup))]
#endif
        [SerializeField] protected float _cleanupThreshold = 0.5f;

        // ── Properties ───────────────────────────────────────────────────

        public bool Prewarm => _prewarm;
        public bool ManualPrewarm => _manualPrewarm;
        public bool AsyncPrewarm => _asyncPrewarm;

        public int PrewarmSize => _prewarmSize;

        public int MaxSize => _maxSize;

        public bool AutoCleanup => _autoCleanup;

        public float CleanupThreshold => _cleanupThreshold;

        private static readonly List<PoolConfig> RuntimePoolConfigs = new();

        public static PoolConfig CreateRuntimeInstance(GameObject prefab)
        {
            var poolConfig = CreateWithAsset<PoolConfig>(prefab);
            poolConfig._id = Guid.NewGuid().ToString();
            RuntimePoolConfigs.Add(poolConfig);
            return poolConfig;
        }

        /// <summary>
        /// Cleans up all runtime-created PoolConfig instances.
        /// Should be called when application finished playing.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void CleanupRuntimeInstances()
        {
            foreach (var config in RuntimePoolConfigs)
            {
                Destroy(config);
            }
            RuntimePoolConfigs.Clear();
        }
    }
}
