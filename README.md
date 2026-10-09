# Masked Values

런타임 값 은닉과 선택적 변조 감지를 제공하는 Unity 패키지입니다. XOR 기반 값 은닉이며 암호학적 보안 기능은 아닙니다.

## 설치

Unity **2022.3 이상**에서 Package Manager → **+ → Add package from git URL**:

```text
https://github.com/achieveonepark/masked-value.git
```

## 사용

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
        health = 100;
        speed = 5f;
    }

    public void TakeDamage(int amount) => health.Value -= amount;
}
```

`MaskedValue<T>`는 `unmanaged` 값을 은닉하고, `GuardedValue<T>`는 무결성 검사도 수행합니다. 문자열은 `MaskedString`, 참조를 포함한 데이터는 `MaskedData<T>`와 명시적 codec을 사용합니다. Unity 직렬화에는 사용하지 않습니다.

샘플: Package Manager의 **Class Fields**를 Import합니다.

자세한 API·제약은 [사용 가이드](Documentation~/masked-values.md), 성능 측정은 [성능 문서](Documentation~/Performance.md)를 참고하세요.

## AI 스킬

스킬 원본은 [`Skills~/achieve-masked-values`](Skills~/achieve-masked-values/SKILL.md)에 있습니다. Unity Skill Manager가 프로젝트의 `.agents/skills`와 `.claude/skills`에 취합합니다. 직접 호출: Codex `$achieve-masked-values`, Claude Code `/achieve-masked-values`.
