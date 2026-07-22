# Deucarian Pointer Capture

`com.deucarian.pointer-capture` owns the reusable lifecycle around mouse pointer capture. It handles browser pointer-lock state, desktop/editor cursor locking, release and loss cleanup, capture permission, rearming, diagnostics, and package configuration without owning application navigation behavior.

## Install

Install the package through the Deucarian Package Installer or add its Git URL to the Unity package manifest. The package requires `com.deucarian.editor` only for its editor management surface; the runtime assembly is input-system agnostic.

Open the package window at:

`Tools > Deucarian > Interaction > Pointer Capture`

Configuration, runtime status, validation, and fix actions all live in this one window.

## Runtime setup

Add `DeucarianPointerCaptureController` to the application object that coordinates navigation input. Capture permission is the conjunction of:

1. The current platform being supported.
2. The project settings asset allowing that platform.
3. The component allowing capture.
4. The runtime gate allowing capture.
5. The rearm gate having observed neutral input and a fresh action after a loss or cancellation.

```csharp
using Deucarian.PointerCapture;
using UnityEngine;

public sealed class ExampleNavigationInput : MonoBehaviour
{
    [SerializeField] private DeucarianPointerCaptureController capture;
    private readonly object owner = new object();

    private void Update()
    {
        bool neutral = !Input.GetMouseButton(1) && !Input.GetKey(KeyCode.Escape);
        bool freshAction = Input.GetMouseButtonDown(1);
        capture.UpdateInputRearming(neutral, freshAction);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            capture.NotifyEscapePressed();
        }

        if (freshAction)
        {
            capture.RequestCapture(owner);
        }

        if (Input.GetMouseButtonUp(1))
        {
            capture.ReleaseCapture(owner);
        }
    }
}
```

The package deliberately does not choose mouse buttons, movement keys, drag thresholds, or camera behavior. Consumers can use Legacy Input, the Input System package, or another source and feed neutral/fresh-action observations into the same lifecycle API.

## State and diagnostics

`DeucarianPointerCaptureState` exposes `Idle`, `Requested`, `Active`, `Rejected`, and `Lost`. Subscribe to `StateChanged` for transitions, inspect `LastReleaseReason` and `LastMessage`, or call `GetDiagnosticsSnapshot()` for a compact status record suitable for an in-app diagnostics page.

WebGL requests must still originate from an eligible user action. Request capture from the same Unity input update in which the application observes the initiating click. The bridge listens for browser `pointerlockchange`, `pointerlockerror`, page visibility, and window blur events and reports the resulting state back to C#.

The package disables Unity's sticky WebGL cursor-lock mode so Unity follows the browser when lock is released instead of retaining or reasserting a centered lock state.

## Application boundary

Keep orbit/fly modes, camera movement, drag thresholds, input bindings, UI blocking, and selection coordination in the application or their respective capability packages. Pointer Capture owns only permission and lifecycle around the cursor/pointer lock.
