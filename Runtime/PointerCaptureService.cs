using System;
using UnityEngine;

namespace Deucarian.PointerCapture
{
    internal sealed class PointerCaptureService : IDisposable
    {
        private bool allowCapture = true;
        private bool hideCursor = true;
        private bool restorePointerPositionOnRelease = true;
        private bool requireNeutralInputBeforeRearming = true;
        private DeucarianPointerCaptureReleasePolicy releasePolicy =
            DeucarianPointerCaptureReleasePolicy.All;

        private readonly IPointerCapturePlatform platform;
        private bool disposed, enabled;
        internal PointerCaptureService(IPointerCapturePlatform platform)
        { this.platform = platform ?? throw new ArgumentNullException(nameof(platform)); }

        internal void Configure(bool allowed, bool hide, bool restore, bool neutral, DeucarianPointerCaptureReleasePolicy release)
        {
            allowCapture = allowed; hideCursor = hide; restorePointerPositionOnRelease = restore;
            requireNeutralInputBeforeRearming = neutral; releasePolicy = release;
        }
        internal bool IsOwnedBy(object candidate) => !disposed && ReferenceEquals(owner, candidate);
        internal bool HasOwner => owner != null;
        public void Dispose() { if (disposed) return; disposed = true; Disable(); StateChanged = null; }

        private readonly DeucarianPointerCaptureRearmGate rearmGate =
            new DeucarianPointerCaptureRearmGate();

        private object owner;
        private bool runtimeCaptureAllowed = true;
        private bool applicationHasFocus = true;
        private bool applicationPaused;
        private CursorLockMode previousCursorLockMode = CursorLockMode.None;
        private bool previousCursorVisibility = true;
        private Vector2 pointerPositionBeforeCapture;
        private bool hasPointerPositionBeforeCapture;
        private DeucarianPointerCaptureState state = DeucarianPointerCaptureState.Idle;
        private DeucarianPointerCaptureReleaseReason lastReleaseReason =
            DeucarianPointerCaptureReleaseReason.None;
        private string lastMessage = "Ready.";

        public event EventHandler<DeucarianPointerCaptureStateChangedEventArgs> StateChanged;

        public DeucarianPointerCaptureState State => state;

        public DeucarianPointerCaptureReleaseReason LastReleaseReason => lastReleaseReason;

        public string LastMessage => lastMessage;

        public bool ComponentCaptureAllowed => allowCapture;

        public bool RuntimeCaptureAllowed => runtimeCaptureAllowed;

        public bool DiagnosticsEnabled
        {
            get
            {
                DeucarianPointerCaptureProjectSettings settings =
                    DeucarianPointerCaptureProjectSettings.Load();
                return settings == null || settings.DiagnosticsEnabled;
            }
        }

        public bool IsInputRearmed => rearmGate.CanProcessInput;

        public bool IsCaptureActive => state == DeucarianPointerCaptureState.Active;

        public DeucarianPointerCapturePlatform Platform =>
            platform.CurrentPlatform;

        public bool ProjectCaptureAllowed =>
            DeucarianPointerCaptureProjectSettings.IsCurrentProjectAllowed(Platform);

        public bool EffectiveCaptureAllowed =>
            platform.IsSupported &&
            ProjectCaptureAllowed &&
            allowCapture &&
            runtimeCaptureAllowed;

        public bool CanRequestCapture => EffectiveCaptureAllowed && IsInputRearmed;

        internal void Enable()
        {
            if (disposed) return;
            enabled = true;
            applicationHasFocus = platform.HasFocus;
            rearmGate.SetFocus(
                applicationHasFocus && !applicationPaused,
                requireNeutralInputBeforeRearming);
            platform.Initialize(
                IncludesReleaseReason(DeucarianPointerCaptureReleasePolicy.PageHidden));
        }

