using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HoloLab.QuestCameraTools.QR.Samples
{
    [RequireComponent(typeof(QRTracker))]
    public class QRObject : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text qrText;

        private QRTracker qrTracker;

        private void Awake()
        {
            qrTracker = GetComponent<QRTracker>();
        }

        public void SetQRCodeDetectedInfo(QRCodeDetectedInfo info)
        {
            qrText.text = info.Text;
            qrTracker.TargetQRText = info.Text;
        }
    }
}

