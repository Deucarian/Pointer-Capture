using System.Linq;
using Deucarian.Editor;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Deucarian.PointerCapture.Tests
{
    public sealed class ControlCenterRegistrationTests
    {
        private const string PackageId =
            "com.deucarian.pointer-capture";

        [Test]
        public void ReturningToThePageKeepsPlatformDetailsExpanded()
        {
            Assert.IsTrue(DeucarianToolRegistry.TryGet(DeucarianToolIds.PointerCapture, out var tool));
            using (var page = tool.CreatePage())
            {
                var details = page.Root.Q<Foldout>("pointer-platform-details"); details.value = true;
                page.Deactivate(); page.Activate(null);
                Assert.AreSame(details, page.Root.Q<Foldout>("pointer-platform-details"));
                Assert.IsTrue(details.value);
            }
        }

        [Test]
        public void PackageRegistersStableToolAndCard()
        {
            Assert.That(
                DeucarianToolRegistry.TryGet(
                    DeucarianToolIds.PointerCapture,
                    out DeucarianToolDescriptor tool),
                Is.True);
            Assert.That(tool.OwningPackage, Is.EqualTo(PackageId));

            DeucarianControlCenterSnapshot snapshot =
                DeucarianControlCenterSnapshotBuilder.Capture(true);
            Assert.That(
                snapshot.Cards.Any(
                    card => card.OwningPackage == PackageId),
                Is.True);
            DeucarianControlCenterCard card = snapshot.Cards.Single(
                candidate => candidate.Id == PackageId + ".setup");
            Assert.That(
                card.Details.Any(
                    detail => detail.StartsWith("Current platform policy:")),
                Is.True);
        }
    }
}
