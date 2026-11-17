using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zellige.UI;

namespace Zellige.Tests
{
    /// <summary>
    /// The Android Back / Escape quit confirmation. The key itself cannot be pressed in an
    /// EditMode test; these tests drive <see cref="QuitDialog.HandleBack"/>, which is exactly what
    /// the key handler calls. Quit itself (Application.Quit) is verified on device.
    /// </summary>
    public class QuitDialogTests
    {
        private GameObject canvas;
        private float savedTimeScale;

        [SetUp]
        public void SetUp()
        {
            savedTimeScale = Time.timeScale;
            canvas = new GameObject("Test Canvas", typeof(RectTransform), typeof(Canvas));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(canvas);
            Time.timeScale = savedTimeScale;
        }

        private QuitDialog Build()
        {
            return QuitDialog.Build(canvas.transform);
        }

        [Test]
        public void Build_CreatesOneHiddenDialog_DrawnAboveTheGame()
        {
            QuitDialog dialog = Build();

            Assert.AreEqual(1, canvas.GetComponentsInChildren<QuitDialog>(true).Length);
            Assert.IsFalse(dialog.IsOpen, "starts hidden");
            Assert.IsTrue(dialog.gameObject.activeSelf, "the listener object stays active so it can hear Back");
            var ownCanvas = dialog.GetComponent<Canvas>();
            Assert.IsTrue(ownCanvas.overrideSorting);
            Assert.Greater(ownCanvas.sortingOrder, 0, "drawn above the table, call-outs and result screen");
            Assert.IsNotNull(dialog.GetComponent<GraphicRaycaster>(), "its buttons must receive taps");
            Assert.IsNotNull(dialog.transform.Find("Overlay/Panel/Cancel Button").GetComponent<Button>());
            Assert.IsNotNull(dialog.transform.Find("Overlay/Panel/Quit Button").GetComponent<Button>());
        }

        [Test]
        public void Back_OpensTheDialog_AndPausesTheGame()
        {
            QuitDialog dialog = Build();
            Time.timeScale = 1f;

            dialog.HandleBack();

            Assert.IsTrue(dialog.IsOpen);
            Assert.AreEqual(0f, Time.timeScale);
        }

        [Test]
        public void BackWhileOpen_ClosesIt_AndRestoresThePreviousTimeScale()
        {
            QuitDialog dialog = Build();
            Time.timeScale = 0.5f; // not 1, to prove the previous value is restored, not a fixed one

            dialog.HandleBack();
            dialog.HandleBack();

            Assert.IsFalse(dialog.IsOpen);
            Assert.AreEqual(0.5f, Time.timeScale);
        }

        [Test]
        public void OpeningTwice_KeepsTheOriginalTimeScale()
        {
            QuitDialog dialog = Build();
            Time.timeScale = 1f;

            dialog.Open();
            dialog.Open(); // must not record the paused value 0 as "previous"
            dialog.Close();

            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void ClosingWhenAlreadyClosed_DoesNothing()
        {
            QuitDialog dialog = Build();
            Time.timeScale = 0.75f;

            dialog.Close();

            Assert.IsFalse(dialog.IsOpen);
            Assert.AreEqual(0.75f, Time.timeScale);
        }
    }
}
