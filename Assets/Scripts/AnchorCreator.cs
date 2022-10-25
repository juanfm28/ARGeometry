using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events;

namespace ARGeometry
{
    [RequireComponent(typeof(ARAnchorManager))]
    [RequireComponent(typeof(ARRaycastManager))]
    [RequireComponent(typeof(ARPlaneManager))]
    public class AnchorCreator : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject pointerPrefab;
        public GameObject anchorPrefab;
        public GameObject helperLinePrefab;

        public bool createLines;

        public Geometry geometry;
        public UnityEvent OnAnchorCreated;

        [Header("References")]
        ARAnchorManager anchorManager;
        ARRaycastManager raycastManager;
        ARPlaneManager planeManager;

        //Class variables
        Transform pointer;
        ARPlane pointedPlane;
        ARAnchor lastAnchor;
        List<GameObject> lines;
        List<GameObject> markers;
        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        private void Awake()
        {
            //Obtain all references needed
            anchorManager = GetComponent<ARAnchorManager>();
            raycastManager = GetComponent<ARRaycastManager>();
            planeManager = GetComponent<ARPlaneManager>();

            //Initialize all variables
            pointer = null;
            pointedPlane = null;
            lines = new List<GameObject>();
            markers = new List<GameObject>();
        }
        //The update main role is placing the pointer that indicates where the vertices are created on a plane
        void Update()
        {
            // If there is no tap, then simply do nothing until the next call to Update().
            if (Input.touchCount == 0)
                return;

            //To only fire this in the first frame of the touch and not all of them
            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
                return;
            
            //Verifies if the touch was on top of a UI element
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = touch.position;
            List<RaycastResult> raycastResults = new List<RaycastResult>();

            EventSystem.current.RaycastAll(pointerEventData, raycastResults);
            //If the touch was over a UI element, it won't continue
            if(raycastResults.Count > 0)
                return;
            //Place the pointer where the touch intersect a detected planes
            if (raycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                var hitPose = s_Hits[0].pose;
                pointedPlane = planeManager.GetPlane(s_Hits[0].trackableId);

                if (pointer == null)
                    pointer = Instantiate(pointerPrefab, hitPose.position, hitPose.rotation).transform;
                else
                    pointer.transform.position = hitPose.position;
            }
        }

        public void CreateAnchor()
        {
            //The anchors can't be created of there is no pointer nor plane touched
            if (pointer == null || pointedPlane == null)
            {
                ARDebugManager.Instance.LogError("No pointer or no plane to spawn anchor");
                return;
            }
            //The geometry is limited to 5 vertices, as mentioned in the specification
            if (geometry.anchors.Count >= 5)
            {
                ARDebugManager.Instance.LogError("Too many anchors");
                return;
            }

            //The anchor is attached to the selected plane
            var anchor = anchorManager.AttachAnchor(pointedPlane, new Pose(pointer.position, Quaternion.identity));

            if (anchor == null)
                ARDebugManager.Instance.LogError("Error creating anchor.");

            geometry.anchors.Add(anchor);
            markers.Add(Instantiate(anchorPrefab,anchor.transform));
            
            //This section creates a line between the vertex placed
            if (createLines)
            {
                if (geometry.anchors.Count < 2)
                {
                    ARDebugManager.Instance.LogError("Not enough anchors to create a line");
                    return;
                }
                else
                {
                        //The line is attached to the new anchor placed as child
                        GameObject newLine = Instantiate(helperLinePrefab, anchor.transform);
                        lines.Add(newLine);
                        LineRenderer lineRenderer = newLine.GetComponent<LineRenderer>();
                        //The line goes from the last placed anchor to the second to last
                        lineRenderer.SetPosition(0, geometry.anchors[^1].transform.position);
                        lineRenderer.SetPosition(1, geometry.anchors[^2].transform.position);
                }
            }
            //Reference to the last anchor placed for other processes
            lastAnchor = anchor;
            //Event for any other procedures needed when a new anchor is created
            OnAnchorCreated.Invoke();
        }

        public void RemoveAnchor()
        {
            //If there are no anchors, nothing gets removed
            if (lastAnchor == null)
            {
                ARDebugManager.Instance.LogInfo("No anchor to remove");
                return;
            }
            //Remove the blue marker and line
            GameObject lastMarker = markers[^1];
            markers.Remove(lastMarker);
            Destroy(lastMarker);
            GameObject lastLine = lines[^1];
            lines.Remove(lastLine);
            Destroy(lastLine);
            //Remove the anchor itself
            geometry.anchors.Remove(lastAnchor);
            Destroy(lastAnchor);
            //Reassign the last anchor
            lastAnchor = geometry.anchors.Count > 0 ? geometry.anchors[^1] : null;
        }

        public void RemoveAllLines()
        {
            foreach (GameObject line in lines)
                Destroy(line);
           lines.Clear();
        }
        //Removes both lines and vertex markers
        public void RemoveAllMarkers()
        {
            foreach(GameObject marker in markers)
                Destroy(marker);
            markers.Clear();
            RemoveAllLines();
        }
        //Shows or hides planes as needed
        public void ViewPlanes(bool state)
        {
            foreach(var plane in planeManager.trackables)
                plane.gameObject.SetActive(state);
        }

    }

}
