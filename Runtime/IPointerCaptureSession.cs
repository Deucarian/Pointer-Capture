using System;

namespace Deucarian.PointerCapture
{
    /// <summary>A borrowed claim on pointer capture. Disposing a claim never destroys its service.</summary>
    public interface IPointerCaptureSession : IDisposable
    {
        event EventHandler<DeucarianPointerCaptureStateChangedEventArgs> StateChanged;
        DeucarianPointerCaptureState State { get; }
        bool RequestCapture(object owner);
        bool ReleaseCapture(object owner);
        void UpdateInputRearming(bool isInputNeutral, bool hasNewCaptureAction);
        void NotifyEscapePressed();
        DeucarianPointerCaptureDiagnosticsSnapshot GetDiagnosticsSnapshot();
    }
}
