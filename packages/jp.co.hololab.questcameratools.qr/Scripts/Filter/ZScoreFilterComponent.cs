using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HoloLab.QuestCameraTools.QR.QRCodeWithFilterState;

namespace HoloLab.QuestCameraTools.QR
{
    public class ZScoreFilterComponent : AbstractFilterComponent
    {
        public enum AggregationType
        {
            Average = 0,
            Latest,
        }

        [SerializeField]
        private int windowSize = 16;

        [SerializeField]
        private float zScoreThreshold = 0.5f;

        [SerializeField]
        private AggregationType aggregationType = AggregationType.Average;

        private readonly List<QRCodeWithFilterState> buffer = new List<QRCodeWithFilterState>();

        public override QRCodeWithFilterState Process(QRCodeWithFilterState codeWithState)
        {
            if (codeWithState.FilterState != FilterStateType.Valid)
            {
                return codeWithState;
            }

            buffer.Add(codeWithState);

            if (buffer.Count > windowSize)
            {
                buffer.RemoveAt(0);
            }

            var validCount = 0;
            var mean = Vector3.zero;
            foreach (var c in buffer)
            {
                if (c.FilterState == FilterStateType.Valid)
                {
                    validCount += 1;
                    mean += c.QRCodeDetectedInfo.Pose.position;
                }
            }
            mean /= validCount;

            float varianceSum = 0;
            foreach (var c in buffer)
            {
                if (c.FilterState == FilterStateType.Valid)
                {
                    varianceSum += Vector3.SqrMagnitude(c.QRCodeDetectedInfo.Pose.position - mean);
                }
            }

            var variance = varianceSum / validCount;

            var threshold = zScoreThreshold * zScoreThreshold;
            return aggregationType switch
            {
                AggregationType.Latest => GetLatestCode(buffer, mean, variance, threshold),
                _ => GetAverageCode(buffer, mean, variance, threshold),
            };
        }

        private static QRCodeWithFilterState GetLatestCode(List<QRCodeWithFilterState> buffer, Vector3 mean, float variance, float threshold)
        {
            var latestValidCode = buffer.LastOrDefault(x => IsValid(x, mean, variance, threshold));
            if (latestValidCode == null)
            {
                // Use the latest code if no valid code is found
                return buffer.Last();
            }
            else
            {
                return latestValidCode;
            }
        }

        private static QRCodeWithFilterState GetAverageCode(List<QRCodeWithFilterState> buffer, Vector3 mean, float variance, float threshold)
        {
            if (TryGetAverage(buffer.Where(x => IsValid(x, mean, variance, threshold)), out var codeInfo))
            {
                return new QRCodeWithFilterState(FilterStateType.Valid, codeInfo);
            }
            else
            {
                // Use the latest code if no valid code is found
                return buffer.Last();
            }
        }

        private static bool TryGetAverage(IEnumerable<QRCodeWithFilterState> codeWithStates, out QRCodeDetectedInfo codeInfo)
        {
            var count = codeWithStates.Count();
            if (count == 0)
            {
                codeInfo = null;
                return false;
            }

            var sumPosition = Vector3.zero;
            var sumForward = Vector3.zero;
            var sumRight = Vector3.zero;

            var sumSize = 0f;
            var sumWidth = 0f;
            var sumHeight = 0f;

            foreach (var c in codeWithStates)
            {
                var info = c.QRCodeDetectedInfo;
                var p = info.Pose;
                sumPosition += p.position;
                sumForward += p.forward;
                sumRight += p.right;

                sumSize += info.PhysicalSize;
                sumWidth += info.PhysicalWidth;
                sumHeight += info.PhysicalHeight;
            }

            var position = sumPosition / count;

            var forward = (sumForward / count).normalized;
            var right = (sumRight / count).normalized;
            var rotation = Quaternion.LookRotation(forward, Vector3.Cross(forward, right));
            var pose = new Pose(position, rotation);

            var size = sumSize / count;
            var width = sumWidth / count;
            var height = sumHeight / count;

            var lastCodeInfo = codeWithStates.Last().QRCodeDetectedInfo;
            codeInfo = new QRCodeDetectedInfo(pose, size, width, height, lastCodeInfo.Text, lastCodeInfo.RawBytes);
            return true;
        }

        private static bool IsValid(QRCodeWithFilterState codeWithState, Vector3 mean, float variance, float threshold)
        {
            if (codeWithState.FilterState != FilterStateType.Valid)
            {
                return false;
            }

            var sqrScore = Vector3.SqrMagnitude(codeWithState.QRCodeDetectedInfo.Pose.position - mean) / variance;
            return sqrScore < threshold;
        }
    }
}

