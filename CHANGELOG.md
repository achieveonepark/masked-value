# Changelog

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
