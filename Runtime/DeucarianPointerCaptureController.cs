using System;
using Deucarian.Diagnostics;
using UnityEngine;

namespace Deucarian.PointerCapture
{
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    public sealed class DeucarianPointerCaptureController : MonoBehaviour
    {
        [SerializeField] private bool allowCapture = true;
        [SerializeField] private bool hideCursor = true;
        [SerializeField] private bool restorePointerPositionOnRelease = true;
        [SerializeField] private bool requireNeutralInputBeforeRearming = true;
        [SerializeField] private DeucarianPointerCaptureReleasePolicy releasePolicy = DeucarianPointerCaptureReleasePolicy.All;
        private PointerCaptureService service;
        private DiagnosticProviderRegistration diagnostics;
        private bool followsProjectDefaults;
        internal void FollowProjectDefaults() => followsProjectDefaults = true;

        internal PointerCaptureService Service
        {
            get
            {
                bool created = service == null;
                if (created) service = new PointerCaptureService(new PointerCapturePlatform());
                var settings = followsProjectDefaults ? DeucarianPointerCaptureProjectSettings.Load() : null;
                service.Configure(allowCapture, settings != null ? settings.HideCursor : hideCursor,
                    settings != null ? settings.RestorePointerPositionOnRelease : restorePointerPositionOnRelease,
                    settings != null ? settings.RequireNeutralInputBeforeRearming : requireNeutralInputBeforeRearming,
                    settings != null ? settings.ReleasePolicy : releasePolicy);
                if (created && isActiveAndEnabled) service.Enable();
                return service;
            }
        }

        public event EventHandler<DeucarianPointerCaptureStateChangedEventArgs> StateChanged
        { add => Service.StateChanged += value; remove { if (service != null) service.StateChanged -= value; } }
        public DeucarianPointerCaptureState State => Service.State;
        public DeucarianPointerCaptureReleaseReason LastReleaseReason => Service.LastReleaseReason;
        public string LastMessage => Service.LastMessage;
        public bool ComponentCaptureAllowed => allowCapture;
        public bool RuntimeCaptureAllowed => Service.RuntimeCaptureAllowed;
        public bool DiagnosticsEnabled => Service.DiagnosticsEnabled;
        public bool IsInputRearmed => Service.IsInputRearmed;
        public bool IsCaptureActive => Service.IsCaptureActive;
        public DeucarianPointerCapturePlatform Platform => Service.Platform;
        public bool ProjectCaptureAllowed => Service.ProjectCaptureAllowed;
        public bool EffectiveCaptureAllowed => Service.EffectiveCaptureAllowed;
        public bool CanRequestCapture => isActiveAndEnabled && Service.CanRequestCapture;

        public IPointerCaptureSession OpenSession(object owner = null) => new PointerCaptureSession(Service, owner);
        public bool RequestCapture(object owner) => isActiveAndEnabled && Service.RequestCapture(owner);
        public bool ReleaseCapture(object owner = null) => Service.ReleaseCapture(owner);
        public void SetComponentCaptureAllowed(bool allowed)
        { allowCapture = allowed; Service.SetComponentCaptureAllowed(allowed); if (!allowed) Service.Tick(); }
        public void SetRuntimeCaptureAllowed(bool allowed) => Service.SetRuntimeCaptureAllowed(allowed);
        public void UpdateInputRearming(bool neutral, bool freshAction) => Service.UpdateInputRearming(neutral, freshAction);
        public void NotifyEscapePressed() => Service.NotifyEscapePressed();
        public DeucarianPointerCaptureDiagnosticsSnapshot GetDiagnosticsSnapshot() => Service.GetDiagnosticsSnapshot();

        private void OnEnable()
        {
            Service.Enable();
            if (diagnostics == null) diagnostics = DiagnosticProviderRegistry.Register(new PointerCaptureDiagnosticProvider(this));
        }
        private void Update() => Service.Tick();
        private void OnApplicationFocus(bool hasFocus) => Service.SetFocus(hasFocus);
        private void OnApplicationPause(bool paused) => Service.SetPaused(paused);
        private void OnDisable() { if (service != null) service.Disable(); diagnostics?.Dispose(); diagnostics = null; }
        private void OnDestroy() { diagnostics?.Dispose(); diagnostics = null; service?.Dispose(); service = null; }
    }
}
