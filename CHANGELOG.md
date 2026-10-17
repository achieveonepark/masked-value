# Changelog

## 1.0.1 - 2026-10-17

- Document the existing tamper event's detection timing, type-only payload, threading, repeated notifications, and integrity/serialization limits.
- Add a Unity shutdown listener guide: stop normal gameplay on detection, signal from any checking thread, and request shutdown once on the main thread; stop Play mode in the Editor.
- Update the integration skill with shutdown-on-detection policy, failure handling, lifecycle, platform constraints, and isolated verification guidance.
- Add matching Codex and Claude skills under `.agents/skills` and `.claude/skills`, with a sync script and CI consistency check. Distributed packages keep the canonical `Skills~` source.
- Runtime APIs and value-access implementations are unchanged. Shutdown remains an explicit game integration policy.

## 1.0.0 - 2026-10-17

- Unity 6: declare `GuardedValue<T>`, `MaskedValue<T>`, `MaskedString`, and codec-backed `MaskedData<T>` directly in the Inspector. No companion field, linking attribute, or initialization method is required.
- Native row heights and controls for authoring initial values and viewing/editing live values. Plain fields in arrays and nested structs are supported.
- Serialization restores fresh masked state from authored initial values. Runtime storage, keys, and tags remain nonserialized. Authored defaults remain available for object cloning.
- Unity 6 wrappers retain an initial-value field for serialization; large structs can occupy more memory. Gameplay getters/setters add no Inspector reflection, polling, or lazy initialization. Standalone .NET layouts remain unchanged.
- Explicit arithmetic, compound assignment, remainder, unary, and increment/decrement operators for 12 numeric types, with guarded/masked combinations and string concatenation. Guarded operands still validate integrity.
- Preserve the optional attribute bridge for Unity 2022.3 and custom initialization. Automatic struct serialization uses Unity 6 callbacks.
- Verify numeric operators and warm zero allocations, Unity serialization/Undo, direct field/array editing, and the original behavior suite.
- Optional `[MaskedValueField]` bridge for separate initial values and custom runtime initialization.
- Separate Unity attribute and Editor assemblies; core gameplay getters/setters retain their existing paths.
- Support private inherited fields, nested structs and collection owners, strings, Unity values, and serializable codec-backed data.
- Isolated native editing buffers handle delayed controls without saving current values into scenes or prefabs. Runtime edits preserve nonserialized state and changes to untouched fields.
- Inspector Fields sample, usage guide, binding tests, and actual Unity serialization/Undo checks.
- First public release, consolidating Inspector support and operators with the masking and integrity-checking APIs developed in 0.1.0 and 0.2.0.
- MIT license, Unity package metadata, and OpenUPM submission metadata.
- English README with a separate Korean translation.
- Consistent distribution contents for local and OpenUPM packaging, including documentation, samples, and the AI skill.
- CI validation of package contents, behavior, allocations, and matching release tags.

## 0.2.0

- Optional unmanaged `GuardedValue<T>` with allocation-free integrity checks on reads, writes and remasking, preserving the original `MaskedValue<T>` implementation.
- Explicit `RefreshMask`/`TryRefreshMask`, `CheckIntegrity`, `TryGetValue` and `TrySetValue` APIs; invalid state is rejected before it can be overwritten or re-tagged.
- Optional `GuardedValueDiagnostics.TamperingDetected` notification on failed checks.
- Non-cryptographic 64-bit tags cover masked storage and the full masking key; aligned and packed copies produce identical tags.
- Guarded health sample, deliberate corruption tests and comparative timing/allocation measurements.

Guarding detects ordinary memory overwrites. It does not prevent code patches, key/seed extraction, valid snapshot replay, full erasure to default, or writes through legitimate APIs. Runtime-only, not thread-safe, and not a replacement for server authority.

## 0.1.0

- Reflection-free `MaskedValue<T>` for primitives, enums and unmanaged structs.
- Aligned word-size XOR paths with byte-wise fallback for packed data.
- `MaskedString` with allocation-free `CopyTo` and `ValueEquals`.
- Explicit span codecs for reference-containing structs/classes in `MaskedData<T>`.
- Runtime class-field samples and comparative performance/GC verification.

This package provides reversible memory masking, not cryptographic encryption or tamper detection.
