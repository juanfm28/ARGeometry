using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

namespace ARGeometry
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent (typeof(MeshFilter))]
    public class Geometry : MonoBehaviour
    {
        //Material to be applied to the plane
        public Material appliedMaterial;
        [Header("References")]
        MeshRenderer geometryRenderer;
        MeshFilter geometryFilter;

        [HideInInspector]
        public List<ARAnchor> anchors;
        bool isUpsideDown;
        void Awake()
        {
            //Get all references and initialize 
            anchors = new List<ARAnchor>();
            geometryRenderer = GetComponent<MeshRenderer>();
            geometryFilter = GetComponent<MeshFilter>();
            isUpsideDown = false;
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
            //Vertex list must be between 3 and 5 points
            if (anchors.Count < 3 || anchors.Count > 5)
            {
                ARDebugManager.Instance.LogInfo("It is imposible to create a plane with this many points");
                return;
            }

            //The Geometry is attached to the first anchor
            transform.parent = anchors[0].transform;

            geometryRenderer.material = appliedMaterial;

            //Initialize the mesh variables
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            //The first vertex must be the center of the plane
            vertices.Add(CalculateCentroid());
            foreach (var anchor in anchors)
                vertices.Add(anchor.transform.position);

            //Triangles are created with the central vertex and two of the external vertices.
            for (int i = 1; i < vertices.Count; i++)
            {
                //One side
                triangles.Add(0);
                triangles.Add(i == vertices.Count - 1 ? 1 : i + 1);
                triangles.Add(i);
            }
            
            //Apply information of the mesh to the filter 
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = CalculateUV(vertices).ToArray();  //Initially there is no offset
            mesh.RecalculateNormals();
            
            //if the normals of the plains are pointing down, the plane won't be seen.
            //This means that when the plane is pointing down, the angle between its normals and the Up vector should be 180? (or close)
            if (Mathf.Abs(Vector3.Angle(mesh.normals[0], transform.up)) >= 120f)
            {
                ARDebugManager.Instance.LogInfo("This is upside down");
                //If the geometry is upside down, redo with the oposite winding to form triangles
                mesh.triangles = null;
                triangles.Clear();
                //Triangles are created with the central vertex and two of the external vertices.
                for (int i = 1; i < vertices.Count; i++)
                {
                    //One side
                    triangles.Add(0);
                    triangles.Add(i);
                    triangles.Add(i == vertices.Count - 1 ? 1 : i + 1);
                }
                mesh.triangles = triangles.ToArray();
                mesh.RecalculateNormals();
            }

            geometryFilter.mesh = mesh;
            
        }

        List<Vector2> CalculateUV(List<Vector3> vertices, float offset = 0)
        {
            List<Vector2> uv = new List<Vector2>();
            //the maximum X value will be taken as 1 for the UV coordinates
            //same for the maximum Z value. This creates a square that contains all the vertices
            float maxX = Mathf.NegativeInfinity, minX = Mathf.Infinity;
            float maxZ = Mathf.NegativeInfinity, minZ = Mathf.Infinity;

            foreach(var v in vertices)
            {
                maxX = Mathf.Max(v.x, maxX);
                minX = Mathf.Min(v.x, minX);
                maxZ = Mathf.Max(v.z, maxZ);
                minZ = Mathf.Min(v.z, minZ);
            }
            //Determine where in this frame of reference each vertex is placed and maps this as is uv coordinates
            foreach(var v in vertices)
                uv.Add(new Vector2(Mathf.InverseLerp(minX,maxX,v.x) + (isUpsideDown ? -offset : offset),Mathf.InverseLerp(minZ,maxZ,v.z)));
            
            return uv;
        }

        public void ChangeMaterial(Material mat)
        {
            geometryRenderer.material = mat;
        }

        public void PanMaterial(float offset)
        {
            geometryFilter.mesh.uv = null;
            geometryFilter.mesh.uv = CalculateUV(geometryFilter.mesh.vertices.ToList<Vector3>(), offset).ToArray();
            geometryFilter.mesh.RecalculateNormals();
        }

        public void RestartGeometry()
        {
            foreach (var anchor in anchors)
                Destroy(anchor);
            
            anchors.Clear();
            geometryFilter.mesh = null;
            transform.rotation = Quaternion.identity;
            isUpsideDown = false;
        }
    }
}
