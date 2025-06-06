using System;
using TMPro;
using UnityEngine;

public class TimeDebug : MonoBehaviour
{
    private const string StringFormat = "Position: x:{0} y:{1} z:{2}";
    public GameObject xrSpace;

    private TextMeshProUGUI m_LabelText;
    void Start()
    {
        m_LabelText = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        m_LabelText.text = string.Format(StringFormat, xrSpace.transform.position.x, xrSpace.transform.position.y, xrSpace.transform.position.z);
    }
}
