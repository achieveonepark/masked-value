# Masked Values

[English](README.md)

런타임 값 은닉과 선택적 변조 감지를 제공하는 Unity 패키지입니다. Unity **2022.3 이상**을 지원하며 외부 패키지 의존성이 없습니다. XOR 기반 값 은닉이며 암호학적 보안 기능은 아닙니다.

## 설치

Package Manager → **+ → Add package from git URL**:

```text
https://github.com/achieveonepark/masked-value.git#v1.2.0
```

[OpenUPM](https://openupm.com/packages/com.achieve.masked-values/) 등록이 완료되면 Unity 프로젝트 폴더에서 다음 명령으로도 설치할 수 있습니다.

```sh
openupm add com.achieve.masked-values
```

Scoped Registry를 직접 설정하는 방법은 [영문 README](README.md#openupm)를 참고하세요.

## 사용

```csharp
using Achieve.MaskedValues;
using UnityEngine;

public sealed class PlayerState : MonoBehaviour
{
    public GuardedValue<int> health = 100;
    public MaskedValue<float> speed = 5f;

    public void TakeDamage(int amount) => health -= amount;
}
```

`MaskedValue<T>`는 `unmanaged` 값을 은닉하고, `GuardedValue<T>`는 읽기·쓰기·명시적 재마스킹 시 무결성 검사도 수행합니다. 문자열은 `MaskedString`, 참조를 포함한 데이터는 `MaskedData<T>`와 명시적 `ISpanValueCodec<T>`를 사용합니다.

변조 감지 시 `TryGetValue`, `TrySetValue`, `CheckIntegrity`, `TryRefreshMask`는 `false`를 반환하며, `Value` 접근과 `RefreshMask()`는 `InvalidOperationException`을 발생시킵니다. 재마스킹은 `RefreshMask()`로 직접 호출합니다.

Unity 6에서는 위 필드 선언만으로 Inspector 표시, 초기값 저장, 로드 시 복원을 지원합니다. `+=`, `-=`, `*=`, `/=`, `%=`, `++`, `--`도 사용할 수 있습니다. 런타임 전용 필드는 `[NonSerialized]`를 사용하세요. 동시 접근에는 외부 동기화가 필요합니다.

키 추출, 검증 코드 패치, 정상 스냅샷 복원, 전체 기본값 초기화, 정상 API를 통한 변경은 막지 못합니다. 온라인 게임의 실제 상태는 서버에서 검증해야 합니다. 초기화 후 unmanaged 값의 정상 읽기·쓰기·재마스킹에는 GC 할당이 없지만 문자열과 codec 처리에서는 할당이 발생할 수 있습니다.

샘플: Package Manager의 **Class Fields**를 Import합니다.

자세한 API·제약은 [사용 가이드](Documentation~/masked-values.md), 성능 측정은 [성능 문서](Documentation~/Performance.md)를 참고하세요.

## AI 스킬

스킬 원본은 [`Skills~/achieve-masked-values`](Skills~/achieve-masked-values/SKILL.md)에 있습니다. Unity Skill Manager가 프로젝트의 `.agents/skills`와 `.claude/skills`에 취합합니다. 직접 호출: Codex `$achieve-masked-values`, Claude Code `/achieve-masked-values`.

## Inspector 지원

Unity 6에서는 기존 타입을 그대로 선언합니다. 별도 초기값 필드, 연결 특성, `Awake`가 필요하지 않습니다.

```csharp
public GuardedValue<int> health = 100;
public MaskedValue<float> moveSpeed = 5f;
public MaskedString playerName = "Player";
```

편집 모드에서는 초기값을 저장하고, 플레이 중에는 현재 값을 확인·수정합니다. 플레이 중 수정은 초기값에 저장되지 않습니다. UI와 리플렉션은 Editor 전용이며 값 접근 경로에는 추가 검사가 없습니다. 직렬화용 초기값 저장 공간과 로드 시 복원 처리는 빌드에도 필요하며 큰 구조체는 메모리가 증가할 수 있습니다. 로드한 초기값은 복제 시 재사용할 평문 설정 데이터로 남습니다. Unity 2022.3에서는 기존 `[MaskedValueField]` 연결 방식을 사용합니다.

Package Manager에서 **Inspector Fields** 샘플을 가져올 수 있습니다. 세부 설정과 제약은 [Inspector 가이드](Documentation~/Inspector.md)를 참고하세요. 초기값은 일반 직렬화 데이터이므로 씬·프리팹·빌드에 평문으로 저장됩니다.

## 개발 및 라이선스

검증·패키징 명령은 [영문 README](README.md#development), 태그와 OpenUPM 등록 절차는 [릴리스 가이드](https://github.com/achieveonepark/masked-value/blob/main/.github/RELEASING.md)를 참고하세요.

[MIT 라이선스](LICENSE.md) © 2026 achieveonepark.