        internal void Tick()
        {
            if (disposed || !enabled) return;
            if ((state == DeucarianPointerCaptureState.Requested ||
                 state == DeucarianPointerCaptureState.Active) &&
                !EffectiveCaptureAllowed)
            {
                DeucarianPointerCaptureReleaseReason reason = GetPolicyFailureReason();
                ReleaseInternal(
                    reason,
                    "Capture permission changed while the pointer was captured.",
                    true,
                    DeucarianPointerCaptureState.Idle);
                return;
            }

            if (state != DeucarianPointerCaptureState.Requested &&
                state != DeucarianPointerCaptureState.Active)
            {
                return;
            }

            ApplyPlatformStatus(platform.Observe());
        }

        public bool RequestCapture(object captureOwner)
        {
            if (disposed || !enabled) return false;
            if (captureOwner == null)
            {
                Reject(
                    DeucarianPointerCaptureReleaseReason.InvalidRequest,
                    "A non-null capture owner is required.");
                return false;
            }

            if (!platform.IsSupported)
            {
                Reject(
                    DeucarianPointerCaptureReleaseReason.UnsupportedPlatform,
                    "Pointer capture is not supported on the current platform.");
                return false;
            }

            if (!ProjectCaptureAllowed)
            {
                Reject(
                    DeucarianPointerCaptureReleaseReason.ProjectPolicyChanged,
                    "Project policy does not allow pointer capture on the current platform.");
                return false;
            }

            if (!allowCapture)
            {
                Reject(
                    DeucarianPointerCaptureReleaseReason.ComponentPolicyChanged,
                    "This component does not allow pointer capture.");
                return false;
            }

            if (!runtimeCaptureAllowed)
            {
                Reject(
                    DeucarianPointerCaptureReleaseReason.RuntimePolicyChanged,
                    "The runtime capture gate is closed.");
                return false;
            }

            if (!IsInputRearmed)
            {
                lastMessage = "Capture is waiting for neutral input and a fresh action.";
                return false;
            }

            if (owner != null && ReferenceEquals(owner, captureOwner))
            {
                return state == DeucarianPointerCaptureState.Requested ||
                       state == DeucarianPointerCaptureState.Active;
            }

            if (owner != null)
            {
                ReleaseInternal(
                    DeucarianPointerCaptureReleaseReason.OwnershipChanged,
                    "Pointer capture moved to a different owner.",
                    false,
                    DeucarianPointerCaptureState.Idle);
            }

            owner = captureOwner;
            previousCursorLockMode = platform.LockState;
            previousCursorVisibility = platform.CursorVisible;
            hasPointerPositionBeforeCapture =
                restorePointerPositionOnRelease &&
                platform.TryGetPointerPosition(
                    out pointerPositionBeforeCapture);
            SetState(
                DeucarianPointerCaptureState.Requested,
                DeucarianPointerCaptureReleaseReason.None,
                "Pointer capture was requested.");

            if (disposed || !enabled || !ReferenceEquals(owner, captureOwner)) return false;

            platform.Initialize(
                IncludesReleaseReason(DeucarianPointerCaptureReleasePolicy.PageHidden));
            DeucarianPointerCapturePlatformStatus status =
                platform.Request(hideCursor);
            ApplyPlatformStatus(status);
            return state == DeucarianPointerCaptureState.Requested ||
                   state == DeucarianPointerCaptureState.Active;
        }

        public bool ReleaseCapture(object captureOwner = null)
        {
            if (disposed) return false;
            if (captureOwner != null && owner != null && !ReferenceEquals(owner, captureOwner))
            {
                return false;
            }

            if (owner == null &&
                state != DeucarianPointerCaptureState.Requested &&
                state != DeucarianPointerCaptureState.Active)
            {
                return false;
            }

            ReleaseInternal(
                DeucarianPointerCaptureReleaseReason.Explicit,
                "Pointer capture was released.",
                false,
                DeucarianPointerCaptureState.Idle);
            return true;
        }

        public void SetComponentCaptureAllowed(bool allowed)
        {
            if (allowCapture == allowed)
            {
                return;
            }

            allowCapture = allowed;
            if (!allowed &&
                (state == DeucarianPointerCaptureState.Requested ||
                 state == DeucarianPointerCaptureState.Active))
            {
                ReleaseInternal(
                    DeucarianPointerCaptureReleaseReason.ComponentPolicyChanged,
                    "Component capture permission was disabled.",
                    true,
                    DeucarianPointerCaptureState.Idle);
            }
        }

