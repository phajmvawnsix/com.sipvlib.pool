# Changelog

## [1.1.0] - 2026-09-08

Odin Inspector is no longer required. PooledObject drops SerializedMonoBehaviour for plain MonoBehaviour; PoolConfig's inspector attributes now come from Alchemy (com.annulusgames.alchemy) instead of Sirenix.OdinInspector.

## [1.0.3] - 2026-09-03

Pin com.sipvlib.config/debugging/event/utilities and com.cysharp.unitask to semver versions instead of git URLs, so this package installs cleanly via the OpenUPM registry.

## [1.0.2] - 2026-09-03

Lower minimum Unity Editor version to 2022.3 LTS (was 6000.3) and add a `repository`
field to `package.json`, both required for OpenUPM registry submission.

## [1.0.1] - 2026-09-01

`PoolConfig.CreateRuntimeInstance` now sets its asset via `AssetConfig.CreateWithAsset` instead of a
direct field assignment, matching the config module's no-public-setter convention.

## [1.0.0] - 2026-07-18

Initial extraction from SiPVLib monolith.
