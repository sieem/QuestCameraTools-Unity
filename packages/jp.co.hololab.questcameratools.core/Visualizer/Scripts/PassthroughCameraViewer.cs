using PassthroughCameraSamples;
using System.Collections;
using UnityEngine;

namespace HoloLab.QuestCameraTools.Core
{
    public class PassthroughCameraViewer : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var webCamTextureManager = FindAnyObjectByType<WebCamTextureManager>();

            while (webCamTextureManager.WebCamTexture == null)
            {
                yield return null;
            }

            var renderer = GetComponent<Renderer>();

            renderer.material.mainTexture = webCamTextureManager.WebCamTexture;
        }
    }
}