        public void SetRuntimeCaptureAllowed(bool allowed)
        {
            if (runtimeCaptureAllowed == allowed)
            {
                return;
            }

            runtimeCaptureAllowed = allowed;
            if (!allowed &&
                (state == DeucarianPointerCaptureState.Requested ||
                 state == DeucarianPointerCaptureState.Active))
            {
                ReleaseInternal(
                    DeucarianPointerCaptureReleaseReason.RuntimePolicyChanged,
                    "Runtime capture permission was disabled.",
                    true,
                    DeucarianPointerCaptureState.Idle);
            }
        }

        public void UpdateInputRearming(bool isInputNeutral, bool hasNewCaptureAction)
        {
            rearmGate.Refresh(isInputNeutral, hasNewCaptureAction);
        }

        public void NotifyEscapePressed()
        {
            if (!IncludesReleaseReason(DeucarianPointerCaptureReleasePolicy.Escape))
            {
                return;
            }

            ReleaseInternal(
                DeucarianPointerCaptureReleaseReason.Escape,
                "Escape canceled pointer capture.",
                true,
                DeucarianPointerCaptureState.Idle);
        }

        public DeucarianPointerCaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()
        {
            bool diagnosticsEnabled = DiagnosticsEnabled;
            return new DeucarianPointerCaptureDiagnosticsSnapshot(
                state,
                Platform,
                platform.IsSupported,
                ProjectCaptureAllowed,
                allowCapture,
                runtimeCaptureAllowed,
                IsInputRearmed,
                diagnosticsEnabled,
                diagnosticsEnabled ? PointerCaptureOwnerDescription.Describe(owner) : string.Empty,
                lastReleaseReason,
                diagnosticsEnabled ? lastMessage : string.Empty);
        }

        internal void SetFocus(bool hasFocus)
        {
            applicationHasFocus = hasFocus;
            rearmGate.SetFocus(
                applicationHasFocus && !applicationPaused,
                requireNeutralInputBeforeRearming);
            if (!hasFocus && IncludesReleaseReason(DeucarianPointerCaptureReleasePolicy.FocusLost))
            {
                ReleaseInternal(
                    DeucarianPointerCaptureReleaseReason.FocusLost,
                    "Application focus was lost.",
                    true,
                    DeucarianPointerCaptureState.Lost);
            }
        }

        internal void SetPaused(bool isPaused)
        {
            applicationPaused = isPaused;
            rearmGate.SetFocus(
                applicationHasFocus && !applicationPaused,
                requireNeutralInputBeforeRearming);
            if (isPaused && IncludesReleaseReason(DeucarianPointerCaptureReleasePolicy.ApplicationPaused))
            {
                ReleaseInternal(
                    DeucarianPointerCaptureReleaseReason.ApplicationPaused,
                    "The application was paused.",
                    true,
                    DeucarianPointerCaptureState.Lost);
            }
        }

        internal void Disable()
        {
            enabled = false;
            ReleaseInternal(
                DeucarianPointerCaptureReleaseReason.ComponentDisabled,
                "The pointer capture component was disabled.",
                false,
                DeucarianPointerCaptureState.Idle);
        }

