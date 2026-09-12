using Deucarian.Diagnostics;

namespace Deucarian.PointerCapture
{
    internal sealed class PointerCaptureDiagnosticProvider : IDiagnosticProvider
    {
        private readonly DeucarianPointerCaptureController controller;
        internal PointerCaptureDiagnosticProvider(DeucarianPointerCaptureController controller) => this.controller = controller;
        public string ProviderId => "pointer-capture." + controller.GetInstanceID();
        public string DisplayName => "Pointer Capture";
        public void Collect(DiagnosticReportBuilder builder)
        {
            if (controller == null) return;
            var snapshot = controller.GetDiagnosticsSnapshot();
            builder.AddSection(ProviderId, DisplayName)
                .AddItem("state", "State", snapshot.State.ToString())
                .AddItem("platform", "Platform", snapshot.Platform.ToString())
                .AddItem("allowed", "Project allows capture", snapshot.ProjectAllowed.ToString())
                .AddItem("release", "Last release", snapshot.LastReleaseReason.ToString());
        }
    }
}
