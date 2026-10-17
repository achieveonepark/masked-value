# Tamper events and game shutdown

`GuardedValueDiagnostics.TamperingDetected` is a global `Action<Type>` event. Subscribe when the game's persistent bootstrap starts and unsubscribe when it stops. This project's default response is to stop normal gameplay and request game shutdown on detection. The package itself does not terminate applications automatically.

## When detection happens

The event runs synchronously whenever a `GuardedValue<T>` integrity check fails. Checks occur during `.Value` reads/writes, implicit reads, arithmetic using guarded operands, `TryGetValue`, `TrySetValue`, `CheckIntegrity`, `RefreshMask`, and `TryRefreshMask`. Healthy checks never raise the event.

This is the **detection time**, not the exact time another process overwrote memory. An unused value is not checked automatically. To monitor it, call `CheckIntegrity()` on the original field at a suitable interval or before a sensitive action. A change restored before the next check can go undetected. Do not scan all objects by reflection or poll every masked field each frame without measuring the cost.

The event reports only `typeof(T)`, not an object, field name, address, plaintext value, or the attacking process. Repeated failed checks produce repeated notifications. It runs on the thread performing the check, including worker threads. Handlers must not throw or read/remask/write the same corrupt value: that can recurse, and a handler exception can escape even from a `Try` method.

`MaskedValue<T>`, `MaskedString`, and `MaskedData<T>` do not provide integrity detection. Use `GuardedValue<T>` for unmanaged values whose corruption must trigger this response.

## Unity listener: quit on detection

Attach this component **once** to an active root object in the bootstrap scene, before guarded gameplay begins. Keep it enabled across scenes. It records detection without Unity API calls or allocations in the event handler, exposes a flag to stop normal game actions, and requests shutdown once on the main thread.

```csharp
using System;
using System.Threading;
using Achieve.MaskedValues;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class QuitOnMaskedValueTampering : MonoBehaviour
{
    private int quitRequested;
    private bool quitIssued;

    public bool ExitRequested => Volatile.Read(ref quitRequested) != 0;

    private void Awake() => DontDestroyOnLoad(gameObject);

    private void OnEnable()
    {
        quitIssued = false;
        Interlocked.Exchange(ref quitRequested, 0);
        GuardedValueDiagnostics.TamperingDetected += OnTampering;
    }

    private void OnDisable()
        => GuardedValueDiagnostics.TamperingDetected -= OnTampering;

    private void OnTampering(Type valueType)
    {
        // May run on a worker thread. Never read the corrupt value here.
        Interlocked.Exchange(ref quitRequested, 1);
    }

    private void Update()
    {
        if (quitIssued || !ExitRequested) return;
        quitIssued = true;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
```

When your game already has a persistent bootstrap and shutdown coordinator, integrate these hooks there instead of creating another singleton. Check `ExitRequested` before starting further game actions. `Update` receives the request on its next run; other code does not automatically stop when the flag is set.

On a failed `TryGetValue`, `TrySetValue`, or `CheckIntegrity`, **return from the current game action immediately**. Do not use the failed output's `default(T)` as valid state, catch an integrity exception and continue, silently replace the value, or save the corrupted session. Shutdown is the default response; recovery requires an explicit project decision and a trusted source.

The listener resets its request in `OnEnable` and unsubscribes in `OnDisable`. Verify the project's bootstrap and subscription lifecycle with its actual domain/scene reload settings; custom static managers must reset their flags and avoid stale or duplicate subscribers when reload is disabled.

[`Application.Quit`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application.Quit.html) is a platform-dependent shutdown request, not a synchronous process kill. The Editor ignores it, so the example stops Play mode with [`EditorApplication.isPlaying`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/EditorApplication-isPlaying.html). Never close the Unity Editor or kill processes as a substitute. On platforms where quitting is unsupported or discouraged, block further gameplay and end the session through the platform-appropriate flow. Check that existing quit/save handlers do not cancel the tamper response or persist suspect state.

## Serialization and protection limits

`[NonSerialized]` excludes a field from Unity serialization and the normal Inspector. It is not memory encryption and does not remove fields from the wrapper type. In Unity 6, loaded authoring `initialValue` fields remain plaintext configuration in memory and assets; changing them does not directly change the current masked value, but can affect future loading/cloning. Those fields are not covered by the runtime integrity tag and do not raise this event when changed. See the [Inspector guide](Inspector.md).

Integrity checks detect ordinary masked-storage/key/tag corruption. They do not detect every cheat: valid snapshot replay, complete erasure to the allowed default state, code/validation patches, forged valid state, or writes through legitimate setters remain possible. UI text, local copies, logs, and decoded string values can also expose plaintext. A tamper event is not proof of the identity or intent of the writer; do not automatically ban an account from this signal alone.

## Verify an integration

Use an isolated test value to corrupt storage, key, or tag, then trigger a check. Verify that the listener receives the value type, repeated failures request shutdown only once, a failed operation does not mutate or re-tag the bad value, and healthy operations do not request shutdown. Include worker-thread notification and subscription cleanup. Test the game action gate separately from actual application shutdown.

Do not inject corruption into the user's open scene or terminate their Editor to test this guide. Use a disposable test scene/player or replace the exit operation with a recording test adapter. Existing event tests are in `Tests~/Verification/VerificationSuite.cs`.
