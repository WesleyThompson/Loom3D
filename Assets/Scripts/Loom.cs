using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Loom : MonoBehaviour {

    public Texture2D texture;
    [Range(1,16)]
    public int segmentResolution;
    [Tooltip("How big, in world-space units, each segment is")]
    public float segmentSize;

    private ImageSegment[,] imageSegments;
    private List<Vector3> vertices;
    private List<int> triangles;
    private Mesh mesh;

    private struct ImageSegment
    {
        public float alphaSum;
        //In order: bottom left, top left, bottom right, and top right
        public int[] vectorIndices;
    }

    private void Awake()
    {
        Debug.Log(texture.name + ":" + texture.width + "x" + texture.height);
        Debug.Log(texture.GetPixel(0, 0).a);

        Debug.Log(CalculateSegmentDimensions(segmentResolution, texture));
        CalculateImageSegments(ref imageSegments, segmentResolution, texture);
        CalculateVertices(ref vertices, imageSegments, segmentSize);
        CalculateTriangles(imageSegments);

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "meshByLoom";
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    private void CalculateTriangles(ImageSegment[,] imageSegments)
    {
        triangles = new List<int>();

        foreach(ImageSegment imageSegment in imageSegments)
        {
            if(imageSegment.alphaSum > 0)
            {
                int[] indices = imageSegment.vectorIndices;
                triangles.Add(indices[0]);
                triangles.Add(indices[1]);
                triangles.Add(indices[2]);
                triangles.Add(indices[2]);
                triangles.Add(indices[1]);
                triangles.Add(indices[3]);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(vertices == null)
        {
            return;
        }
        else
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < vertices.Count; i++)
            {
                Gizmos.DrawSphere(vertices[i], 0.05f);
            }
        }
    }

    /// <summary>
    /// Calculates the width and height in segments
    /// </summary>
    /// <param name="segmentResolution">How wide and tall the segment is in pixels</param>
    /// <param name="texture"></param>
    /// <returns>The width and height in segments (x being width..)</returns>
    private Vector2 CalculateSegmentDimensions(int segmentResolution, Texture2D texture)
    {
        Vector2 dimensions = new Vector2();
        dimensions.x = texture.width / segmentResolution;
        if(texture.width % segmentResolution != 0)
        {
            dimensions.x++;
        }
        dimensions.y = texture.height / segmentResolution;
        if(texture.height % segmentResolution != 0)
        {
            dimensions.y++;
        }

        return dimensions;
    }

    private void CalculateImageSegments(ref ImageSegment[,] imageSegments, int segmentResolution, Texture2D texture)
    {
        Vector2 segDimensions = CalculateSegmentDimensions(segmentResolution, texture);
        imageSegments = new ImageSegment[(int)segDimensions.x, (int)segDimensions.y];

        //Texture coordinates start at lower left corner.
        for(int i = 0; i < texture.width; i++)
        {
            for(int j = 0; j < texture.height; j++)
            {
                float alpha = texture.GetPixel(i, j).a;
                imageSegments[i / segmentResolution, j / segmentResolution].alphaSum += alpha;
            }
        }

    }

    private void CalculateVertices(ref List<Vector3> vertices, ImageSegment[,] imageSegments, float segmentSize)
    {
        vertices = new List<Vector3>();

        for (int i = 0; i < imageSegments.GetLength(0); i++)
        {
            for (int j = 0; j < imageSegments.GetLength(1); j++)
            {
                if (imageSegments[i, j].alphaSum > 0)
                {
                    //Create 4 vectors for our square segment
                    float rCoord = (i + 1) * segmentSize;
                    float cCoord = (j + 1) * segmentSize;
                    Vector3 bottomLeft = new Vector3(i * segmentSize, j * segmentSize);
                    Vector3 bottomRight = new Vector3(rCoord, j * segmentSize);
                    Vector3 topLeft = new Vector3(i * segmentSize, cCoord);
                    Vector3 topRight = new Vector3(rCoord, cCoord); //This one will always be a new coordinate

                    //Make sure none of our vectors are already in our vector list
                    bool bL = false;
                    bool bR = false;
                    bool tL = false;
                    foreach (Vector3 v in vertices)
                    {
                        if (!bL && v == bottomLeft)
                        {
                            bL = true;
                        }
                        else if (!bR && v == bottomRight)
                        {
                            bR = true;
                        }
                        else if (!tL && v == topLeft)
                        {
                            tL = true;
                        }

                        if (bL && bR && tL)
                        {
                            break;
                        }
                    }
                    //If not add it
                    //TODO fix all this bs
                    int[] vertIndices = new int[4];
                    if (!bL)
                    {
                        vertIndices[0] = vertices.Count;
                        vertices.Add(bottomLeft);
                    }
                    else
                    {
                        //This value shouldn't ever be < 0
                        int value = vertices.IndexOf(bottomLeft);
                        if(value >= 0)
                        {
                            vertIndices[0] = value;
                        }
                    }
                    if (!tL)
                    {
                        vertIndices[1] = vertices.Count;
                        vertices.Add(topLeft);
                    }
                    else
                    {
                        //This value shouldn't ever be < 0
                        int value = vertices.IndexOf(topLeft);
                        if (value >= 0)
                        {
                            vertIndices[1] = value;
                        }
                    }
                    if (!bR)
                    {
                        vertIndices[2] = vertices.Count;
                        vertices.Add(bottomRight);
                    }
                    else
                    {
                        //This value shouldn't ever be < 0
                        int value = vertices.IndexOf(bottomRight);
                        if (value >= 0)
                        {
                            vertIndices[2] = value;
                        }
                    }
                    vertIndices[3] = vertices.Count;
                    vertices.Add(topRight);

                    imageSegments[i, j].vectorIndices = vertIndices;
                }
            }
        }
    }
}
