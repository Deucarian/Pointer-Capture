using System.Linq;
using Deucarian.Editor;
using NUnit.Framework;

namespace Deucarian.PointerCapture.Tests
{
    public sealed class ControlCenterRegistrationTests
    {
        private const string PackageId =
            "com.deucarian.pointer-capture";

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
