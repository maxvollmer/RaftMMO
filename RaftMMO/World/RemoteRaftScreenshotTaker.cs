using RaftMMO.Network;
using RaftMMO.Utilities;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace RaftMMO.World
{
    public class RemoteRaftScreenshotTaker
    {
        private static GameObject screenshotCamera = null;
        private static RenderTexture screenshotTexture = null;
        private static Stopwatch takeScreenshotStopwatch = new Stopwatch();
        private static string takeScreenshotFilepath = null;

        public static void Initialize()
        {
            screenshotTexture = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);
            screenshotTexture.Create();

            screenshotCamera = new GameObject("ScreenshotCamera", typeof(Camera));
            screenshotCamera.transform.SetParent(Camera.main.transform.parent, true);
            screenshotCamera.GetComponent<Camera>().targetTexture = screenshotTexture;

            DisableCamera();
        }

        private static void DisableCamera()
        {
            if (screenshotCamera == null)
                return;

            screenshotCamera.GetComponent<Camera>().enabled = false;
            screenshotCamera.SetActive(false);
        }

        private static void EnableCamera()
        {
            if (screenshotCamera == null)
                return;

            screenshotCamera.SetActive(true);
            screenshotCamera.GetComponent<Camera>().enabled = true;
        }

        private static void LookAtRemoteRaft()
        {
            if (screenshotCamera == null)
                return;

            var remoteRaftPosition = RemoteRaft.Transform.position;
            var remoteRaftDirection = (Globals.CurrentRaftMeetingPoint - remoteRaftPosition);
            remoteRaftDirection.y = 0f;
            remoteRaftDirection = remoteRaftDirection.normalized;

            // rotate a little bit for a nicer view angle
            remoteRaftDirection = Quaternion.AngleAxis(Random.Range(-7f, 7f), Vector3.up) * remoteRaftDirection;

            var remoteRaftBounds = RemoteRaft.CalculateBounds();
            float distance = System.Math.Max(5f, System.Math.Max(remoteRaftBounds.extents.x, remoteRaftBounds.extents.z));

            screenshotCamera.transform.position = remoteRaftBounds.center + (remoteRaftDirection * distance) + new Vector3(0f, 5f, 0f);
            screenshotCamera.transform.LookAt(remoteRaftBounds.center);
        }

        public static void TakeScreenshot(string filepath)
        {
            takeScreenshotFilepath = filepath;
            takeScreenshotStopwatch.Restart();
            EnableCamera();
        }

        public static void Update()
        {
            if (!takeScreenshotStopwatch.IsRunning)
                return;

            LookAtRemoteRaft();

            if (takeScreenshotStopwatch.ElapsedMilliseconds < 5000)
                return;

            ActuallyTakeScreenshot();

            DisableCamera();

            takeScreenshotFilepath = null;
            takeScreenshotStopwatch.Stop();
        }

        private static void ActuallyTakeScreenshot()
        {
            // make sure we are still connected!
            if (!RemoteSession.IsConnectedToPlayer)
                return;

            var backupActive = RenderTexture.active;

            Texture2D tex = new Texture2D(screenshotTexture.width, screenshotTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = screenshotTexture;
            tex.ReadPixels(new Rect(0, 0, screenshotTexture.width, screenshotTexture.height), 0, 0);
            tex.Apply();

            File.WriteAllBytes(takeScreenshotFilepath, ImageConversion.EncodeToPNG(tex));

            RenderTexture.active = backupActive;

            Object.Destroy(tex);
        }

        public static void Destroy()
        {
            DisableCamera();
            Object.Destroy(screenshotCamera);
            screenshotCamera = null;
            screenshotTexture?.Release();
            screenshotTexture = null;
            takeScreenshotStopwatch.Stop();
            takeScreenshotStopwatch.Reset();
            takeScreenshotFilepath = null;
        }
    }
}
