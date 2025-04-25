using System;
using System.Collections.Generic;
using UnityEngine;
using static HoloLab.QuestCameraTools.QR.QRCodeWithFilterState;

namespace HoloLab.QuestCameraTools.QR
{
    public class AspectRatioFilterComponent : AbstractFilterComponent
    {
        [SerializeField]
        private float threshold = 0.95f;

        public override QRCodeWithFilterState Process(QRCodeWithFilterState codeWithState)
        {
            if (codeWithState.FilterState != FilterStateType.Valid)
            {
                return codeWithState;
            }

            var info = codeWithState.QRCodeDetectedInfo;
            var width = info.PhysicalWidth;
            var height = info.PhysicalHeight;

            var aspectRatio = width > height
                ? height / width
                : width / height;

            if (aspectRatio < threshold)
            {
                return new QRCodeWithFilterState(FilterStateType.Ignored, info);
            }
            else
            {
                return new QRCodeWithFilterState(FilterStateType.Valid, info);
            }
        }
    }
}

