using System.Collections;
using System.IO;
using UnityEngine;

namespace ProjectAstra.Core.Rendering
{
    // TEMPORARY diagnostic. Grabs the real backbuffer to a file a moment after the map loads.
    //
    // Exists because Unity's per-camera screenshot path re-renders through Camera.Render(), which
    // skips renderer features entirely — so it shows the world without any full-screen effect and
    // gives a confidently wrong answer. ScreenCapture reads what is actually on screen.
    //
    // Delete once the CRT filter is verified.
    public class CrtCaptureProbe : MonoBehaviour
    {
        [SerializeField] private string outputPath = "Temp/crt_capture.png";
        [SerializeField] private int framesToWait = 60;

        private IEnumerator Start()
        {
            for (int i = 0; i < framesToWait; i++) yield return null;

            string full = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            ScreenCapture.CaptureScreenshot(full);

            // CaptureScreenshot lands at the end of a later frame, so give it room before
            // anything goes looking for the file.
            for (int i = 0; i < 10; i++) yield return null;
            Debug.Log($"[CrtCaptureProbe] wrote {full} exists={File.Exists(full)}");
        }
    }
}
