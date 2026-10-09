# Masked Values verification

Passed 170628 assertions. No reflection or automatic serialization is used by the package.
Measured managed allocation after warmup: masked/guarded primitive and struct read-write, guarded refresh and failed TryGetValue, string CopyTo/ValueEquals, and codec CopyTo: 0 bytes.

Release, single thread, median of 7 samples, 1,000,000 iterations per sample. These are this machine's measurements, not a zero-overhead guarantee.

| Operation | Plain ns/op | Masked ns/op | Guarded ns/op | Guarded - Masked ns/op | Guarded / Masked |
|---|---:|---:|---:|---:|---:|
| int read | 0.23 | 1.16 | 2.54 | 1.38 | 2.19x |
| int array read (1024 values) | 0.41 | 1.38 | 2.79 | 1.41 | 2.02x |
| int write + read | 0.23 | 2.07 | 4.63 | 2.57 | 2.24x |
| float write + read | 0.69 | 2.04 | 4.41 | 2.36 | 2.16x |
| 24-byte struct write + read | 0.67 | 3.20 | 6.94 | 3.74 | 2.17x |

Guarded int RefreshMask: 3.20 ns/op; refresh is explicit, not charged to every read.
64-bit wrapper sizes: int plain 4 B, masked 16 B, guarded 24 B.

Read/write costs and wrapper size increase with the struct size. String Value and reference-containing codecs can allocate. Unity Editor/IL2CPP and target devices must be measured separately.
## 측정 조건과 해석

2026-10-08, Windows x64, Unity 6000.5.10f1에 포함된 .NET SDK 8.0.318 / 런타임 8.0.21에서 `Tools~/verify.ps1`로 측정했습니다. Roslyn 최적화 빌드이며 tiered compilation은 껐습니다. 각 루프를 50,000회 준비 실행한 후 1,000,000회씩 7번 측정한 중앙값입니다. 난수 시드 생성 등 최초 초기화 비용은 정상 경로의 GC 측정과 타이밍에서 제외됩니다.

읽기는 클래스 필드와 1,024개 값 배열을 각각 측정합니다. 쓰기+읽기는 값 대입 후 반환값을 누적하는 루프입니다. JIT는 일반 변수 루프의 일부 작업을 생략하거나 합칠 수 있으므로 Plain 수치를 독립적인 메모리 쓰기 지연으로 해석하면 안 됩니다. 배열 읽기는 여러 저장 위치를 실제로 접근하게 하여 한 값만 반복해서 읽는 최적화의 영향을 줄입니다. 실제 게임의 캐시, 접근 패턴과 프레임 시간은 별도로 측정해야 합니다.

현재 경량 검증도 기존 래퍼보다 대략 2배의 연산 비용이 있습니다. 이번 PC의 `int` 읽기 추가 비용은 약 1.4ns, 쓰기+읽기는 약 2.6ns입니다. 전체 게임에 영향이 없거나 비용이 0이라고 보장하지 않습니다. 기존 `MaskedValue<T>` 구현에는 변경이 없으며 `GuardedValue<T>`를 선택한 값만 검증 비용과 8바이트의 추가 저장 공간을 사용합니다. packed 위치는 정렬된 위치보다 느릴 수 있습니다.

정상 읽기·쓰기·재마스킹 및 이벤트 구독자가 없는 실패 `TryGetValue` 경로의 워밍업 후 GC 할당은 0바이트였습니다. `.Value`와 `RefreshMask()`의 실패 예외 및 사용자 이벤트 처리에서 생기는 할당은 이 보장에 포함되지 않습니다. 최초 프로세스 난수 생성은 런타임의 초기화 비용이 발생할 수 있습니다.

## 검증 범위

- 기본형·enum·포인터·부동소수점 특수 비트·decimal·중첩/packed 구조체의 값 보존.
- 정렬이 다른 위치로 구조체를 복사한 후 읽기와 재마스킹.
- `int` 저장값의 모든 비트, 64비트 키와 검증값의 모든 비트, 큰 구조체 각 바이트에 대한 변조 감지.
- 키를 0으로 바꿔 검증을 우회하는 경우, 손상된 bool/decimal, 기본 상태에 비정상 바이트를 쓰는 경우의 거부.
- 실패한 getter/setter/remask의 예외와 이벤트, 실패한 Try 연산이 상태를 변경하지 않는지 확인.
- 전체 과거 스냅샷 복원과 전체 0 초기화는 감지하지 못한다는 보호 범위 확인.

패키지 런타임에는 리플렉션이 없습니다. 검증 코드만 의도적인 손상을 만들기 위해 private 필드를 변경합니다.

Unity 6000.5.10f1의 실제 .NET Standard 2.1 및 엔진 참조 DLL을 사용한 런타임·샘플·검증 코드 컴파일을 통과했습니다. 이번 변경에서는 Unity Editor 실행, Cheat Engine을 사용한 실제 게임 실험, IL2CPP·모바일 기기의 실행 및 성능은 검증하지 않았습니다. 이전 Editor 실행은 라이선스 확인 실패로 수행하지 못했습니다.
