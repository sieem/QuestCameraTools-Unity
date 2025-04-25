using UnityEngine;

namespace HoloLab.QuestCameraTools.QR
{
    public abstract class AbstractFilterComponent : MonoBehaviour
    {
        public abstract QRCodeWithFilterState Process(QRCodeWithFilterState codeWithState);
    }
}

