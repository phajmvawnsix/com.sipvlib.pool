# SiPV.Pool

Object pooling for prefabs (bullets, VFX, UI views, enemies) behind one static facade
(`PoolManager`), so gameplay code never calls `Instantiate`/`Destroy` directly.

Depends on `SiPV.Config` (pool tuning lives in a `PoolConfig` asset, looked up by Id),
`SiPV.Utilities` (`Singleton<T>`), `SiPV.Debugging` (`CustomLog`), and `SiPV.Event`
(`EventManager`'s `mono.late_update` tick drives auto-cleanup and deferred despawns).

---

## Spawning and despawning

Two ways to get a pool: a `PoolConfig` asset looked up by Id (recommended — lets designers tune
prewarm/max-size/cleanup per prefab without touching code), or directly by prefab reference
(quick one-off pools, default-tuned).

```csharp
using SiPVLib.Pool;

// By PoolConfig Id (defined as a config asset, see below)
var bullet = PoolManager.Spawn<Bullet>("bullet_basic");

// By prefab reference directly — pool is created on first use with default settings
var vfx = PoolManager.Spawn<ParticleSystem>(explosionPrefab);

// Positioning at spawn time via SpawnParams (parent, world/local transform, RectTransform anchors)
var spawnParams = new SpawnParams
{
    parent = muzzlePoint,
    worldPosition = muzzlePoint.position,
    worldRotation = muzzlePoint.rotation
};
var projectile = PoolManager.Spawn<Bullet>("bullet_basic", spawnParams);

// Despawn — by the object's PooledObject, or by its GameObject
PoolManager.Despawn(bullet);
PoolManager.Despawn(bullet.gameObject);

// Or let the object despawn itself (PooledObject.Despawn(), e.g. from an OnTriggerEnter)
bullet.Despawn();
```

`PoolManager.Spawn<T>` returns `null` (and logs) if the pool couldn't be created, or if the pool
has hit its `MaxSize` — always guard the result before using it.

---

## Defining a `PoolConfig`

`PoolConfig` is a `PrefabConfig` (see [Config module](../Config/README.md)), so it's created and
looked up the same way as any other `GameConfig` — via the Master Window or `[CreateAssetMenu]`,
given an `Id`, and referenced elsewhere by that `Id`.

Tunables on the asset:

| Field | Meaning |
|---|---|
| `Prewarm` / `PrewarmSize` | Create `PrewarmSize` instances up front instead of on first `Spawn`. |
| `ManualPrewarm` | If true, call `PoolManager.PrewarmAsync(poolId)` yourself instead of prewarming on pool creation. |
| `AsyncPrewarm` | Spreads prewarm instantiation across frames (`Object.InstantiateAsync`) instead of one frame spike. |
| `MaxSize` | Hard cap on live instances (0 = unlimited). `Spawn` returns `null` past this. |
| `AutoCleanup` / `CleanupThreshold` | Periodically destroys idle instances down toward `PrewarmSize` (or 0) when the active fraction drops below the threshold — keeps a pool that spiked for a burst (e.g. a wave of enemies) from permanently holding that many instances. |

Pools created directly from a prefab (no `PoolConfig` asset) get one auto-generated at runtime with
default (off) settings — fine for quick prototyping, but give frequently-spawned prefabs a real
`PoolConfig` asset so designers can tune prewarm/cleanup without code changes.

---

## Custom spawn/despawn behavior — `IPoolable`

Attach `PooledObject` (or subclass it) to any prefab. `Pool` calls `OnSpawn()` right after
activating an instance and `OnDespawn()` right before deactivating and returning it to the pool —
override these for per-object reset logic (UI's `UIView` uses this to reset its `Completed` event
and internal state on every reuse):

```csharp
public class Enemy : PooledObject
{
    public override void OnSpawn()
    {
        base.OnSpawn();
        _health = _maxHealth;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        _target = null;
    }
}
```

`PooledObject` also exposes `IsSpawned`, `OnSpawnEvent`/`OnDespawnEvent` (Inspector-wireable
`UnityEvent<PooledObject>`), and an `_autoDespawn` toggle (with `_autoDespawnDelay`) so an object
returns itself to the pool automatically after N seconds without any code — handy for VFX/damage
numbers that just need to play once and disappear.

---

## Pool lifecycle / debug queries

```csharp
// Force-warm a pool ahead of time (e.g. during a loading screen)
await PoolManager.PrewarmAsync("bullet_basic");

// Force every currently-active instance of a pool back to available (e.g. on level/scene teardown)
PoolManager.DespawnAll("enemy_grunt");

// Inspect pool health (debug HUD, tuning PrewarmSize/MaxSize)
var active    = PoolManager.GetActiveCount("bullet_basic");
var available = PoolManager.GetAvailableCount("bullet_basic");
```

`DespawnAll` returns objects to the pool (available for reuse) — it does not destroy them. Pools
themselves live for the application's lifetime under a `DontDestroyOnLoad` root; there is currently
no per-pool "destroy everything and forget the pool" API exposed on `PoolManager` (the underlying
`Pool.Destroy()` exists but isn't surfaced statically, since pools are expected to persist and be
reused across scenes rather than torn down).
