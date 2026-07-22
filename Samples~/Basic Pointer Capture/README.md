# Basic Pointer Capture

1. Create a scene GameObject.
2. Add `BasicPointerCaptureSample`; Unity also adds `DeucarianPointerCaptureController`.
3. Enter Play Mode and press the right mouse button to request capture.
4. Release the button to release capture, or press Escape to cancel and require neutral input plus a fresh action.

The sample uses Unity's legacy `Input` API only as an application-side input source. The pointer capture package itself has no dependency on either Unity input system.

