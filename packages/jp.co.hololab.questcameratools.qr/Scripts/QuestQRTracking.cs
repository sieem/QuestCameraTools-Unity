using Meta.XR;
using PassthroughCameraSamples;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using ZXing;

namespace HoloLab.QuestCameraTools.QR
{
    public class QRCodeDetectedInfo
    {
        public Pose Pose { get; }

        public float PhysicalSize { get; }

        public float PhysicalWidth { get; }

        public float PhysicalHeight { get; }

        public string Text { get; }

        public byte[] RawBytes { get; }


        public QRCodeDetectedInfo(Pose pose, float physicalSize, float physicalWidth, float physicalHeight, string text, byte[] rawBytes)
        {
            Pose = pose;
            PhysicalSize = physicalSize;
            PhysicalWidth = physicalWidth;
            PhysicalHeight = physicalHeight;
            Text = text;
            RawBytes = rawBytes;
        }
    }

    public class QuestQRTracking : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The frame rate at which the QR code detection is performed. If set to 0, the detection will be performed at the minimum interval.")]
        private float detectionFrameRate = 0;

        public float DetectionFrameRate
        {
            get => detectionFrameRate;
            set => detectionFrameRate = value;
        }

        private WebCamTextureManager webCamTextureManager;
        private EnvironmentRaycastManager environmentRaycastManager;
        private CancellationTokenSource trackingLoopTokenSource;
        private float lastDetectionTime = float.MinValue;

        private readonly QRDetector qrDetector = new QRDetector();

        private bool TrackingEnabled => gameObject != null && gameObject.activeInHierarchy && enabled;

        public event Action<List<QRCodeDetectedInfo>> OnQRCodeDetected;

        private void Awake()
        {
            webCamTextureManager = FindFirstObjectByType<WebCamTextureManager>();
            if (webCamTextureManager == null)
            {
                Debug.LogError("WebCamTextureManager not found in scene");
            }

            environmentRaycastManager = FindFirstObjectByType<EnvironmentRaycastManager>();
            if (environmentRaycastManager == null)
            {
                Debug.LogError("EnvironmentRaycastManager not found in scene");
            }
        }

        private async void Start()
        {
            trackingLoopTokenSource = new CancellationTokenSource();
            if (webCamTextureManager != null)
            {
                await TrackingLoop(trackingLoopTokenSource.Token);
            }
        }

        private void OnDestroy()
        {
            if (trackingLoopTokenSource != null)
            {
                trackingLoopTokenSource.Cancel();
                trackingLoopTokenSource.Dispose();
                trackingLoopTokenSource = null;
            }
        }

