using System.Collections.Generic;
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
        Mesh mesh;
        // Start is called before the first frame update
        void Awake()
        {
            anchors = new List<ARAnchor>();
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

            GetComponent<MeshFilter>().mesh = mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            vertices.Add(CalculateCentroid());
            foreach (var anchor in anchors)
                vertices.Add(anchor.transform.position);

            for (int i = 1; i < vertices.Count; i++)
            {
                triangles.Add(0);
                triangles.Add(i == vertices.Count - 1 ? 1 : i + 1);
                triangles.Add(i);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = CalculateUV(vertices).ToArray();
            mesh.RecalculateNormals();
            geometryRenderer.material = appliedMaterial;
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
                uv.Add(new Vector2(Mathf.InverseLerp(minX,maxX,v.x),Mathf.InverseLerp(minZ,maxZ,v.z)));

            return uv;
        }

        public void ChangeMaterial(Material mat)
        {
            geometryRenderer.material = mat;
        }

        public void PanMaterial(float offset)
        {
            if(geometryRenderer.material != null)
                geometryRenderer.material.SetTextureOffset(0, new Vector2(offset,0));
        }

        public void RestartGeometry()
        {
            foreach (var anchor in anchors)
            {
                Destroy(anchor);
            }
            anchors.Clear();
        }
    }
}
