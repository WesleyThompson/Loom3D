using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Loom3D
{
    public class Loom3D : MonoBehaviour
    {
        private static readonly string path = "Assets/";
        private static readonly string assetType = ".asset";
        private static readonly string meshExtension = "_mesh";
        private static readonly string materialExtension = "_material";

        public static void CreateMeshFromTexture(Texture2D texture, int pixelsPerQuad, float quadSize)
        {
            //Calculate all our necessary mesh ingredients
            ImageSegment[,] imageSegments = CalculateImageSegments(pixelsPerQuad, texture);
            Vector3[] vertices = CalculateVertices(imageSegments, quadSize);
            int[] triangles = CalculateTriangles(imageSegments);
            Vector2[] uvs = CalculateUVs(vertices, texture, imageSegments, quadSize, pixelsPerQuad);

            CreateAndSaveMesh(vertices, triangles, uvs, texture.name);
        }

        private static void CreateAndSaveMesh(Vector3[] vertices, int[] triangles, Vector2[] uvs, string meshName)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.name = meshName;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            AssetDatabase.CreateAsset(mesh, path + meshName + meshExtension + assetType);
        }

        public static void CreateAndSaveMaterial(Texture2D texture)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.name = texture.name + materialExtension;
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Mode", 1); //Set render mode to cutout

            AssetDatabase.CreateAsset(material, path + material.name + ".mat");
        }

        //ImageSegment directly correlates to the mesh's quads
        private struct ImageSegment
        {
            public float alphaSum;
            //In order: bottom left, top left, bottom right, and top right
            public int[] vectorIndices;
        }

        private static ImageSegment[,] CalculateImageSegments(int pixelsPerQuad, Texture2D texture)
        {
            Vector2 dimensions = CalculateSegmentDimensions(pixelsPerQuad, texture);
            ImageSegment[,] imageSegments = new ImageSegment[(int)dimensions.x, (int)dimensions.y];

            //Texture coordinates start at the lower left corner
            for (int i = 0; i < texture.width; i++)
            {
                for (int j = 0; j < texture.height; j++)
                {
                    float alpha = texture.GetPixel(i, j).a;
                    imageSegments[i / pixelsPerQuad, j / pixelsPerQuad].alphaSum += alpha;
                }
            }

            return imageSegments;
        }

        private static Vector2 CalculateSegmentDimensions(int pixelsPerQuad, Texture2D texture)
        {
            Vector2 dimensions = new Vector2();
            dimensions.x = texture.width / pixelsPerQuad;
            dimensions.y = texture.height / pixelsPerQuad;

            if(texture.width % pixelsPerQuad != 0)
            {
                dimensions.x++;
            }
            if(texture.height % pixelsPerQuad != 0)
            {
                dimensions.y++;
            }

            return dimensions;
        }

        private static Vector3[] CalculateVertices(ImageSegment[,] imageSegments, float quadSize)
        {
            List<Vector3> verticeList = new List<Vector3>();

            for(int i = 0; i < imageSegments.GetLength(0); i++)
            {
                for(int j = 0; j < imageSegments.GetLength(1); j++)
                {
                    if(imageSegments[i,j].alphaSum > 0)
                    {
                        float rowCoord = (i + 1) * quadSize;
                        float colCoord = (j + 1) * quadSize;

                        //Calculate the 4 Vectors per quad
                        Vector3[] quadVectors = new Vector3[4];
                        quadVectors[0] = new Vector3(i * quadSize, j * quadSize); //bottomLeft
                        quadVectors[1] = new Vector3(i * quadSize, colCoord); //topLeft
                        quadVectors[2] = new Vector3(rowCoord, j * quadSize); //bottomRight
                        quadVectors[3] = new Vector3(rowCoord, colCoord); //topRight

                        //Check that each of the vertices aren't already in the list
                        //Record the index of each vectice in the imageSegment
                        int index = 0;
                        int[] vertIndices = new int[4];
                        for(int k = 0; k < quadVectors.Length; k++)
                        {
                            index = verticeList.Count;
                            if(!verticeList.Contains(quadVectors[k]))
                            {
                                vertIndices[k] = index;
                                verticeList.Add(quadVectors[k]);
                            }
                            else
                            {
                                vertIndices[k] = verticeList.IndexOf(quadVectors[k]);
                            }
                        }

                        imageSegments[i, j].vectorIndices = vertIndices;
                    }
                }
            }

            return verticeList.ToArray();
        }

        private static int[] CalculateTriangles(ImageSegment[,] imageSegments)
        {
            List<int> triangleList = new List<int>();

            foreach (ImageSegment imageSegment in imageSegments)
            {
                if (imageSegment.alphaSum > 0)
                {
                    int[] indices = imageSegment.vectorIndices;
                    triangleList.Add(indices[0]);
                    triangleList.Add(indices[1]);
                    triangleList.Add(indices[2]);
                    triangleList.Add(indices[2]);
                    triangleList.Add(indices[1]);
                    triangleList.Add(indices[3]);
                }
            }

            return triangleList.ToArray();
        }

        private static Vector2[] CalculateUVs(Vector3[] vertices, Texture2D texture, ImageSegment[,] imageSegments, float quadSize, int pixelsPerQuad)
        {
            Vector2[] uvs = new Vector2[vertices.Length];

            //Assuming all quads are equal size
            float meshWidth = imageSegments.GetLength(0) * quadSize;
            float meshHeight = imageSegments.GetLength(1) * quadSize;

            for(int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = new Vector2((vertices[i].x / meshWidth),
                    (vertices[i].y / meshHeight));
            }

            return uvs;
        }
    }
}