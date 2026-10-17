---
name: achieve-masked-values
description: Integrate, modify, or debug Achieve Masked Values in Unity or C#, including Inspector serialization, low-allocation masking, and GuardedValue tamper listeners that shut down gameplay on detection. Use for this package's APIs and integration, not unrelated game security systems.
---

# Achieve Masked Values

게임 상태의 런타임 값 은닉과 선택적 무결성 검사를 구현하거나 패키지 자체를 변경할 때 사용한다.

## 패키지 찾기

- 패키지 ID는 `com.achieve.masked-values`, 네임스페이스와 어셈블리는 `Achieve.MaskedValues`다. GitHub 저장소 이름과 UPM ID를 혼동하지 않는다.
- 현재 프로젝트의 Packages/manifest.json과 게임 asmdef를 확인하여 설치된 패키지 경로와 버전을 찾는다. 고정된 로컬 경로를 가정하지 않는다.
- 타입 선택·codec·성능 또는 내부 구현을 작업할 때 [구현 가이드](references/package-guide.md)를 읽는다. 패키지 `README.md`, `Samples~/ClassFields`와 실제 소스가 기준이다.

## 변조 감지 시 게임 종료

게임에 패키지를 연동할 때는 아래를 기본 정책으로 적용한다. 사용자가 명시적으로 다른 대응을 요청하면 그 정책을 우선한다. 단순 설명·문서 작업에서 실행 중인 게임에 리스너를 몰래 설치하거나 종료시키지는 않는다.

- HP·재화 등 변조 감지가 필요한 unmanaged 값은 `GuardedValue<T>`를 사용한다. `MaskedValue<T>`·`MaskedString`·`MaskedData<T>`가 무결성 이벤트를 제공한다고 가정하지 않는다.
- 활성 부트스트랩에 `GuardedValueDiagnostics.TamperingDetected` 리스너를 한 번 연결한다. 감지하면 정상 게임 처리를 차단하고 **게임 종료를 요청**한다. 로그만 남기고 진행하거나, 손상값을 기본값으로 바꾸거나, 자동 복구로 종료를 대체하지 않는다.
- 이벤트는 외부 메모리 변경 순간이 아니라 **다음 무결성 검사 실패 시점**에 동기 호출된다. 값의 타입만 전달하고 객체·필드·주소는 제공하지 않는다. 사용하지 않는 중요 값은 원래 필드를 명시적으로 검사하며, 빈도는 접근 패턴과 비용에 맞춘다. 리플렉션으로 모든 필드를 매 프레임 탐색하지 않는다.
- 검사한 스레드에서 이벤트가 실행된다. 핸들러는 `Interlocked` 등으로 종료 플래그만 설정하고 예외·Unity API 호출·평문 로깅·같은 손상값의 재접근을 하지 않는다. 메인 스레드에서 중복 요청을 합쳐 한 번 종료한다.
- 플레이어는 `Application.Quit()`를 요청하고 Editor에서는 `#if UNITY_EDITOR`로 `EditorApplication.isPlaying = false`를 사용한다. Unity Editor 전체 종료, `Environment.Exit`, 프로세스 강제 종료는 사용하지 않는다. 종료 요청이 동기적으로 모든 실행을 멈춘다고 가정하지 않는다.
- `TryGetValue`·`TrySetValue`·`CheckIntegrity` 실패 시 해당 게임 동작에서 즉시 반환한다. 중앙 종료 플래그도 확인하여 다음 메인 스레드 처리 전까지 추가 구매·저장·상태 변경이 계속되지 않게 한다. 기존 저장/종료 핸들러가 손상 상태를 저장하거나 종료를 취소하지 않는지 확인한다.
- 구독/해제를 짝지으며 씬 전환에도 유지할 리스너의 소유자를 정한다. 프로젝트의 도메인/씬 리로드 설정에서 중복 구독·남은 플래그가 없는지 확인한다. 정적 매니저를 사용한다면 플레이 세션 초기화도 구현한다.
- 종료가 불가능하거나 플랫폼 정책상 부적절한 환경에서는 후속 플레이를 차단하고 플랫폼에 맞게 세션을 끝낸다. 검사 우회·정상 스냅샷 재생·전체 0 초기화·정상 setter 호출까지 차단한다고 설명하거나 이 이벤트 하나로 계정을 자동 제재하지 않는다.

