using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DebugTriangleNormals : MonoBehaviour {

#if UNITY_EDITOR

    List<Vector3> trisCenters;
    List<Vector3> trisNormals;
    
    [SerializeField] float m_normalSpotRadius = 0.1f, m_normalLineLength = 1f;
    [SerializeField] Color m_normalColor = Color.yellow;

    [Header("Unity or computed normals")]
    [SerializeField] bool m_UnityOrTriangleNormals = true;

    void OnEnable () {
        trisCenters = new List<Vector3>();
        trisNormals = new List<Vector3>();
        Mesh m = GetComponent<Renderer>().GetComponent<MeshFilter>().sharedMesh;
        for (int i = 0; i < m.triangles.Length/3; i++)
        {
            // triangle data
            int tAi = i * 3, tBi = i * 3 + 1, tCi = i * 3 + 2;
            Vector3 tvA = m.vertices[m.triangles[tAi]], tvB = m.vertices[m.triangles[tBi]], tvC = m.vertices[m.triangles[tCi]];
            //Vector2 tuvA = m.uv[m.triangles[tAi]], tuvB = m.uv[m.triangles[tBi]], tuvC = m.uv[m.triangles[tCi]];

            // triangle's barycentre/centroid
            Vector3 TB = (tvA + tvB + tvC) / 3f;

            // triangle's normal
            Vector3 TN = (m_UnityOrTriangleNormals) ?
                TN = (m.normals[m.triangles[tAi]] + m.normals[m.triangles[tBi]] + m.normals[m.triangles[tCi]])/3f :
                Vector3.Cross(tvB - tvA, tvC - tvB).normalized;

            trisCenters.Add(transform.TransformPoint(TB));
            trisNormals.Add(transform.TransformDirection(TN));
        }
	}

    private void OnDrawGizmos()
    {
        if (trisCenters == null || trisNormals == null)
            return;

        Gizmos.color = m_normalColor;
        for (int i = 0; i < trisCenters.Count; i++)
        {
            Gizmos.DrawSphere(trisCenters[i], m_normalSpotRadius);
            Gizmos.DrawLine(trisCenters[i], trisCenters[i] + trisNormals[i] * m_normalLineLength);
        }
        Gizmos.color = Color.white;
    }

#endif

}