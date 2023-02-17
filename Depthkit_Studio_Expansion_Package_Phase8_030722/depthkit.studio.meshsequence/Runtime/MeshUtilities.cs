using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Depthkit
{
    public static class MeshUtilities
    {
        public static void WriteMeshPLYFile(in string filename, in Vector3[] vertices, in Vector3[] normals, in Vector2[] uvs, in int[] indexBuffer, in ImageFormat textureFormat)
        {
            BinaryWriter writer = new BinaryWriter(new FileStream(filename, FileMode.Create), Encoding.ASCII);

            bool useUvs = uvs != null;

            // Write the headers for vertices with normals and triangle faces
            writer.Write(System.Text.Encoding.ASCII.GetBytes("ply\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("format binary_little_endian 1.0\n"));
            if (useUvs) writer.Write(System.Text.Encoding.ASCII.GetBytes("comment TextureFile " + Path.GetFileNameWithoutExtension(filename) + "." + textureFormat.ToString().ToLower() +"\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes($"element vertex {vertices.Length}\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("property float x\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("property float y\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("property float z\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("property float nx\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("property float ny\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("property float nz\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes($"element face {indexBuffer.Length / 3}\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("property list uchar int vertex_index\n"));
            if (useUvs)
                writer.Write(System.Text.Encoding.ASCII.GetBytes("property list uchar float texcoord\n"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("end_header\n"));

            // write the verts
            for (int i = 0; i < vertices.Length; i++)
            {
                writer.Write(BitConverter.GetBytes(vertices[i].x));
                writer.Write(BitConverter.GetBytes(vertices[i].y));
                writer.Write(BitConverter.GetBytes(vertices[i].z));

                writer.Write(BitConverter.GetBytes(normals[i].x));
                writer.Write(BitConverter.GetBytes(normals[i].y));
                writer.Write(BitConverter.GetBytes(normals[i].z));
            }

            // write faces
            for (int i = 0; i < indexBuffer.Length; i += 3)
            {
                writer.Write((byte)3u); // 3 verts
                writer.Write(BitConverter.GetBytes(indexBuffer[i]));
                writer.Write(BitConverter.GetBytes(indexBuffer[i + 1]));
                writer.Write(BitConverter.GetBytes(indexBuffer[i + 2]));

                if (useUvs)
                {
                    writer.Write((byte)6u); // UVs
                    writer.Write(BitConverter.GetBytes(uvs[i].x));
                    writer.Write(BitConverter.GetBytes(uvs[i].y));
                    writer.Write(BitConverter.GetBytes(uvs[i + 1].x));
                    writer.Write(BitConverter.GetBytes(uvs[i + 1].y));
                    writer.Write(BitConverter.GetBytes(uvs[i + 2].x));
                    writer.Write(BitConverter.GetBytes(uvs[i + 2].y));
                }
            }

            writer.Close();
        }

        // Welds duplicate verts
        // Based on http://answers.unity.com/answers/1658955/view.html
        public static void WeldVerts(ref Vector3[] vertices, ref Vector3[] normals, ref Vector2[] uvs, ref int[] indexBuffer)
        {
            Dictionary<Vector3, int> duplicateHashTable = new Dictionary<Vector3, int>();
            List<int> newVerts = new List<int>();
            List<int> newIndices = new List<int>();
            int[] map = new int[indexBuffer.Length];

            // weld and skip degenerate triangles
            for (int ti = 0; ti < indexBuffer.Length; ti += 3)
            {
                ref Vector3 v0 = ref vertices[indexBuffer[ti]];
                ref Vector3 v1 = ref vertices[indexBuffer[ti + 1]];
                ref Vector3 v2 = ref vertices[indexBuffer[ti + 2]];
                int vh0 = v0.GetHashCode();
                int vh1 = v1.GetHashCode();
                int vh2 = v2.GetHashCode();

                // Skip degenerate triangles
                // Note: the Vector3 operator == appears to be using an epsilon and may return true even if the vectors are not identical
                // This can lead to skipping very small triangles. The work around is to also compare the hash codes
                if ((vh0 == vh1 || vh0 == vh2 || vh1 == vh2) && (v0 == v1 || v0 == v2 || v1 == v2)) continue;

                for (int vi = 0; vi < 3; vi++)
                {
                    int i = ti + vi;
                    int originalVertIndex = indexBuffer[i];

                    ref Vector3 key = ref vertices[originalVertIndex];

                    if (!duplicateHashTable.ContainsKey(key))
                    {
                        duplicateHashTable.Add(key, newVerts.Count); // add new vert index to hash table
                        map[i] = newVerts.Count; // map original index to new index
                        newVerts.Add(originalVertIndex);
                    }
                    else
                    {
                        map[i] = duplicateHashTable[key]; // extract new vert index from hash table
                    }
                    newIndices.Add(i);
                }
            }
            duplicateHashTable.Clear();

            // create new vertices
            var weldedVerts = new Vector3[newVerts.Count];
            var weldedNormals = new Vector3[newVerts.Count];
            for (int i = 0; i < newVerts.Count; i++)
            {
                int originalVertId = newVerts[i];
                weldedVerts[i] = vertices[originalVertId];
                weldedNormals[i] = normals[originalVertId];
            }
            newVerts.Clear();

            // map the triangle to the new vertices
            var weldedIndices = new int[newIndices.Count];
            var weldedUVs = new Vector2[newIndices.Count];
            for (int i = 0; i < weldedIndices.Length; i++)
            {
                weldedIndices[i] = map[newIndices[i]];
                weldedUVs[i] = uvs[newIndices[i]];
            }
            newIndices.Clear();

            indexBuffer = weldedIndices;
            vertices = weldedVerts;
            normals = weldedNormals;
            uvs = weldedUVs;
        }

        // Scale and Mirror
        // Inverts winding order if necessary
        public static void ScaleAndMirror(ref Vector3[] vertices, ref Vector3[] normals, ref Vector2[] uvs, ref int[] indexBuffer, float scaleFactor, bool mirrorX, bool mirrorY, bool mirrorZ)
        {
            Vector3 mirror = new Vector3(mirrorX ? -1f : 1f, mirrorY ? -1f : 1f, mirrorZ ? -1f : 1f);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= scaleFactor;
                vertices[i].x *= mirror.x;
                vertices[i].y *= mirror.y;
                vertices[i].z *= mirror.z;

                normals[i].x *= mirror.x;
                normals[i].y *= mirror.y;
                normals[i].z *= mirror.z;
            }

            // Invert winding order if the number of mirrored axes is odd
            bool invertWinding = ((mirrorX ? 1 : 0) + (mirrorY ? 1 : 0) + (mirrorZ ? 1 : 0)) % 2 != 0;

            if (invertWinding)
            {
                for (int i = 0; i < indexBuffer.Length; i += 3)
                {
                    (indexBuffer[i], indexBuffer[i + 2]) = (indexBuffer[i + 2], indexBuffer[i]);
                    (uvs[i], uvs[i + 2]) = (uvs[i + 2], uvs[i]);
                }
            }
        }

        private static Vector3 QuantizeVector(in Vector3 v, in float quantum)
        {
            return new Vector3(
                Mathf.Round(v.x / quantum),
                Mathf.Round(v.y / quantum),
                Mathf.Round(v.z / quantum)
            ) * quantum;
        }

        public static void CollapseVerts(ref Vector3[] vertices, float thresholdDistance)
        {
            // Merge close verts
            if (thresholdDistance > 0.0f)
            {
                Dictionary<Vector3, List<int>> toCollapseHashTable = new Dictionary<Vector3, List<int>>();

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 roundedVert = QuantizeVector(vertices[i], thresholdDistance);
                    if (!toCollapseHashTable.ContainsKey(roundedVert))
                    {
                        toCollapseHashTable[roundedVert] = new List<int>();
                    }
                    toCollapseHashTable[roundedVert].Add(i);
                }

                bool[] visited = new bool[vertices.Length];
                for (int i = 0; i < visited.Length; i++)
                {
                    visited[i] = false;
                }

                List<Vector3> offsets = new List<Vector3>();
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            offsets.Add(new Vector3(x, y, z) * thresholdDistance);
                        }
                    }
                }

                for (int i = 0; i < vertices.Length; i++)
                {
                    if (!visited[i])
                    {
                        visited[i] = true;

                        // get all verts near the current vert
                        foreach (var offset in offsets)
                        {
                            Vector3 bin = QuantizeVector(offset + vertices[i], thresholdDistance);
                            if (!toCollapseHashTable.ContainsKey(bin)) continue;
                            foreach (var vertexId in toCollapseHashTable[bin])
                            {
                                if (!visited[vertexId] && Vector3.Distance(vertices[i], vertices[vertexId]) < thresholdDistance * 0.5f)
                                {
                                    // move them all to the current vert if they are within range
                                    visited[vertexId] = true;
                                    vertices[vertexId] = vertices[i];
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void RemoveDuplicateTriangles(ref int[] indexBuffer)
        {
            Dictionary<Vector3Int, bool> duplicateTriangleMap = new Dictionary<Vector3Int, bool>();
            int[] sortedTriangle = new int[3];
            List<int> newTris = new List<int>();
            for (int ti = 0; ti < indexBuffer.Length; ti += 3)
            {
                sortedTriangle[0] = indexBuffer[ti];
                sortedTriangle[1] = indexBuffer[ti + 1];
                sortedTriangle[2] = indexBuffer[ti + 2];
                Array.Sort(sortedTriangle);

                Vector3Int key = new Vector3Int(sortedTriangle[0], sortedTriangle[1], sortedTriangle[2]);

                if (!duplicateTriangleMap.ContainsKey(key))
                {
                    duplicateTriangleMap[key] = true;
                    newTris.Add(indexBuffer[ti]);
                    newTris.Add(indexBuffer[ti + 1]);
                    newTris.Add(indexBuffer[ti + 2]);
                }
            }
            indexBuffer = newTris.ToArray();
        }

        public static void RemoveNonManifoldEdges(ref Vector3[] vertices, ref Vector3[] normals, ref int[] indexBuffer)
        {
            // Map of vertexid -> list of faces (index into triangle buffer of first vertex in the face)
            int[] vertexToFacesMap = new int[vertices.Length];
            for (int i = 0; i < vertexToFacesMap.Length; i++)
            {
                vertexToFacesMap[i] = 0;
            }

            // Dictionary of edge (vertID, vertID) to number of times the edge is seen.
            Dictionary<Tuple<int, int>, List<int>> edgeToFaceMap = new Dictionary<Tuple<int, int>, List<int>>();

            for (int ti = 0; ti < indexBuffer.Length; ti += 3)
            {
                for (int vi = 0; vi < 3; vi++)
                {
                    int i = vi + ti;
                    int vertexId = indexBuffer[i];
                    vertexToFacesMap[vertexId]++; // increment face count for this vert

                    int vi2 = (vi + 1) % 3;
                    int i2 = vi2 + ti;
                    int vertexId2 = indexBuffer[i2];
                    Tuple<int, int> edge = new Tuple<int, int>(Math.Min(vertexId, vertexId2), Math.Max(vertexId, vertexId2));
                    if (!edgeToFaceMap.ContainsKey(edge))
                    {
                        edgeToFaceMap[edge] = new List<int>();
                    }
                    edgeToFaceMap[edge].Add(ti);
                }
            }

            // find all edges with count > 2
            foreach (var keyValuePair in edgeToFaceMap)
            {
                if (keyValuePair.Value.Count > 2)
                {
                    int destinationVertexId = -1;
                    List<int> toCollapseVertexIds = new List<int>();
                    // find all associated faces to the verts in the edge
                    foreach (var triangleId in keyValuePair.Value)
                    {
                        // find if any verts are only used in a single face
                        for (int vi = 0; vi < 3; vi++)
                        {
                            int vertexId = indexBuffer[triangleId + vi];
                            if (vertexToFacesMap[vertexId] == 1)
                            {
                                toCollapseVertexIds.Add(vertexId);
                            }
                            else if (destinationVertexId == -1)
                            {
                                destinationVertexId = vertexId;
                            }
                        }
                    }
                    if (toCollapseVertexIds.Count > 0 && destinationVertexId >= 0)
                    {
                        foreach (int vertexId in toCollapseVertexIds)
                        {
                            // move the vertex to the destination. Will be welded later
                            vertices[vertexId] = vertices[destinationVertexId];
                        }
                    }
                    else
                    {
                        Debug.Log("Failed to repair non-manifold edge");
                    }
                }
            }
        }
    }
}
