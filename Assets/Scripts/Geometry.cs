using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARGeometry
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent (typeof(MeshFilter))]
    public class Geometry : MonoBehaviour
    {
        public List<ARAnchor> anchors;
        public Material appliedMaterial;

        MeshRenderer geometryRenderer;
        MeshFilter geometryFilter;
        Mesh mesh;
        float texOffset = 0f;
        // Start is called before the first frame update
        void Awake()
        {
            anchors = new List<ARAnchor>();
            geometryRenderer = GetComponent<MeshRenderer>();
            geometryFilter = GetComponent<MeshFilter>();
        }

        public Vector3 CalculateCentroid()
        {
            Vector3 avgPos = new Vector3(0, 0, 0);
            foreach(ARAnchor anchor in anchors)
                avgPos += anchor.transform.position;

            avgPos /= anchors.Count;

            return avgPos;
        }

        public void CreatePlane()
        {
            if (anchors.Count < 3 || anchors.Count > 5)
            {
                ARDebugManager.Instance.LogInfo("Not enough points to create");
                return;
            }

            transform.parent = anchors[0].transform;

            geometryRenderer.material = appliedMaterial;
            geometryFilter.mesh = mesh = new();
            List<Vector3> vertices = new();
            List<int> triangles = new();

            vertices.Add(CalculateCentroid());
            foreach (var anchor in anchors)
                vertices.Add(anchor.transform.position);

            ARDebugManager.Instance.LogInfo("Vertices: "+vertices.Count);

            for (int i = 1; i < vertices.Count; i++)
            {
                triangles.Add(0);
                triangles.Add(i == vertices.Count - 1 ? 1 : i + 1);
                triangles.Add(i);
            }

            ARDebugManager.Instance.LogInfo("Triangles: " + (triangles.Count/3));

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = CalculateUV(vertices).ToArray();
            mesh.RecalculateNormals();
        }

        List<Vector2> CalculateUV(List<Vector3> vertices)
        {
            List<Vector2> uv = new List<Vector2>();

            float maxX = Mathf.NegativeInfinity, minX = Mathf.Infinity;
            float maxZ = Mathf.NegativeInfinity, minZ = Mathf.Infinity;

            foreach(var v in vertices)
            {
                maxX = Mathf.Max(v.x, maxX);
                minX = Mathf.Min(v.x, minX);
                maxZ = Mathf.Max(v.z, maxZ);
                minZ = Mathf.Min(v.z, minZ);
            }

            foreach(var v in vertices)
                uv.Add(new Vector2(Mathf.InverseLerp(minX,maxX,v.x) + texOffset,Mathf.InverseLerp(minZ,maxZ,v.z)));
            
            return uv;
        }

        public void ChangeMaterial(Material mat)
        {
            geometryRenderer.material = mat;
        }

        public void PanMaterial(float offset)
        {
            texOffset = offset;
            geometryFilter.mesh.uv = null;
            geometryFilter.mesh.uv = CalculateUV(geometryFilter.mesh.vertices.ToList<Vector3>()).ToArray();
            geometryFilter.mesh.RecalculateNormals();
        }

        public void RestartGeometry()
        {
            foreach (var anchor in anchors)
            {
                Destroy(anchor);
            }
            anchors.Clear();
            geometryFilter.mesh = null;
        }
    }
}
