using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ZXing;
using ZXing.Unity;

namespace HoloLab.QuestCameraTools.QR
{
    internal class QRDetector
    {
        private readonly BarcodeReader barcodeReader;

        public QRDetector()
        {
            barcodeReader = new BarcodeReader
            {
                AutoRotate = false,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[]
                    {
                        BarcodeFormat.QR_CODE
                    }
                }
            };
        }

        public async Task<Result[]> DetectMultipleAsync(WebCamTexture webCamTexture)
        {
            var pixels = webCamTexture.GetPixels32();

            var width = webCamTexture.width;
            var height = webCamTexture.height;

            var results = await Task.Run(() =>
            {
                return barcodeReader.DecodeMultiple(pixels, width, height);
            });

            return results ?? Array.Empty<Result>();
        }
    }
}

