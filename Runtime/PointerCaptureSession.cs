using System;

namespace Deucarian.PointerCapture
{
    internal sealed class PointerCaptureSession : IPointerCaptureSession
    {
        private readonly PointerCaptureService service;
        private object owner;
        private bool disposed, requesting;
        private DeucarianPointerCaptureState state;
        internal PointerCaptureSession(PointerCaptureService service, object owner)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.owner = owner;
            service.StateChanged += OnStateChanged;
        }
        public event EventHandler<DeucarianPointerCaptureStateChangedEventArgs> StateChanged;
        public DeucarianPointerCaptureState State => disposed ? DeucarianPointerCaptureState.Idle : state;
        public bool RequestCapture(object requester)
        {
            if (disposed || requester == null || (owner != null && !ReferenceEquals(requester, owner))) return false;
            owner = requester;
            requesting = true;
            try { return service.RequestCapture(this); }
            finally { requesting = false; }
        }
        public bool ReleaseCapture(object requester) => !disposed && ReferenceEquals(requester, owner) && service.ReleaseCapture(this);
        public void UpdateInputRearming(bool neutral, bool freshAction)
        { if (!disposed && (!service.HasOwner || service.IsOwnedBy(this))) service.UpdateInputRearming(neutral, freshAction); }
        public void NotifyEscapePressed()
        { if (!disposed && service.IsOwnedBy(this)) service.NotifyEscapePressed(); }
        public DeucarianPointerCaptureDiagnosticsSnapshot GetDiagnosticsSnapshot() => service.GetDiagnosticsSnapshot();
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            service.StateChanged -= OnStateChanged;
            StateChanged = null;
            service.ReleaseCapture(this);
        }
        private void OnStateChanged(object sender, DeucarianPointerCaptureStateChangedEventArgs args)
        {
            if (disposed) return;
            var next = requesting || service.IsOwnedBy(this) ? args.CurrentState : DeucarianPointerCaptureState.Idle;
            var previous = state;
            state = next;
            if (previous != next) StateChanged?.Invoke(this, new DeucarianPointerCaptureStateChangedEventArgs(previous, next, args.Reason, args.Message));
        }
    }
}
