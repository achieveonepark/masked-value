---
name: achieve-masked-values
description: Implement, extend, integrate, or debug Achieve Masked Values (com.achieve.masked-values) in Unity or C#. Use for 값 은닉, 마스킹, HP or gold integrity checks, MaskedValue, GuardedValue, MaskedString, MaskedData, explicit span codecs, and low-allocation runtime state, including related requests that do not name this skill. Excludes cryptographic encryption, save-file encryption, and server-authoritative anti-cheat systems.
---

# Achieve Masked Values

게임 상태의 런타임 값 은닉과 선택적 무결성 검사를 구현하거나 패키지 자체를 변경할 때 사용한다.

## 패키지 찾기

- 패키지 ID는 `com.achieve.masked-values`, 네임스페이스와 어셈블리는 `Achieve.MaskedValues`다. 다른 두 패키지와 달리 ID에 `achieve`를 사용한다. GitHub 저장소 이름과 UPM ID를 혼동하지 않는다.
- 현재 프로젝트에 설치된 버전과 게임 asmdef를 확인한다. 알려진 원본은 `C:/Users/parka/orca/projects/ts1/com.achieve.masked-values`다.
- 타입 선택·codec·성능 또는 내부 구현을 작업할 때 [구현 가이드](references/package-guide.md)를 읽는다. 패키지 `README.md`, `Samples~/ClassFields`와 실제 소스가 기준이다.

## 구현 기준

- `unmanaged` 값의 은닉은 `MaskedValue<T>`, 저장 상태 검증도 필요하면 `GuardedValue<T>`를 선택한다. 문자열은 `MaskedString`, 참조를 포함하는 데이터는 명시적 `ISpanValueCodec<T>`와 `MaskedData<T>`를 사용한다.
- Unity 컴포넌트 필드는 `[NonSerialized]`로 두고 게임 초기화 단계에서 설정한다. Inspector·저장 파일·도메인 리로드용 직렬화를 이 래퍼가 제공한다고 가정하지 않는다. 래퍼를 Unity 직렬화 필드로 추가하지 않는다.
- `.Value`로 복원해 계산한 뒤 한 번 대입한다. 래퍼가 구조체이므로 복사본이나 속성에서 얻은 임시 값만 변경하지 않는다. 구조체 데이터는 읽은 값을 수정하고 다시 대입한다.
- `TryGetValue` 실패 시 반환되는 기본값을 정상 게임 상태로 사용하지 않는다. `RefreshMask`는 정상 상태를 검증하고 값을 유지하며 표현만 변경한다. 요구가 없으면 매 읽기마다 재마스킹하지 않는다.
- 패키지는 XOR 기반 값 은닉과 경량 검증이다. 암호학적 인증, 코드 패치 차단, 게임 규칙 검증으로 설명하지 않는다. 정상 setter를 두 번 부르는 중복 차감은 Event Tracer 등 게임 진단 코드로 확인한다.
- `Runtime/Achieve.MaskedValues.asmdef`의 unsafe 및 엔진 비의존 구성을 유지한다. 관리형 자동 필드 탐색·리플렉션·JsonUtility 기반 직렬화를 core 경로에 추가하지 않는다. 검증 결과를 근거 없이 IL2CPP 성능 보장으로 확대하지 않는다.
- 다른 두 패키지와 함께 구현할 때 관련 스킬도 적용하고 연동은 게임 코드에 둔다. 이 패키지에 추적기나 북마크 런타임 의존성을 추가하지 않는다.

## 확인

정상 읽기·쓰기·복사·기본값, guarded 변조 검사와 실패 후 상태 유지 중 영향을 받는 동작을 검증한다. 성능을 바꿨다면 첫 초기화 비용과 steady-state 할당·시간을 구분한다. 기존 검증은 `Tests~/Verification`, `Tests~/SmokeTests`, `Tools~/verify.ps1`에 있다.

Unity가 파일을 무시할 때는 C# 코드뿐 아니라 `.meta` GUID와 asmdef 포함 여부도 확인한다. 기존 GUID는 보존하고 잘못된 GUID만 수정한다. 일반 사용에는 사용자 게임 코드에서 unsafe를 작성할 필요가 없다.
