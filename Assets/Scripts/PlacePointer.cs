using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARGeometry.ARTools
{
    [RequireComponent(typeof(ARRaycastManager))]
    public class PlacePointer : MonoBehaviour
    {
        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        public GameObject pointerPrefab;

        GameObject spawnMarker;

        public Vector3 PointerPosition
        {
            get { return spawnMarker.transform.position; }
        }

        ARRaycastManager raycastManager;

        // Start is called before the first frame update
        void Awake()
        { 
            raycastManager = GetComponent<ARRaycastManager>();
            spawnMarker = null;
        }

        // Update is called once per frame
        void Update()
        {
            // If there is no tap, then simply do nothing until the next call to Update().
            if (Input.touchCount == 0)
                return;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
                return;

            if (raycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                var hitPose = s_Hits[0].pose;

                if (spawnMarker == null)
                    spawnMarker = Instantiate(pointerPrefab, hitPose.position, hitPose.rotation);

                else
                    spawnMarker.transform.position = hitPose.position;
            }
        }

    }
}