        private void ApplyPlatformStatus(DeucarianPointerCapturePlatformStatus status)
        {
            switch (status)
            {
                case DeucarianPointerCapturePlatformStatus.Requested:
                    SetState(
                        DeucarianPointerCaptureState.Requested,
                        DeucarianPointerCaptureReleaseReason.None,
                        "The platform is processing the pointer capture request.");
                    break;
                case DeucarianPointerCapturePlatformStatus.Active:
                    SetState(
                        DeucarianPointerCaptureState.Active,
                        DeucarianPointerCaptureReleaseReason.None,
                        "Pointer capture is active.");
                    break;
                case DeucarianPointerCapturePlatformStatus.Rejected:
                    ReleaseInternal(
                        DeucarianPointerCaptureReleaseReason.BrowserRejected,
                        "The platform rejected the pointer capture request.",
                        true,
                        DeucarianPointerCaptureState.Rejected);
                    break;
                case DeucarianPointerCapturePlatformStatus.PageHidden:
                    ReleaseInternal(
                        DeucarianPointerCaptureReleaseReason.PageHidden,
                        "The page became hidden while pointer capture was active.",
                        true,
                        DeucarianPointerCaptureState.Lost);
                    break;
                case DeucarianPointerCapturePlatformStatus.Lost:
                case DeucarianPointerCapturePlatformStatus.Idle:
                    bool shouldBlock = IncludesReleaseReason(
                        DeucarianPointerCaptureReleasePolicy.LockLost);
                    ReleaseInternal(
                        DeucarianPointerCaptureReleaseReason.LockLost,
                        "The platform pointer lock was lost.",
                        shouldBlock,
                        DeucarianPointerCaptureState.Lost);
                    break;
            }
        }

        private void Reject(
            DeucarianPointerCaptureReleaseReason reason,
            string message)
        {
            if (owner != null) { lastMessage = message; return; }
            SetState(DeucarianPointerCaptureState.Rejected, reason, message);
        }

        private void ReleaseInternal(
            DeucarianPointerCaptureReleaseReason reason,
            string message,
            bool blockUntilNewAction,
            DeucarianPointerCaptureState targetState)
        {
            bool ownsCapture = owner != null ||
                               state == DeucarianPointerCaptureState.Requested ||
                               state == DeucarianPointerCaptureState.Active;
            if (ownsCapture)
            {
                platform.Release(
                    previousCursorLockMode,
                    previousCursorVisibility,
                    hasPointerPositionBeforeCapture && ShouldRestorePointerPosition(reason),
                    pointerPositionBeforeCapture);
            }

            owner = null;
            hasPointerPositionBeforeCapture = false;
            if (blockUntilNewAction)
            {
                rearmGate.BlockUntilNewAction(requireNeutralInputBeforeRearming);
            }

            if (!ownsCapture && targetState == DeucarianPointerCaptureState.Lost)
            {
                targetState = DeucarianPointerCaptureState.Idle;
            }

            SetState(targetState, reason, message);
        }

        private static bool ShouldRestorePointerPosition(
            DeucarianPointerCaptureReleaseReason reason)
        {
            return reason != DeucarianPointerCaptureReleaseReason.FocusLost &&
                   reason != DeucarianPointerCaptureReleaseReason.ApplicationPaused &&
                   reason != DeucarianPointerCaptureReleaseReason.PageHidden;
        }

        private DeucarianPointerCaptureReleaseReason GetPolicyFailureReason()
        {
            if (!platform.IsSupported)
            {
                return DeucarianPointerCaptureReleaseReason.UnsupportedPlatform;
            }

            if (!ProjectCaptureAllowed)
            {
                return DeucarianPointerCaptureReleaseReason.ProjectPolicyChanged;
            }

            if (!allowCapture)
            {
                return DeucarianPointerCaptureReleaseReason.ComponentPolicyChanged;
            }

            return DeucarianPointerCaptureReleaseReason.RuntimePolicyChanged;
        }

        private void SetState(
            DeucarianPointerCaptureState nextState,
            DeucarianPointerCaptureReleaseReason reason,
            string message)
        {
            DeucarianPointerCaptureState previousState = state;
            state = nextState;
            lastReleaseReason = reason;
            lastMessage = message ?? string.Empty;

            if (previousState == nextState)
            {
                return;
            }

            StateChanged?.Invoke(
                this,
                new DeucarianPointerCaptureStateChangedEventArgs(
                    previousState,
                    nextState,
                    reason,
                    lastMessage));
        }

        private bool IncludesReleaseReason(DeucarianPointerCaptureReleasePolicy reason)
        {
            return (releasePolicy & reason) == reason;
        }

    }
}
