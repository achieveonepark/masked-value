# Masked Values 사용 가이드

Unity에서 클래스 필드의 실제 값을 그대로 보관하지 않도록 하는 성능 우선 패키지입니다. **강한 암호화가 아닌 XOR 기반 메모리 값 은닉**입니다. 리플렉션, 필드 탐색, `JsonUtility`를 사용하지 않습니다.

## 설치

Unity 2022.3 이상에서 `Window > Package Manager > + > Add package from disk`를 선택하고 이 폴더의 `package.json`을 지정합니다. Samples 탭에서 **Class Fields** 예제를 가져올 수 있습니다. 외부 서버나 추가 패키지는 필요 없습니다.

배포용 `com.achieve.masked-values-0.2.0.tgz`는 `+ > Add package from tarball`로 설치할 수 있습니다. [Unity 설치 문서](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-ui-tarball.html)

```csharp
using System;
using Achieve.MaskedValues;

public class Player
{
    [NonSerialized] public MaskedValue<int> hp = 100;
    [NonSerialized] public MaskedValue<float> speed = 5f;
    [NonSerialized] public MaskedValue<MyStats> stats;
    [NonSerialized] public MaskedString name = "Player";

    public void TakeDamage(int damage) => hp.Value -= damage;
}

public struct MyStats
{
    public int level;
    public float damage;
}
```

`MaskedValue<T>`는 C#의 `unmanaged` 조건을 만족하는 기본 자료형, enum, 참조 없는 중첩 구조체를 지원합니다. `bool`, `char`, 정수, 실수, `decimal`, `Vector2/3/4`, `Color`, `Quaternion` 등이 해당합니다. 구조체는 `.Value`로 복사해 수정한 뒤 다시 대입합니다. 포인터를 포함한다면 주소값만 은닉하며 가리키는 메모리를 보호하지 않습니다.

문자열이나 배열, 클래스 참조를 포함한 구조체는 `MaskedData<T>`와 직접 작성한 `ISpanValueCodec<T>`로 사용합니다. 샘플의 `PlayerProfileCodec`은 문자열을 포함한 구조체를 필드 탐색 없이 처리합니다. 어떤 필드를 어떻게 저장할지는 직렬화기에 명시해야 하며 임의의 구조체를 자동으로 탐색하지 않습니다.

## 선택적 변조 감지

HP·재화처럼 메모리 덮어쓰기를 감지할 값에는 `GuardedValue<T>`를 사용할 수 있습니다. 기존 `MaskedValue<T>` 구현은 유지되며, 보호가 필요 없는 값에는 추가 검증 비용을 부과하지 않습니다. 지원하는 `unmanaged` 타입은 동일합니다.

```csharp
GuardedValue<int> hp = 100;
hp.Value -= 10;                 // 기존 상태 검증 후 쓰기
int current = hp.Value;         // 검증 후 복원; 실패하면 InvalidOperationException
hp.RefreshMask();               // 현재 값은 유지하며 키와 저장 비트 갱신

if (!hp.TryGetValue(out current))
{
    // 실패한 current는 default(int). 이 값을 게임 상태로 사용하지 않습니다.
    // 세션 중단, 서버 상태 재조회 등 게임에 맞는 처리를 수행합니다.
}
```

- 저장 바이트와 마스킹 키 전체를 64비트 경량 검증값으로 검사합니다. 프로세스당 한 번 난수 시드를 만들며, 정상적인 읽기·쓰기·재마스킹에는 초기화 후 GC 할당이 없습니다. HMAC이나 암호학적 인증 기능은 아닙니다.
- `CheckIntegrity()`, `TryGetValue`, `TrySetValue`, `TryRefreshMask`는 실패 시 `false`를 반환합니다. 쓰기와 재마스킹도 기존 상태부터 검사하므로, 손상된 값을 다시 정상 값으로 태깅하지 않습니다. 실패한 연산은 상태를 변경하지 않습니다.
- `GuardedValueDiagnostics.TamperingDetected`는 실패한 검사마다 값의 타입을 알립니다. 정상 경로에서는 이벤트를 호출하지 않습니다. 구독자는 같은 손상된 값을 다시 읽거나 예외를 던지지 않아야 합니다. 구독자에서 던진 예외는 `Try` 메서드에서도 전파됩니다. 이벤트를 오류 로그·복구에 사용할 수 있지만 단독으로 계정 제재를 결정하는 근거는 아닙니다.
- `RefreshMask()`는 명시적으로 호출합니다. 매번 읽을 때 갱신하지 않습니다. 샘플은 64프레임에 한 번 호출하며, 게임에 맞게 빈도와 호출 시점을 조정할 수 있습니다. 변경 여부로 검색할 때 후보를 늘리는 기능이며, 이미 찾은 주소에 대한 추적을 차단하지 않습니다.
- 구조체 복사는 독립적으로 수정할 수 있고, 정렬된 위치와 packed 필드 사이의 복사도 지원합니다. 동시 읽기·쓰기에는 외부 동기화가 필요합니다. `IsInitialized`는 초기화 표시이며 무결성 검사를 수행하지 않습니다.

