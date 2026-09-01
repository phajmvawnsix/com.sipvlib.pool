# Changelog

## [1.0.1] - 2026-09-01

`PoolConfig.CreateRuntimeInstance` now sets its asset via `AssetConfig.CreateWithAsset` instead of a
direct field assignment, matching the config module's no-public-setter convention.

## [1.0.0] - 2026-07-18

Initial extraction from SiPVLib monolith.