전체 리스너 예제는 설치된 패키지 루트의 `Documentation~/Tampering.md`를 읽는다. 종료 테스트는 격리한 테스트 어댑터/일회용 플레이어에서 수행하고, 사용자의 열려 있는 Editor나 정상 게임 상태를 변조해 검증하지 않는다.

## 구현 기준

- `unmanaged` 값의 은닉은 `MaskedValue<T>`, 저장 상태 검증도 필요하면 `GuardedValue<T>`를 선택한다. 문자열은 `MaskedString`, 참조를 포함하는 데이터는 명시적 `ISpanValueCodec<T>`와 `MaskedData<T>`를 사용한다.
- Unity 6에서는 `public GuardedValue<int> health = 100;`처럼 기존 타입을 직접 직렬화한다. 초기값 필드·연결 특성·Awake를 추가하지 않는다. 런타임 전용 필드는 `[NonSerialized]`를 사용한다. 초기값만 저장되며 플레이 중 값은 저장되지 않는다. Unity 2022.3이나 수동 codec 초기화에는 선택적 MaskedValueField 연결을 사용한다. 자세한 제약은 Documentation~/Inspector.md를 확인한다.
- `[NonSerialized]`는 메모리 은닉이나 타입 내부 필드 제거가 아니다. Unity 6에서 로드한 `initialValue`는 메모리/에셋에 평문 설정 데이터로 남고 런타임 검증 태그의 대상이 아니다. 초기값을 바꾸면 현재 masked 값에 바로 반영되지는 않지만 향후 복원/복제에 영향을 줄 수 있다. 평문 백업·UI·로그·지역 복사본의 노출도 고려한다.
- 숫자 래퍼는 사칙연산·복합 대입·증감을 사용할 수 있다. 여러 계산이 필요하면 `.Value`를 한 번 읽고 계산하여 한 번 대입한다. 복사본이나 속성의 임시 값만 변경하지 않는다. 구조체 데이터는 수정 후 다시 대입한다.
- `TryGetValue` 실패 시 반환되는 기본값을 정상 게임 상태로 사용하지 않는다. `RefreshMask`는 정상 상태를 검증하고 값을 유지하며 표현만 변경한다. 요구가 없으면 매 읽기마다 재마스킹하지 않는다.
- 패키지는 XOR 기반 값 은닉과 경량 검증이다. 암호학적 인증, 코드 패치 차단, 게임 규칙 검증으로 설명하지 않는다. 정상 setter를 두 번 부르는 중복 차감은 Event Tracer 등 게임 진단 코드로 확인한다.
- unsafe 구성을 유지하고 값 접근 경로에 관리형 자동 필드 탐색·리플렉션·JsonUtility를 추가하지 않는다. Unity 6 직렬화용 필드·콜백은 조건부 컴파일하며 독립 .NET 빌드는 엔진에 의존하지 않는다. 초기값 저장 공간과 로드 시 복원 비용을 숨기지 않는다. 검증을 IL2CPP 성능 보장으로 확대하지 않는다.
- 종료 정책과 다른 패키지 연동은 게임 코드에 둔다. core 래퍼에 전역 자동 종료나 추적기·북마크 런타임 의존성을 추가하지 않는다.

## 확인

정상 읽기·쓰기·복사·기본값, guarded 변조 검사와 실패 후 상태 유지 중 영향을 받는 동작을 검증한다. 성능을 바꿨다면 첫 초기화 비용과 steady-state 할당·시간을 구분한다. 기존 검증은 `Tests~/Verification`, `Tests~/SmokeTests`, `Tools~/verify.ps1`에 있다.

게임 연동에서는 정상 상태의 종료 미발생, 손상 감지 알림, 반복 실패의 종료 중복 방지, 워커 스레드 통지, 구독 해제, 실패 후 게임 동작 중단을 확인한다. 구현하지 않은 자동 복구나 메모리 쓰기 순간의 감지를 검증했다고 보고하지 않는다.

Unity가 파일을 무시할 때는 C# 코드뿐 아니라 `.meta` GUID와 asmdef 포함 여부도 확인한다. 기존 GUID는 보존하고 잘못된 GUID만 수정한다. 일반 사용에는 사용자 게임 코드에서 unsafe를 작성할 필요가 없다.
