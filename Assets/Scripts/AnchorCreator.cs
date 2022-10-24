using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events;
using UnityEngine.Timeline;

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
        ARAnchorManager anchorManager;
        ARRaycastManager raycastManager;
        ARPlaneManager planeManager;

        Transform pointer;
        ARPlane pointedPlane;
        ARAnchor lastAnchor;
        List<GameObject> lines;
        List<GameObject> markers;

        private void Awake()
        {
            anchorManager = GetComponent<ARAnchorManager>();
            raycastManager = GetComponent<ARRaycastManager>();
            planeManager = GetComponent<ARPlaneManager>();

            pointer = null;
            pointedPlane = null;
            lines = new List<GameObject>();
            markers = new List<GameObject>();
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

            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = touch.position;
            List<RaycastResult> raycastResults = new List<RaycastResult>();

            EventSystem.current.RaycastAll(pointerEventData, raycastResults);

            if(raycastResults.Count > 0)
                return;

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
            if (pointer == null || pointedPlane == null)
            {
                ARDebugManager.Instance.LogError("No pointer or no plane to spawn anchor");
                return;
            }

            if (geometry.anchors.Count >= 5)
            {
                ARDebugManager.Instance.LogError("Too many anchors");
                return;
            }

            var anchor = anchorManager.AttachAnchor(pointedPlane, new Pose(pointer.position, Quaternion.identity));

            if (anchor == null)
                ARDebugManager.Instance.LogError("Error creating anchor.");

            geometry.anchors.Add(anchor);
            markers.Add(Instantiate(anchorPrefab,anchor.transform));
            ARDebugManager.Instance.LogInfo("Anchor created. Count: "+geometry.anchors.Count);
            
            if (createLines)
            {
                if (geometry.anchors.Count < 2)
                {
                    ARDebugManager.Instance.LogError("Not enough anchors to create a line");
                    return;
                }
                else
                {
                    try
                    {
                        ARDebugManager.Instance.LogInfo("Attempting to create line between anchors");
                        //Line is instantiated
                        GameObject newLine = Instantiate(helperLinePrefab, anchor.transform);
                        if (newLine == null)
                            ARDebugManager.Instance.LogError("Error creating line");
                        lines.Add(newLine);
                        //Line renderer is obtained
                        LineRenderer lineRenderer = newLine.GetComponent<LineRenderer>();
                        if (lineRenderer == null)
                            ARDebugManager.Instance.LogError("Error geting line renderer");


                        lineRenderer.SetPosition(0, geometry.anchors[^1].transform.position);
                        lineRenderer.SetPosition(1, geometry.anchors[^2].transform.position);
                        ARDebugManager.Instance.LogInfo("Line created");

                    }catch(Exception e)
                    {
                        ARDebugManager.Instance.LogError(e.Message);

                    }
                }
            }
            lastAnchor = anchor;
            OnAnchorCreated.Invoke();
        }

        public void RemoveAnchor()
        {
            if (lastAnchor == null)
            {
                ARDebugManager.Instance.LogInfo("No anchor to remove");
                return;
            }
            //Remove the blue marker
            GameObject lastMarker = markers[^1];
            markers.Remove(lastMarker);
            Destroy(lastMarker);
            ARDebugManager.Instance.LogInfo("Removed blue marker. Count: " + markers.Count);
            GameObject lastLine = lines[^1];
            lines.Remove(lastLine);
            Destroy(lastLine);
            ARDebugManager.Instance.LogInfo("Removed last line. Count: " + lines.Count);
            //Remove the anchor itself
            geometry.anchors.Remove(lastAnchor);
            Destroy(lastAnchor);
            //Reassign the last anchor
            lastAnchor = geometry.anchors.Count > 0 ? geometry.anchors[^1] : null;
            ARDebugManager.Instance.LogInfo("Removed anchor. Count: "+ geometry.anchors.Count);
        }

        public void RemoveAllLines()
        {
            foreach (GameObject line in lines)
                Destroy(line);
           lines.Clear();
        }

        public void RemoveAllMarkers()
        {
            foreach(GameObject marker in markers)
            {
                Destroy(marker);
            }
            RemoveAllLines();
        }

        public void ViewPlanes(bool state)
        {
            foreach(var plane in planeManager.trackables)
                plane.gameObject.SetActive(state);
        }

        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    }

}
