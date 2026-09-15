using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deucarian.PointerCapture.Tests
{
    public sealed class PointerCaptureSessionTests
    {
        [UnityTearDown]
        public IEnumerator RestoreEditorMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [Test]
        public void OldSessionCannotReleaseOrEscapeANewOwnersCapture()
        {
            var platform = new FakePlatform();
            using (var service = new PointerCaptureService(platform))
            {
                service.Enable();
                var firstOwner = new object(); var secondOwner = new object();
                using (var first = new PointerCaptureSession(service, firstOwner))
                using (var second = new PointerCaptureSession(service, secondOwner))
                {
                    Assert.That(first.RequestCapture(firstOwner), Is.True);
                    Assert.That(second.RequestCapture(secondOwner), Is.True);
                    Assert.That(first.State, Is.EqualTo(DeucarianPointerCaptureState.Idle));
                    Assert.That(second.State, Is.EqualTo(DeucarianPointerCaptureState.Active));
                    int releases = platform.Releases;
                    Assert.That(first.ReleaseCapture(firstOwner), Is.False);
                    first.NotifyEscapePressed(); first.Dispose();
                    Assert.That(platform.Releases, Is.EqualTo(releases));
                    Assert.That(second.State, Is.EqualTo(DeucarianPointerCaptureState.Active));
                }
            }
        }

        [Test]
        public void SceneClaimCanEndAndAnotherClaimUsesTheSameService()
        {
            var platform = new FakePlatform();
            using (var service = new PointerCaptureService(platform))
            {
                service.Enable();
                var owner = new object();
                var oldScene = new PointerCaptureSession(service, owner);
                Assert.That(oldScene.RequestCapture(owner), Is.True);
                oldScene.Dispose();
                Assert.That(oldScene.RequestCapture(owner), Is.False);
                using (var nextScene = new PointerCaptureSession(service, null))
                    Assert.That(nextScene.RequestCapture(new object()), Is.True);
                Assert.That(platform.Requests, Is.EqualTo(2));
            }
        }

        [Test]
        public void DisabledOrDisposedHostRejectsBorrowedClaims()
        {
            var platform = new FakePlatform();
            var service = new PointerCaptureService(platform);
            service.Enable();
            var owner = new object();
            using (var session = new PointerCaptureSession(service, owner))
            {
                Assert.That(session.RequestCapture(owner), Is.True);
                service.Disable();
                Assert.That(session.RequestCapture(owner), Is.False);
                service.Enable();
                Assert.That(session.RequestCapture(owner), Is.True);
                service.Dispose();
                Assert.That(session.State, Is.EqualTo(DeucarianPointerCaptureState.Idle));
                Assert.That(session.RequestCapture(owner), Is.False);
                Assert.That(platform.LockState, Is.EqualTo(CursorLockMode.None));
            }
        }

        [Test]
        public void DisposingDuringRequestedEventNeverLocksThePlatformAfterTeardown()
        {
            var platform = new FakePlatform();
            using (var service = new PointerCaptureService(platform))
            {
                service.Enable();
                service.StateChanged += (_, args) => { if (args.CurrentState == DeucarianPointerCaptureState.Requested) service.Dispose(); };
                Assert.That(service.RequestCapture(this), Is.False);
                Assert.That(platform.Requests, Is.Zero);
            }
        }

        [Test]
        public void ScopeOwnsHostButDisposingASessionDoesNotDestroyIt()
        {
            using (var scope = new PointerCaptureScope())
            {
                var first = scope.OpenSession();
                first.Dispose();
                using (var second = scope.OpenSession()) Assert.That(second, Is.Not.Null);
                scope.Dispose();
                Assert.Throws<ObjectDisposedException>(() => scope.OpenSession());
            }
        }

        [Test]
        public void DisposingSessionCannotReacquireFromAReleaseCallback()
        {
            using (var service = new PointerCaptureService(new FakePlatform()))
            {
                service.Enable();
                var session = new PointerCaptureSession(service, this);
                Assert.That(session.RequestCapture(this), Is.True);
                bool reacquired = false;
                service.StateChanged += (_, args) => { if (args.CurrentState == DeucarianPointerCaptureState.Idle) reacquired = session.RequestCapture(this); };
                session.Dispose();
                Assert.That(reacquired, Is.False);
                Assert.That(service.HasOwner, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ApplicationScopeSurvivesSceneUnloadAndOwnsHostTeardown()
        {
            yield return new EnterPlayMode();
            var scene = SceneManager.CreateScene("Capture lifetime fixture");
            SceneManager.SetActiveScene(scene);
            var scope = new PointerCaptureScope();
            var first = scope.OpenSession();
            var hosts = Resources.FindObjectsOfTypeAll<DeucarianPointerCaptureController>();
            Assert.That(hosts.Length, Is.EqualTo(1));
            var host = hosts[0];
            first.Dispose();
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.That(host != null && host.isActiveAndEnabled, Is.True);
            using (var second = scope.OpenSession()) Assert.That(second, Is.Not.Null);
            scope.Dispose();
            Assert.That(host.isActiveAndEnabled, Is.False);
            for (int frame = 0; frame < 10 && host != null; frame++) yield return null;
            Assert.That(host == null, Is.True);
            yield return new ExitPlayMode();
        }

        private sealed class FakePlatform : IPointerCapturePlatform
        {
            public DeucarianPointerCapturePlatform CurrentPlatform => DeucarianPointerCapturePlatform.Editor;
            public bool IsSupported => true;
            public bool HasFocus => true;
            public CursorLockMode LockState { get; private set; }
            public bool CursorVisible => true;
            internal int Requests, Releases;
            public void Initialize(bool release) { }
            public DeucarianPointerCapturePlatformStatus Request(bool hide) { Requests++; LockState = CursorLockMode.Locked; return Observe(); }
            public DeucarianPointerCapturePlatformStatus Observe() => LockState == CursorLockMode.Locked ? DeucarianPointerCapturePlatformStatus.Active : DeucarianPointerCapturePlatformStatus.Idle;
            public bool TryGetPointerPosition(out Vector2 position) { position = default; return false; }
            public void Release(CursorLockMode mode, bool visible, bool restore, Vector2 position) { Releases++; LockState = mode; }
        }
    }
}
