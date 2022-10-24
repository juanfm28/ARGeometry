using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARGeometry
{
    [RequireComponent(typeof(Button))]
    public class FinishEnabler : MonoBehaviour
    {
        Geometry geometry;
        private void Awake()
        {
            geometry = FindObjectOfType<Geometry>();
            AnchorCreator creator = FindObjectOfType<AnchorCreator>();
            creator.OnAnchorCreated.AddListener(TryEnableButton);
        }

        private void TryEnableButton()
        {
            if(geometry.anchors.Count >= 3)
            {
                GetComponent<Button>().interactable = true;
                ARDebugManager.Instance.LogInfo("Can create now!");
            }
            else
                GetComponent<Button>().interactable = false;
        }

    }
}
