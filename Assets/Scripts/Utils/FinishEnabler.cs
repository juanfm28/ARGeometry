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
            //This method will subscribe to the anchor creation procedure. 
            creator.OnAnchorCreated.AddListener(TryEnableButton);
        }
        //This is to only 
        private void TryEnableButton()
        {
            //The button will only be enabled if there are at least 3 anchors
            if(geometry.anchors.Count >= 3)
            {
                GetComponent<Button>().interactable = true;
            }
            else
                GetComponent<Button>().interactable = false;
        }

    }
}
