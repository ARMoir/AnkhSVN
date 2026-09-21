using System.Drawing;
using System.Runtime.InteropServices;
using Ankh.UI.PendingChanges;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NUnit.Framework;

namespace AnkhSvn_UnitTestProject.Dialogs
{
    [TestFixture]
    public class PendingNavigationIconTests
    {
        [Test]
        public void RequestsSurfaceAndDpiAndOwnsReturnedBitmap()
        {
            var service = new Mock<IVsImageService2>();
            var image = new Mock<IVsUIObject>();
            var source = new Bitmap(48, 48);
            source.SetPixel(0, 0, Color.Magenta);
            object data = source;
            image.Setup(x => x.get_Data(out data)).Returns(0);
            ImageAttributes requested = new ImageAttributes();
            service.Setup(x => x.GetImage(It.IsAny<ImageMoniker>(), It.IsAny<ImageAttributes>()))
                .Callback<ImageMoniker, ImageAttributes>((moniker, attributes) => requested = attributes)
                .Returns(image.Object);
            var background = Color.FromArgb(30, 40, 50);
            using (source)
            using (var result = PendingChangesToolControl.RenderNavigationIcon(service.Object, KnownMonikers.Conflict, background, 144))
            {
                Assert.That(result, Is.Not.SameAs(source));
                Assert.That(requested.Dpi, Is.EqualTo(144));
                Assert.That(requested.LogicalWidth, Is.EqualTo(32));
                Assert.That(requested.Background, Is.EqualTo(unchecked((uint)background.ToArgb())));
                Assert.That(requested.Flags & unchecked((uint)_ImageAttributesFlags.IAF_Background), Is.Not.Zero);
                source.Dispose();
                Assert.That(result.GetPixel(0, 0).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));
            }
        }

        [Test]
        public void UnavailableImageAllowsExistingIconToRemain()
        {
            var service = new Mock<IVsImageService2>();
            service.Setup(x => x.GetImage(It.IsAny<ImageMoniker>(), It.IsAny<ImageAttributes>()))
                .Throws(new COMException());
            Assert.That(PendingChangesToolControl.RenderNavigationIcon(service.Object, KnownMonikers.TaskList, Color.White, 96), Is.Null);
        }
    }
}
