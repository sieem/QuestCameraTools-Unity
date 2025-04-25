using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static HoloLab.QuestCameraTools.QR.QRCodeWithFilterState;

namespace HoloLab.QuestCameraTools.QR
{
    public class QRCodeWithFilterState
    {
        public enum FilterStateType
        {
            NotDetected = 0,
            Valid,
            Ignored
        }

        public FilterStateType FilterState { get; }

        public QRCodeDetectedInfo QRCodeDetectedInfo { get; }

        public QRCodeWithFilterState(FilterStateType filterState, QRCodeDetectedInfo qrCodeDetectedInfo)
        {
            FilterState = filterState;
            QRCodeDetectedInfo = qrCodeDetectedInfo;
        }
    }

    public class QRTracker : MonoBehaviour
    {
        private enum TrackingStateType
        {
            None = 0,
            Tracking,
            Lost
        }

        private enum AnchorPointType
        {
            Center = 0,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        [SerializeField]
        private string targetQRText;

        public string TargetQRText
        {
            get => targetQRText;
            set => targetQRText = value;
        }

        [SerializeField]
        private AnchorPointType anchorPoint = AnchorPointType.Center;

        [SerializeField]
        private bool scaleByPhysicalSize = false;

        [SerializeField]
        private RotationConstraintType rotationConstraint = RotationConstraintType.AnyDirection;

        public RotationConstraintType RotationConstraint
        {
            get => rotationConstraint;
            set => rotationConstraint = value;
        }

        [SerializeField]
        private List<AbstractFilterComponent> filterComponents = new List<AbstractFilterComponent>();

        [SerializeField]
        private UnityEvent onAwake = new UnityEvent();

        [SerializeField]
        private UnityEvent<QRCodeDetectedInfo> onFirstDetected = new UnityEvent<QRCodeDetectedInfo>();

        [SerializeField]
        private UnityEvent<QRCodeDetectedInfo> onDetected = new UnityEvent<QRCodeDetectedInfo>();

        [SerializeField]
        private UnityEvent onLost = new UnityEvent();

        private QuestQRTracking qrTracking;

        private TrackingStateType trackingState = TrackingStateType.None;

        public event Action<QRCodeDetectedInfo> OnFirstDetected;
        public event Action<QRCodeDetectedInfo> OnDetected;
        public event Action OnLost;

        private void Awake()
        {
            qrTracking = FindFirstObjectByType<QuestQRTracking>();
            if (qrTracking == null)
            {
                Debug.LogError($"{nameof(QuestQRTracking)} not found in scene");
            }

            InvokeOnAwake();
        }

        private void Start()
        {
            if (qrTracking != null)
            {
                qrTracking.OnQRCodeDetected += OnQRCodeDetected;
            }
        }

        private void OnQRCodeDetected(List<QRCodeDetectedInfo> infoList)
        {
            QRCodeWithFilterState codeWithState;

            var info = infoList.FirstOrDefault(x => x.Text == targetQRText);
            if (info == null)
            {
                codeWithState = new QRCodeWithFilterState(FilterStateType.NotDetected, null);
            }
            else
            {
                codeWithState = new QRCodeWithFilterState(FilterStateType.Valid, info);
            }

            // Apply filters to the detected pose
            foreach (var filter in filterComponents)
            {
                codeWithState = filter.Process(codeWithState);
            }

            switch (codeWithState.FilterState)
            {
                case FilterStateType.NotDetected:
                    // Target QR code not detected
                    if (trackingState == TrackingStateType.Tracking)
                    {
                        trackingState = TrackingStateType.Lost;
                        InvokeOnLost();
                    }
                    break;
                case FilterStateType.Valid:
                    var detectedInfo = codeWithState.QRCodeDetectedInfo;
                    var anchorPose = GetAnchorPointPose(detectedInfo.Pose, detectedInfo.PhysicalSize, anchorPoint);
                    var rotation = RotationConstraintUtility.ApplyConstraint(anchorPose.rotation, rotationConstraint);
                    transform.SetPositionAndRotation(anchorPose.position, rotation);

                    if (scaleByPhysicalSize)
                    {
                        transform.localScale = info.PhysicalSize * Vector3.one;
                    }

                    var firstDetected = trackingState == TrackingStateType.None;
                    trackingState = TrackingStateType.Tracking;

                    if (firstDetected)
                    {
                        InvokeOnFirstDetected(info);
                    }

                    InvokeOnDetected(info);
                    break;
                case FilterStateType.Ignored:
                    break;
            }
        }

        private void InvokeOnAwake()
        {
            try
            {
                onAwake.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void InvokeOnFirstDetected(QRCodeDetectedInfo info)
        {
            try
            {
                onFirstDetected.Invoke(info);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                OnFirstDetected?.Invoke(info);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void InvokeOnDetected(QRCodeDetectedInfo info)
        {
            try
            {
                onDetected.Invoke(info);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                OnDetected?.Invoke(info);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void InvokeOnLost()
        {
            try
            {
                onLost.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                OnLost?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static Pose GetAnchorPointPose(Pose centerPose, float markerSize, AnchorPointType anchorPoint)
        {
            if (anchorPoint == AnchorPointType.Center)
            {
                return centerPose;
            }

            var offset = anchorPoint switch
            {
                AnchorPointType.TopLeft => new Vector3(-markerSize / 2, 0, markerSize / 2),
                AnchorPointType.TopRight => new Vector3(markerSize / 2, 0, markerSize / 2),
                AnchorPointType.BottomLeft => new Vector3(-markerSize / 2, 0, -markerSize / 2),
                AnchorPointType.BottomRight => new Vector3(markerSize / 2, 0, -markerSize / 2),
                _ => Vector3.zero,
            };

            return new Pose(centerPose.position + centerPose.rotation * offset, centerPose.rotation);
        }
    }
}

