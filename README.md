# com.sipvlib.pool

Part of [SiPVLib](https://github.com/phajmvawnsix/SiPVLib). Object pooling for prefabs (bullets, VFX, UI views, enemies) behind a single static `PoolManager` facade, so gameplay code never calls `Instantiate`/`Destroy` directly. Pool tuning (prewarm, max size, auto cleanup) is configured per prefab via a `PoolConfig` asset looked up by Id, or pools can be created ad hoc directly from a prefab reference.

## Install

Add to your project's `Packages/manifest.json`:

```json
"com.sipvlib.pool": "https://github.com/phajmvawnsix/com.sipvlib.pool.git",
"com.sipvlib.config": "https://github.com/phajmvawnsix/com.sipvlib.config.git",
"com.sipvlib.debugging": "https://github.com/phajmvawnsix/com.sipvlib.debugging.git",
"com.sipvlib.event": "https://github.com/phajmvawnsix/com.sipvlib.event.git",
"com.sipvlib.utilities": "https://github.com/phajmvawnsix/com.sipvlib.utilities.git",
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
```

UPM does not automatically resolve nested git dependencies — you must add the `com.sipvlib.*` and UniTask entries above yourself alongside this package.

## Optional: Odin Inspector

This package integrates with [Odin Inspector](https://odininspector.com) (Sirenix) if you have it installed, but does NOT require it and does NOT bundle it — Odin is a paid Unity Asset Store asset and cannot be redistributed here.

- **Without Odin installed**: `PooledObject` falls back to `MonoBehaviour` (instead of Odin's `SerializedMonoBehaviour`) and its `_autoDespawn`/`_autoDespawnDelay`/event fields render with plain Unity Inspector — no foldout grouping and no conditional show/hide of the auto-despawn fields. `PoolConfig`'s conditional fields (`PrewarmSize`, `ManualPrewarm`, `AsyncPrewarm`, `CleanupThreshold`) are always visible instead of only when their parent toggle (`Prewarm`/`AutoCleanup`) is enabled. Runtime pooling behavior is unaffected either way.
- **With Odin installed** (purchase + import from the Asset Store, which auto-defines the `ODIN_INSPECTOR` scripting define symbol): `PooledObject` inherits Odin's `SerializedMonoBehaviour`, and both `PooledObject` and `PoolConfig` light up Odin's grouping (`FoldoutGroup`) and conditional visibility (`ShowIf`) attributes.

No manual setup is needed beyond installing Odin itself — detection is automatic via the `ODIN_INSPECTOR` define.

## Documentation
- [Usage guide](USAGE.md) — original module documentation carried over from the SiPVLib monolith
