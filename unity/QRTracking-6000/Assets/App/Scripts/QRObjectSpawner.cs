using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoloLab.QuestCameraTools.QR.Samples
{
    public class QRObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        private QRObject qrObjectPrefab;

        private QuestQRTracking qrTracking;

        private readonly Dictionary<string, QRObject> qrObjects = new Dictionary<string, QRObject>();

        private void Start()
        {
            qrTracking = FindFirstObjectByType<QuestQRTracking>();
            if (qrTracking == null)
            {
                Debug.LogError($"{nameof(QuestQRTracking)} not found in the scene.");
                return;
            }

            qrTracking.OnQRCodeDetected += OnQRCodeDetected;
        }

        private void OnQRCodeDetected(List<QRCodeDetectedInfo> infoList)
        {
            foreach (var info in infoList)
            {
                if (qrObjects.ContainsKey(info.Text))
                {
                    continue;
                }

                var qrObject = Instantiate(qrObjectPrefab);
                qrObject.SetQRCodeDetectedInfo(info);

                qrObjects[info.Text] = qrObject;
            }
        }
    }
}

