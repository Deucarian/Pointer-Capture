using System;

namespace Deucarian.PointerCapture
{
    public enum DeucarianPointerCaptureState
    {
        Idle,
        Requested,
        Active,
        Rejected,
        Lost
    }

    public enum DeucarianPointerCapturePlatform
    {
        Unsupported,
        Editor,
        Standalone,
        WebGL
    }

    public enum DeucarianPointerCaptureReleaseReason
    {
        None,
        Explicit,
        Escape,
        FocusLost,
        ApplicationPaused,
        PageHidden,
        ComponentDisabled,
        ProjectPolicyChanged,
        ComponentPolicyChanged,
        RuntimePolicyChanged,
        OwnershipChanged,
        InvalidRequest,
        UnsupportedPlatform,
        BrowserRejected,
        LockLost
    }

    [Flags]
    public enum DeucarianPointerCaptureReleasePolicy
    {
        None = 0,
        Escape = 1 << 0,
        FocusLost = 1 << 1,
        ApplicationPaused = 1 << 2,
        PageHidden = 1 << 3,
        LockLost = 1 << 4,
        All = Escape | FocusLost | ApplicationPaused | PageHidden | LockLost
    }

    public sealed class DeucarianPointerCaptureStateChangedEventArgs : EventArgs
    {
        public DeucarianPointerCaptureStateChangedEventArgs(
            DeucarianPointerCaptureState previousState,
            DeucarianPointerCaptureState currentState,
            DeucarianPointerCaptureReleaseReason reason,
            string message)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason;
            Message = message ?? string.Empty;
        }

        public DeucarianPointerCaptureState PreviousState { get; }

        public DeucarianPointerCaptureState CurrentState { get; }

        public DeucarianPointerCaptureReleaseReason Reason { get; }

        public string Message { get; }
    }

    public readonly struct DeucarianPointerCaptureDiagnosticsSnapshot
    {
        public DeucarianPointerCaptureDiagnosticsSnapshot(
            DeucarianPointerCaptureState state,
            DeucarianPointerCapturePlatform platform,
            bool platformSupported,
            bool projectAllowed,
            bool componentAllowed,
            bool runtimeAllowed,
            bool inputRearmed,
            bool diagnosticsEnabled,
            string owner,
            DeucarianPointerCaptureReleaseReason lastReleaseReason,
            string message)
        {
            State = state;
            Platform = platform;
            PlatformSupported = platformSupported;
            ProjectAllowed = projectAllowed;
            ComponentAllowed = componentAllowed;
            RuntimeAllowed = runtimeAllowed;
            InputRearmed = inputRearmed;
            DiagnosticsEnabled = diagnosticsEnabled;
            Owner = owner ?? string.Empty;
            LastReleaseReason = lastReleaseReason;
            Message = message ?? string.Empty;
        }

        public DeucarianPointerCaptureState State { get; }

        public DeucarianPointerCapturePlatform Platform { get; }

        public bool PlatformSupported { get; }

        public bool ProjectAllowed { get; }

        public bool ComponentAllowed { get; }

        public bool RuntimeAllowed { get; }

        public bool InputRearmed { get; }

        public bool DiagnosticsEnabled { get; }

        public string Owner { get; }

        public DeucarianPointerCaptureReleaseReason LastReleaseReason { get; }

        public string Message { get; }

        public bool EffectiveCaptureAllowed =>
            PlatformSupported && ProjectAllowed && ComponentAllowed && RuntimeAllowed;
    }
}
