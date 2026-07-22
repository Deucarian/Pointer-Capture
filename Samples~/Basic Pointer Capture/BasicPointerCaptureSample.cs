using UnityEngine;

namespace Deucarian.PointerCapture.Samples
{
    [RequireComponent(typeof(DeucarianPointerCaptureController))]
    public sealed class BasicPointerCaptureSample : MonoBehaviour
    {
        private DeucarianPointerCaptureController controller;

        private void Awake()
        {
            controller = GetComponent<DeucarianPointerCaptureController>();
        }

        private void Update()
        {
            bool neutral = !Input.GetMouseButton(1) && !Input.GetKey(KeyCode.Escape);
            bool newAction = Input.GetMouseButtonDown(1);
            controller.UpdateInputRearming(neutral, newAction);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                controller.NotifyEscapePressed();
            }

            if (newAction)
            {
                controller.RequestCapture(this);
            }

            if (Input.GetMouseButtonUp(1))
            {
                controller.ReleaseCapture(this);
            }
        }

        private void OnGUI()
        {
            DeucarianPointerCaptureDiagnosticsSnapshot snapshot =
                controller.GetDiagnosticsSnapshot();
            GUILayout.BeginArea(new Rect(16f, 16f, 420f, 112f), GUI.skin.box);
            GUILayout.Label("Right mouse: capture | Escape: cancel");
            GUILayout.Label("State: " + snapshot.State);
            GUILayout.Label("Allowed: " + snapshot.EffectiveCaptureAllowed);
            GUILayout.Label(snapshot.Message);
            GUILayout.EndArea();
        }
    }
}

