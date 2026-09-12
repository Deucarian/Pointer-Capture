# Deucarian Pointer Capture

## Asset selection and project defaults

Project defaults are configurable without a scene object. An optional controller picker inspects live state or legacy scene overrides; Play Mode selects an available controller only when there is exactly one. Ambiguous settings assets require repair before project defaults can be edited.

`com.deucarian.pointer-capture` owns the reusable lifecycle around mouse pointer capture. It handles browser pointer-lock state, desktop/editor cursor locking, release and loss cleanup, capture permission, rearming, diagnostics, and package configuration without owning application navigation behavior.

## Install

Install the package through the Deucarian Package Installer or add its Git URL to the Unity package manifest. The package requires `com.deucarian.editor` only for its editor management surface; the runtime assembly is input-system agnostic.

Open the package window at:

**Deucarian Control Center > Experience > Pointer Capture**

Configuration, runtime status, validation, and fix actions all live in this one window.

## Runtime setup

Create one `PointerCaptureScope` in the application's composition root. It owns
its persistent Unity host; callers borrow an `IPointerCaptureSession` from
`scope.OpenSession()`. Configure Viewer Navigation with that session before
initializing it. Disposing a session releases only its own capture; disposing
the application scope releases the host. Do not create one application scope
per viewer. Installation itself does not start cursor capture.

Existing scene controllers and unconfigured Viewer Navigation callers remain
supported by `PointerCaptureCompatibility.OpenSceneSession`. That compatibility
route remains scene-scoped; it is not an automatically shared global service.
New multi-scene applications should explicitly compose the shared scope.

### Existing scene-controller setup

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

On Editor and desktop players, the optional Input System adapter remembers the pointer position before capture and restores it after an intentional release. This adapter is enabled automatically when `com.unity.inputsystem` is installed, even when the application continues to use Legacy Input for its own controls. Focus loss, application pause, and page hiding deliberately skip cursor warping so the package never pulls the system pointer back into an inactive application.

## Application boundary

Keep orbit/fly modes, camera movement, drag thresholds, input bindings, UI blocking, and selection coordination in the application or their respective capability packages. Pointer Capture owns only permission and lifecycle around the cursor/pointer lock.
