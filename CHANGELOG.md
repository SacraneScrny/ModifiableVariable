# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.0.0] - 2026-10-10

### Added
- Initial UPM release: `Modifiable<T, TStage>` core (base value + ordered stages, per-frame cached `Value`, change callbacks).
- Ready-made pipelines (`Modifiables.cs`): `Simple / General / Complex / Damage / Defense / Speed / Resource / Chance / Cooldown / Color / Position / Rotation / Overridable / Blend` + `Gate*` (bool) variants.
- `StageOpAttribute` + `ModifiableFactory`, `StageArithmetic<T>` registry with primitive and Unity type bootstraps.
- Epsilon-aware comparers (`Comparers/`), disposable modifier/callback handles (`Entities/`).
- Inspector drawer (`Editor/`): Base | Result view (separate `ModifiableVariables.Editor` assembly).
- UPM packaging: `package.json`, assembly definitions, `.meta` files.