        private async Task TrackingLoop(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (TrackingEnabled)
                {
                    if (detectionFrameRate <= 0 || Time.time >= lastDetectionTime + 1f / detectionFrameRate)
                    {
                        var webCamTexture = webCamTextureManager.WebCamTexture;
                        if (webCamTexture != null)
                        {
                            lastDetectionTime = Time.time;
                            await DetectQRAsync(webCamTexture, token);
                        }
                    }
                }

                await Task.Yield();
            }
        }

        private async Task DetectQRAsync(WebCamTexture webCamTexture, CancellationToken token)
        {
            var height = webCamTexture.height;
            var eye = webCamTextureManager.Eye;
            var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(eye);

            var results = await qrDetector.DetectMultipleAsync(webCamTexture);

            if (token.IsCancellationRequested)
            {
                return;
            }

            var detectedInfos = new List<QRCodeDetectedInfo>();
            foreach (var result in results)
            {
                if (TryGetDetectedInfo(result, height, cameraPose, eye, out var detectedInfo) == false)
                {
                    continue;
                }

                detectedInfos.Add(detectedInfo);
            }

            if (TrackingEnabled)
            {
                try
                {
                    OnQRCodeDetected?.Invoke(detectedInfos);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private bool TryGetDetectedInfo(Result result, int imageHeight, Pose cameraPose, PassthroughCameraEye eye, out QRCodeDetectedInfo detectedInfo)
        {
            detectedInfo = null;

            if (ZXingUtility.TryGetCellSize(result, out var cellSize) == false)
            {
                Debug.LogWarning("Failed to get cell size from QR code.");
                return false;
            }

            var points = result.ResultPoints;
            if (points.Length < 3)
            {
                return false;
            }

            if (RaycastToEnvironment(environmentRaycastManager, points[0], imageHeight, cameraPose, eye, out var bottomLeftHitInfo) == false)
            {
                Debug.LogWarning($"Failed to raycast to environment. Hit status: {bottomLeftHitInfo.status}");
                return false;
            }

            if (RaycastToEnvironment(environmentRaycastManager, points[1], imageHeight, cameraPose, eye, out var topLeftHitInfo) == false)
            {
                Debug.LogWarning($"Failed to raycast to environment. Hit status: {topLeftHitInfo.status}");
                return false;
            }

            if (RaycastToEnvironment(environmentRaycastManager, points[2], imageHeight, cameraPose, eye, out var topRightHitInfo) == false)
            {
                Debug.LogWarning($"Failed to raycast to environment. Hit status: {topRightHitInfo.status}");
                return false;
            }

            var bottomLeftPoint = bottomLeftHitInfo.point;
            var topLeftPoint = topLeftHitInfo.point;
            var topRightPoint = topRightHitInfo.point;

            var center = Vector3.LerpUnclamped(bottomLeftPoint, topRightPoint, 0.5f);

            var forward = (topLeftPoint - bottomLeftPoint).normalized;
            var right = (topRightPoint - topLeftPoint).normalized;

            if (forward == Vector3.zero || right == Vector3.zero)
            {
                return false;
            }

            var upwards = Vector3.Cross(forward, right);
            var rotation = Quaternion.LookRotation(forward, upwards);
            var pose = new Pose(center, rotation);

            var (physicalSize, physicalWidth, physicalHeightt) = GetPhysicalSize(bottomLeftPoint, topLeftPoint, topRightPoint, cellSize);

            detectedInfo = new QRCodeDetectedInfo(pose, physicalSize, physicalWidth, physicalHeightt, result.Text, result.RawBytes);
            return true;
        }

        private static bool RaycastToEnvironment(EnvironmentRaycastManager raycastManager, ResultPoint result, int imageHeight, Pose cameraPose, PassthroughCameraEye eye, out EnvironmentRaycastHit hitInfo)
        {
            var rayToEnvironment = GetRayToEnvironment(result.X, imageHeight - result.Y, cameraPose, eye);
            return raycastManager.Raycast(rayToEnvironment, out hitInfo);
        }

        private static Ray GetRayToEnvironment(float x, float y, Pose cameraPose, PassthroughCameraEye eye)
        {
            var screenPoint = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
            var rayInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(eye, screenPoint);
            var rayDirectionInWorld = cameraPose.rotation * rayInCamera.direction;
            var ray = new Ray(cameraPose.position, rayDirectionInWorld);
            return ray;
        }

        private static (float Size, float Width, float Height) GetPhysicalSize(Vector3 bottomLeftPoint, Vector3 topLeftPoint, Vector3 topRightPoint, int cellSize)
        {
            var horizontalDistance = Vector3.Distance(topLeftPoint, topRightPoint);
            var verticalDistance = Vector3.Distance(bottomLeftPoint, topLeftPoint);

            var cellSizeBetweenFinderPattern = cellSize - 7;

            var physicalWidth = horizontalDistance * cellSize / cellSizeBetweenFinderPattern;
            var physicalHeight = verticalDistance * cellSize / cellSizeBetweenFinderPattern;
            var physicalSize = (physicalHeight + physicalWidth) / 2f;

            return (physicalSize, physicalWidth, physicalHeight);
        }
    }
}

