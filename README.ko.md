# Masked Values

[![openupm](https://img.shields.io/npm/v/com.achieve.masked-values?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.achieve.masked-values/)
[![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.achieve.masked-values)](https://openupm.com/packages/com.achieve.masked-values/)

[English](README.md)

게임 값을 메모리에서 은닉하고 `GuardedValue<T>`로 일반적인 메모리 변조를 감지하는 패키지입니다. 숫자, unmanaged 구조체, 문자열, 사용자 정의 codec을 사용하는 데이터를 지원합니다. **Unity 2022.3 이상**, 자동 Inspector 표시는 **Unity 6**에서 지원합니다. 외부 패키지 의존성은 없습니다.

## 설치

### Git URL

**Window > Package Manager**에서 **+ > Add package from git URL**을 선택하고 다음 주소를 입력합니다.

```text
https://github.com/achieveonepark/masked-value.git#v1.0.1
```

### OpenUPM

[OpenUPM CLI](https://openupm.com/docs/getting-started-cli.html)를 사용한다면 Unity 프로젝트 폴더에서 실행합니다.

```sh
openupm add com.achieve.masked-values
```

<details>
<summary>CLI 없이 Scoped Registry로 설치하기</summary>

기존 registry와 dependency를 유지하면서 `Packages/manifest.json`에 다음 항목을 합칩니다.

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.achieve.masked-values"]
    }
  ],
  "dependencies": {
    "com.achieve.masked-values": "1.0.1"
  }
}
```

</details>

## 빠른 시작

다음 컴포넌트를 GameObject에 추가합니다. Unity 6에서는 필드가 Inspector에도 표시됩니다.

```csharp
using Achieve.MaskedValues;
using UnityEngine;

public sealed class PlayerState : MonoBehaviour
{
    public GuardedValue<int> health = 100;
    public MaskedValue<float> moveSpeed = 5f;
    public MaskedString playerName = "Player";

    public void TakeDamage(int amount) => health -= amount;
}
```

### 타입 선택

| 타입 | 사용 목적 |
| --- | --- |
| `GuardedValue<T>` | HP·재화 등 은닉과 변조 감지가 모두 필요한 unmanaged 값. |
| `MaskedValue<T>` | 무결성 검사 없이 은닉할 숫자·enum·unmanaged 구조체. |
| `MaskedString` | 문자열. 비교는 `ValueEquals`, 재사용 버퍼로 읽기는 `CopyTo`를 사용합니다. |
| `MaskedData<T>` | 참조를 포함하는 데이터. 명시적인 `ISpanValueCodec<T>`를 전달합니다. Class Fields 샘플을 참고하세요. |

### 읽기·쓰기·연산

```csharp
GuardedValue<int> health = 100;
health -= 10;
health += 5;
health *= 2;
health /= 2;
int currentHealth = health;       // 무결성을 검사하는 암시적 읽기.
health.Value = 80;               // 명시적 쓰기에도 무결성 검사 적용.
bool isAlive = health > 0;
```

숫자 래퍼는 `%`, `%=`, 단항 부호, `++`·`--`도 지원합니다. 연산 결과는 래퍼 타입을 유지하므로 `var result = health + 10`은 `GuardedValue<int>`입니다. 일반 숫자가 필요하면 `int result = health + 10` 또는 `health.Value + 10`을 사용합니다. 정수 덧셈·뺄셈·곱셈의 오버플로는 호출부의 `checked` 블록 안에서도 순환합니다. 원래 타입의 연산 규칙이나 사용자 정의 구조체 연산이 필요하면 `.Value`를 사용하세요.

`GuardedValue<T>.RefreshMask()`는 값을 유지하며 마스킹 표현을 바꿉니다. 필요한 시점에 직접 호출하면 됩니다. 읽기에도 이미 무결성 검사가 적용됩니다.

## Inspector

**Unity 6**에서는 public 필드를 그대로 사용합니다. private 필드에는 일반 Unity 필드처럼 `[SerializeField]`를 붙입니다. 별도 초기값 필드, 연결 특성, `Awake`는 필요하지 않습니다.

| 모드 | 할 수 있는 작업 |
| --- | --- |
| 편집 | 초기값 설정, 씬·프리팹 저장, Undo. |
| 플레이 | 현재 값 확인·수정. 플레이 종료 시 변경사항은 사라집니다. |

런타임 전용 컴포넌트 필드는 `[System.NonSerialized]`를 사용합니다. 사용자 정의 Inspector 값 구조체는 `[System.Serializable]`을 붙이고 Unity 직렬화 규칙을 따릅니다. Unity 2022.3에서는 자동 표시 대신 선택적인 `[MaskedValueField]` 연결 방식을 사용합니다.

이전 버전 예제, 어셈블리 참조, codec 필드 설정은 [Inspector 가이드](Documentation~/Inspector.md)를 참고하세요.

## 변조 감지 처리

`GuardedValue<T>`는 무결성 검사에 실패하면 값을 반환하거나 덮어쓰지 않습니다. `.Value`와 `RefreshMask()`는 `InvalidOperationException`을 발생시키고, `TryGetValue`, `TrySetValue`, `CheckIntegrity`, `TryRefreshMask`는 `false`를 반환합니다. 실패하면 현재 게임 동작을 중단하고 실패 출력값을 정상 상태로 사용하지 마세요.

`GuardedValueDiagnostics.TamperingDetected`를 구독해 대응할 수 있습니다. 이벤트는 검사한 스레드에서 값의 타입을 전달하며, 외부 메모리가 변경된 순간이 아니라 **다음 검사가 실패한 시점**에 호출됩니다. 평소 접근하지 않는 중요한 값은 적절한 주기로 `CheckIntegrity()`를 호출하세요.

감지 시 게임을 종료하려면 [바로 사용할 수 있는 리스너 예제](Documentation~/Tampering.md#unity-listener-quit-on-detection)를 연결합니다. 스레드 안전한 종료 플래그를 확인해 후속 게임 동작을 차단하고 메인 스레드에서 종료를 한 번 요청합니다. Editor에서는 플레이 모드만 종료합니다. 이 연동 없이 패키지가 게임을 자동 종료하지는 않습니다.

## 샘플

**Package Manager > Masked Values > Samples**에서 가져올 수 있습니다.

- **Class Fields** — 런타임 필드, guarded HP, 문자열, 구조체, 사용자 정의 codec.
- **Inspector Fields** — Unity 6용 HP·속도·위치·플레이어 이름·사용자 정의 스탯 필드. `InspectorFieldsExample`을 GameObject에 추가하세요.

## AI 코딩 도구와 사용

포함된 `Skills~/achieve-masked-values` 폴더를 Codex 프로젝트의 `.agents/skills/` 또는 Claude Code 프로젝트의 `.claude/skills/`에 복사합니다. Unity Skill Manager로 패키지 스킬을 취합할 수도 있습니다.

- **Codex:** `$achieve-masked-values`
- **Claude Code:** `/achieve-masked-values`

[스킬](Skills~/achieve-masked-values/SKILL.md)은 필드 설정, Inspector 사용, 메모리·직렬화의 한계와 변조 감지 시 게임 진행을 중단하고 종료를 요청하는 리스너 연동을 안내합니다.

## 알아둘 점

- 마스킹은 단순 평문 검색을 어렵게 하지만 암호화나 완전한 치트 방지 기능은 아닙니다. 코드 패치·정상 스냅샷 복원·정상 API를 통한 변경은 가능합니다. 온라인 게임의 실제 상태는 서버에서 관리하세요.
- Unity 6 초기값은 메모리와 에셋에 평문 설정 데이터로 남습니다. 로드·복제 시 현재 게임 상태가 아닌 초기값을 복원합니다. `[NonSerialized]`는 직렬화 설정이지 메모리 보호 기능이 아닙니다.
- unmanaged 값의 정상 읽기·쓰기·재마스킹은 초기화 후 GC 할당이 없습니다. 문자열 읽기·대입과 codec은 할당이 생길 수 있고 큰 구조체는 메모리와 처리 비용도 커집니다. 동시 접근은 직접 동기화해야 합니다.

더 보기: [사용 가이드](Documentation~/masked-values.md) · [변조 이벤트와 종료](Documentation~/Tampering.md) · [성능 상세](Documentation~/Performance.md)

## 라이선스

[MIT](LICENSE.md) © 2026 achieveonepark.
