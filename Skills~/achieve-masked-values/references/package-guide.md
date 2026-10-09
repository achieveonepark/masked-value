# Masked Values 구현 가이드

## 타입 선택

| 요구 | 타입 |
| --- | --- |
| primitive, enum, 참조 없는 구조체의 은닉 | `MaskedValue<T>` where T : unmanaged |
| 위 값의 저장 상태 검증 추가 | `GuardedValue<T>` where T : unmanaged |
| 문자열 | `MaskedString` |
| 문자열/배열/참조를 포함한 데이터 | `MaskedData<T>` + `ISpanValueCodec<T>` |

```csharp
using System;
using Achieve.MaskedValues;
using UnityEngine;

public sealed class PlayerState : MonoBehaviour
{
    [NonSerialized] private GuardedValue<int> health;
    [NonSerialized] private MaskedValue<float> speed;
    private void Awake()
    {
        health = new GuardedValue<int>(100);
        speed = new MaskedValue<float>(5f);
    }

    public bool TryTakeDamage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!health.TryGetValue(out var current)) return false;
        return health.TrySetValue(Math.Max(0, current - amount));
    }
}
```

## 무결성과 복사

`GuardedValue<T>.Value`는 손상 시 예외를 던진다. `CheckIntegrity`, `TryGetValue`, `TrySetValue`, `TryRefreshMask`는 실패를 반환하며 실패한 쓰기·재마스킹은 기존 상태를 바꾸지 않는다. 기본 0 상태는 유효하다. `IsInitialized`는 무결성 검사 결과가 아니다.

`GuardedValueDiagnostics.TamperingDetected`는 실패한 검사마다 타입만 전달한다. 핸들러가 같은 손상된 값을 다시 읽어 재귀를 만들지 않게 한다. 게임에서 워커 스레드 접근이 있으면 외부 동기화와 진단 이벤트의 스레드 처리를 설계한다.

래퍼 복사는 독립된 값처럼 사용한다. 원본을 변경할 목적이라면 원래 필드에 다시 대입하거나 적절한 ref 접근을 사용한다. 마스킹된 내부 storage는 유효하지 않은 bool/float/decimal 비트일 수 있으므로 core 구현에서 T로 읽지 않고 바이트로 취급한다.

## 명시적 codec과 할당

```csharp
public interface ISpanValueCodec<T>
{
    int GetByteCount(in T value);
    void Encode(in T value, System.Span<byte> destination);
    T Decode(System.ReadOnlySpan<byte> source);
}
```

이는 실제 인터페이스의 서명 설명이다. 게임 코드에서 인터페이스를 다시 선언하지 않고 패키지 인터페이스를 구현한다. Span은 저장하지 않는다. 길이·문자 인코딩·바이트 순서·손상된 입력 처리를 명시하고 `Samples~/ClassFields/PlayerProfileCodec.cs`를 필요할 때 읽는다.

`MaskedValue<T>`와 `GuardedValue<T>`는 초기화 이후 일반 읽기·쓰기에 관리형 할당이 없다. `MaskedString.Value`는 문자열을 생성하고 대입은 배열을 만든다. 호출자 버퍼를 재사용하는 `CopyTo`나 `ValueEquals`로 필요 없는 문자열 생성을 피한다. `MaskedData<T>` 대입은 저장 배열을 만들고 Decode 과정은 codec에 따라 할당한다.

제공된 래퍼의 `ToString()`은 평문 대신 표식 문자열을 반환한다. `.Value`를 로그 또는 추적 페이로드에 전달하면 평문을 명시적으로 복원해 기록하는 것이다. 포인터 값의 은닉이 가리키는 메모리까지 보호하지는 않는다.

## 구현과 검증 위치

core 구현은 `Runtime/MaskedValue.cs`, `GuardedValue.cs`, `ValueMask.cs`, `ValueIntegrity.cs`, `MaskKeys.cs`다. 문자열·codec은 `MaskedString.cs`, `MaskedData.cs`, `ISpanValueCodec.cs`를 확인한다. 이 패키지에는 에디터 창이 없다. UI를 추가하는 작업이 요청되면 UI Toolkit과 Unity 톤을 따른다.

`Tools~/verify.ps1`은 Unity에 포함된 SDK로 .NET 검증과 엔진 참조 컴파일을 할 수 있다. .NET 실행 결과는 실제 Unity 또는 IL2CPP 실행 결과와 구분한다. `Tests~/UnityProject`는 별도 Unity 검증 프로젝트다. 툴 실행 전 그 스크립트의 입력·출력 경로를 확인하고 현재 게임 프로젝트를 임의로 교체하지 않는다.

세 패키지 연동 예제의 HP·골드·페이즈는 알려진 원본 루트의 `Samples/AchieveIntegration/Runtime/IntegrationDemoPlayer.cs`에서 확인한다.