이 보호는 일반적인 저장값·키·검증값 덮어쓰기를 감지합니다. 검증 코드 패치, 난수 시드 추출과 검증값 위조, 객체 전체의 과거 스냅샷 복원, 전체를 0으로 지우는 초기화, 정상 setter 호출은 차단하지 않습니다. `default(GuardedValue<T>)`를 허용하므로 전체 0 초기화와 원래 기본값은 구분할 수 없습니다. 자동 복구나 범용 게임 규칙 검증은 제공하지 않습니다. 온라인 게임의 실제 HP·재화와 유효한 상태 변화는 서버에서 결정해야 합니다.

## 성능

- `MaskedValue<T>`는 초기화 후 생성·읽기·쓰기에서 GC 할당이 없습니다. 고정 크기 데이터에 비트 연산을 수행합니다.
- `GuardedValue<T>`는 경량 검증을 추가하며 초기화 후 GC 할당이 없습니다. 64비트 환경에서 `int` 래퍼는 24바이트로 기존 타입의 16바이트보다 8바이트 큽니다. 대량 배열에는 캐시 비용도 추가됩니다.
- 쓰면 키와 저장된 비트가 바뀝니다. 읽을 때 평문 캐시를 남기지 않습니다.
- `MaskedString`은 대입 시 배열을, `.Value` 읽기 시 문자열을 할당합니다. `CopyTo(Span<char>)`와 `ValueEquals(string)`은 호출자가 버퍼를 제공하면 할당 없이 사용할 수 있습니다.
- `MaskedData<T>`는 대입 시 배열을 할당합니다. 읽기는 작은 값에 스택 버퍼를 사용하고 큰 값은 풀에서 임시 버퍼를 빌립니다. 직렬화기에서 문자열·배열을 만들면 그 할당은 별도로 발생합니다. `CopyTo(Span<byte>)`는 호출자의 버퍼를 재사용합니다.
- 큰 구조체는 바이트 수에 비례해 비용이 늘어납니다. 매 프레임 반복 접근하는 값은 한 번 읽어 지역 변수로 계산한 후 한 번 대입하세요.
- 일반 변수보다 비용이 정확히 0이라고 보장하지 않습니다. `Tests~/SmokeTests`에는 동작·GC 확인과 일반 변수 대비 벤치마크가 있습니다. JIT 결과가 IL2CPP나 모바일 기기의 속도를 보장하지 않습니다.

실측 결과와 추가 비용은 `Documentation~/Performance.md`에 있습니다. .NET 8 최적화 루프에서 비교하며, 배열 읽기도 별도로 측정합니다. 절대 비용이 나노초 단위여도 일반 변수나 기존 래퍼와 완전히 같은 성능이라고 보장하지 않습니다. 큰 구조체, 대량 배열, 접근 빈도에 따라 추가 비용이 누적됩니다.

## 범위

기본 `MaskedValue<T>`의 `.Value`는 `default(T)`입니다. 기본 `MaskedString`의 값은 `null`입니다. 값 타입 복사는 독립적으로 수정할 수 있고, 배열을 사용하는 타입은 대입할 때 새 저장 공간을 만듭니다.

이 타입들은 런타임 전용입니다. Inspector 편집, Unity 직렬화, 저장 파일, 도메인 리로드 후 복원 기능은 제공하지 않습니다. MonoBehaviour/ScriptableObject 필드에는 `[NonSerialized]`를 사용하고 초기 상태는 `Awake` 등에서 설정하세요. 스크립트 리로드 시 값이 초기화될 수 있습니다.

키와 은닉된 값은 같은 프로세스에 존재합니다. 단순 평문 값 검색을 어렵게 하지만 디버거, 키 추출이나 코드를 분석하는 공격자를 막지 않습니다. `MaskedValue<T>`, `MaskedString`, `MaskedData<T>`는 무결성 검증을 제공하지 않습니다. 선택적인 `GuardedValue<T>`는 위 범위에서 변조를 감지하며 메모리 추적 자체를 차단하지 않습니다. 읽어서 반환한 실제 값과 기존 입력 문자열은 별도의 평문 데이터입니다. 로그로 `.Value`를 출력하면 평문이 기록됩니다. 래퍼 자체의 `ToString()`은 내용을 출력하지 않습니다.

## 로컬 검증

```powershell
dotnet run --project 'Tests~/SmokeTests/SmokeTests.csproj' -c Release
```

Unity Editor에서의 실제 컴파일 확인은 `Tests~/UnityProject`를 열어 `MaskedValuesVerification.Run`을 실행할 수 있습니다. 로컬 패키지 참조와 실행 절차는 그 프로젝트의 README에 있습니다.

Unity에 포함된 SDK를 이용해 NuGet 없이 검증하려면 `Tools~/verify.ps1`을 실행합니다. 다른 에디터 경로는 `-UnityEditorPath`로 지정합니다. 이 스크립트는 .NET 테스트와 Unity의 .NET Standard 2.1/엔진 참조 DLL에 대한 컴파일을 검사합니다. 실제 Unity 실행이나 IL2CPP 빌드는 수행하지 않습니다.
