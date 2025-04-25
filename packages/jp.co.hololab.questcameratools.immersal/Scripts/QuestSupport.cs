using Immersal.XR;
using PassthroughCameraSamples;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace HoloLab.QuestCameraTools.Immersal
{
    public class QuestSupport : MonoBehaviour, IPlatformSupport
    {
        private WebCamTextureManager webCamTextureManager;

        private Color32[] webCamTexturePixels = new Color32[0];
        private byte[] questImageBytes = new byte[0];

        private void Awake()
        {
            webCamTextureManager = FindFirstObjectByType<WebCamTextureManager>();
        }

        public async Task<IPlatformConfigureResult> ConfigurePlatform()
        {
            var config = new PlatformConfiguration
            {
                CameraDataFormat = CameraDataFormat.RGB
            };
            return await ConfigurePlatform(config);
        }

        public async Task<IPlatformConfigureResult> ConfigurePlatform(IPlatformConfiguration configuration)
        {
            if (webCamTextureManager == null)
            {
                Debug.LogError("WebCamTextureManager not found");
                return new SimplePlatformConfigureResult { Success = false };
            }

            if (configuration.CameraDataFormat != CameraDataFormat.RGB)
            {
                Debug.LogError("Only RGB format is supported");
                return new SimplePlatformConfigureResult { Success = false };
            }

            while (webCamTextureManager.WebCamTexture == null)
            {
                await Task.Yield();
            }

            return new SimplePlatformConfigureResult { Success = true };
        }

        public Task StopAndCleanUp()
        {
            return Task.CompletedTask;
        }

        public Task<IPlatformUpdateResult> UpdatePlatform()
        {
            var configuration = new PlatformConfiguration
            {
                CameraDataFormat = CameraDataFormat.RGB
            };

            return UpdatePlatform(configuration);
        }

        private Vector4 GetIntrinsics(PassthroughCameraEye eye)
        {
            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(eye);
            var focalLength = intrinsics.FocalLength;
            var principalPoint = intrinsics.PrincipalPoint;

            return new Vector4(focalLength.x, focalLength.y, principalPoint.x, principalPoint.y);
        }

        public Task<IPlatformUpdateResult> UpdatePlatform(IPlatformConfiguration oneShotConfiguration)
        {
            var tracked = OVRManager.hasVrFocus
                && OVRManager.hasInputFocus
                && OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.EyeCenter)
                && OVRPlugin.GetNodeOrientationTracked(OVRPlugin.Node.EyeCenter);

            if (tracked == false)
            {
                IPlatformUpdateResult notTrackedResult = new SimplePlatformUpdateResult
                {
                    Success = false,
                    Status = new SimplePlatformStatus
                    {
                        TrackingQuality = 0
                    }
                };

                return Task.FromResult(notTrackedResult);
            }

            var webCamTexture = webCamTextureManager.WebCamTexture;
            UpdateQuestImageBytes(webCamTexture);
            var questImageData = new QuestImageData(questImageBytes);

            var eye = webCamTextureManager.Eye;
            var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(eye);
            var intrinsics = GetIntrinsics(eye);

            var data = new CameraData(questImageData)
            {
                Width = webCamTexture.width,
                Height = webCamTexture.height,
                Intrinsics = intrinsics,
                Format = CameraDataFormat.RGB,
                Channels = 3,
                CameraPositionOnCapture = cameraPose.position,
                CameraRotationOnCapture = cameraPose.rotation,
                Orientation = Quaternion.identity
            };

            IPlatformUpdateResult result = new SimplePlatformUpdateResult
            {
                Success = true,
                CameraData = data,
                Status = new SimplePlatformStatus
                {
                    TrackingQuality = 1
                }
            };

            return Task.FromResult(result);
        }

        private void UpdateQuestImageBytes(WebCamTexture webCamTexture)
        {
            var width = webCamTexture.width;
            var height = webCamTexture.height;

            if (webCamTexturePixels.Length != width * height)
            {
                webCamTexturePixels = new Color32[width * height];
            }
            webCamTexture.GetPixels32(webCamTexturePixels);

            if (questImageBytes.Length != width * height * 3)
            {
                questImageBytes = new byte[width * height * 3];
            }

            // Flip the image horizontally
            for (var y = 0; y < webCamTexture.height; y++)
            {
                for (var x = 0; x < webCamTexture.width; x++)
                {
                    var i = y * webCamTexture.width + x;
                    var j = y * webCamTexture.width + (webCamTexture.width - x - 1);
                    var color = webCamTexturePixels[i];
                    questImageBytes[j * 3] = color.r;
                    questImageBytes[j * 3 + 1] = color.g;
                    questImageBytes[j * 3 + 2] = color.b;
                }
            }
        }
    }

    public class QuestImageData : ImageData
    {
        public override IntPtr UnmanagedDataPointer => unmanagedDataPointer;

        public override byte[] ManagedBytes { get; }

        private IntPtr unmanagedDataPointer;
        private GCHandle managedDataHandle;

        public QuestImageData(byte[] imageBytes)
        {
            ManagedBytes = imageBytes;
            managedDataHandle = GCHandle.Alloc(ManagedBytes, GCHandleType.Pinned);
            unmanagedDataPointer = managedDataHandle.AddrOfPinnedObject();
        }

        public override void DisposeData()
        {
            managedDataHandle.Free();
            unmanagedDataPointer = IntPtr.Zero;
        }
    }
}

