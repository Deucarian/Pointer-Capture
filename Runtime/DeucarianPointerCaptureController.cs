using System;
using UnityEngine;

namespace Deucarian.PointerCapture
{
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    public sealed class DeucarianPointerCaptureController : MonoBehaviour
    {
        [SerializeField] private bool allowCapture = true;
        [SerializeField] private bool hideCursor = true;
        [SerializeField] private bool requireNeutralInputBeforeRearming = true;
        [SerializeField] private DeucarianPointerCaptureReleasePolicy releasePolicy =
            DeucarianPointerCaptureReleasePolicy.All;

        private readonly DeucarianPointerCaptureRearmGate rearmGate =
            new DeucarianPointerCaptureRearmGate();

        private object owner;
        private bool runtimeCaptureAllowed = true;
        private bool applicationHasFocus = true;
        private bool applicationPaused;
        private CursorLockMode previousCursorLockMode = CursorLockMode.None;
        private bool previousCursorVisibility = true;
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
            DeucarianPointerCapturePlatformBridge.CurrentPlatform;

        public bool ProjectCaptureAllowed =>
            DeucarianPointerCaptureProjectSettings.IsCurrentProjectAllowed(Platform);

        public bool EffectiveCaptureAllowed =>
            DeucarianPointerCapturePlatformBridge.IsSupported &&
            ProjectCaptureAllowed &&
            allowCapture &&
            runtimeCaptureAllowed;

        public bool CanRequestCapture => EffectiveCaptureAllowed && IsInputRearmed;

        private void OnEnable()
        {
            applicationHasFocus = Application.isFocused;
            rearmGate.SetFocus(
                applicationHasFocus && !applicationPaused,
                requireNeutralInputBeforeRearming);
            DeucarianPointerCapturePlatformBridge.Initialize(
                IncludesReleaseReason(DeucarianPointerCaptureReleasePolicy.PageHidden));
        }

        private void Update()
        {
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

            ApplyPlatformStatus(DeucarianPointerCapturePlatformBridge.Observe());
        }

        public bool RequestCapture(object captureOwner)
        {
            if (captureOwner == null)
            {
                Reject(
                    DeucarianPointerCaptureReleaseReason.InvalidRequest,
                    "A non-null capture owner is required.");
                return false;
            }

            if (!DeucarianPointerCapturePlatformBridge.IsSupported)
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

            if (owner != null && Equals(owner, captureOwner))
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
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisibility = Cursor.visible;
            SetState(
                DeucarianPointerCaptureState.Requested,
                DeucarianPointerCaptureReleaseReason.None,
                "Pointer capture was requested.");

            DeucarianPointerCapturePlatformBridge.Initialize(
                IncludesReleaseReason(DeucarianPointerCaptureReleasePolicy.PageHidden));
            DeucarianPointerCapturePlatformStatus status =
                DeucarianPointerCapturePlatformBridge.Request(hideCursor);
            ApplyPlatformStatus(status);
            return state == DeucarianPointerCaptureState.Requested ||
                   state == DeucarianPointerCaptureState.Active;
        }

        public bool ReleaseCapture(object captureOwner = null)
        {
            if (captureOwner != null && owner != null && !Equals(owner, captureOwner))
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
                DeucarianPointerCapturePlatformBridge.IsSupported,
                ProjectCaptureAllowed,
                allowCapture,
                runtimeCaptureAllowed,
                IsInputRearmed,
                diagnosticsEnabled,
                diagnosticsEnabled ? DescribeOwner(owner) : string.Empty,
                lastReleaseReason,
                diagnosticsEnabled ? lastMessage : string.Empty);
        }

        private void OnApplicationFocus(bool hasFocus)
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

        private void OnApplicationPause(bool isPaused)
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

        private void OnDisable()
        {
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
            owner = null;
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
                DeucarianPointerCapturePlatformBridge.Release(
                    previousCursorLockMode,
                    previousCursorVisibility);
            }

            owner = null;
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

        private DeucarianPointerCaptureReleaseReason GetPolicyFailureReason()
        {
            if (!DeucarianPointerCapturePlatformBridge.IsSupported)
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

        private static string DescribeOwner(object captureOwner)
        {
            if (captureOwner == null)
            {
                return string.Empty;
            }

            UnityEngine.Object unityObject = captureOwner as UnityEngine.Object;
            if (unityObject != null)
            {
                return unityObject.name;
            }

            return captureOwner.GetType().Name;
        }
    }
}
