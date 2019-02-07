using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

//TODO refactor
//TODO add saving object
//TODO add 2 sided option
public class Loom : MonoBehaviour {

    public Texture2D texture;
    [Range(1,256)]
    public int segmentResolution;
    [Tooltip("How big, in world-space units, each segment is")]
    public float segmentSize;
    public string meshName;
    public bool twoSided;

    private ImageSegment[,] imageSegments;
    private List<Vector3> vertices;
    private List<int> triangles;
    public Mesh mesh;
    private List<Vector2> uv;

    //Shader settings
    private string shaderType = "Standard";
    private Material material;

    private struct ImageSegment
    {
        public float alphaSum;
        //In order: bottom left, top left, bottom right, and top right
        public int[] vectorIndices;
    }

    private void Awake()
    {
        CalculateImageSegments(ref imageSegments, segmentResolution, texture);
        CalculateVertices(ref vertices, imageSegments, segmentSize);
        CalculateTriangles(ref imageSegments);
        CalculateUVs(ref uv, vertices);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh = new Mesh();
        mesh.name = meshName;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals(); //This may not work later. Migh have to assign normals per vertex

        //Setup material, order is important
        material = new Material(Shader.Find(shaderType));
        material.name = meshName + "_mat";
        material.SetTexture("_MainTex", texture);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetFloat("_Mode", 1); //Set render mode to CutOut
        
        MeshRenderer meshRenderer = this.GetComponent<MeshRenderer>();
        meshRenderer.material = material;

        Cloth cloth = gameObject.AddComponent<Cloth>();
        cloth.useGravity = false;
    }

    private void CalculateUVs(ref List<Vector2> uv, List<Vector3> vertices)
    {
        uv = new List<Vector2>();
        //TODO this calculation will change when segments can have variable length
        float meshWidth = imageSegments.Length * segmentSize;
        float meshHeight = imageSegments.LongLength * segmentSize;

        //To allow us to have no tiling on the material
        float tilingWidthMultiplier = (texture.height / segmentResolution);
        float tilingHeightMultiplier = (texture.width / segmentResolution);

        foreach (Vector3 vert in vertices)
        {
            uv.Add(new Vector2((vert.x / meshWidth) * tilingWidthMultiplier, (vert.y / meshHeight) * tilingHeightMultiplier));
        }
    }

    private void CalculateTriangles(ref ImageSegment[,] imageSegments)
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

    [ContextMenu("Save Mesh+Material")]
    public void SaveMeshNMaterial()
    {
        SaveMeshNMaterial(mesh, material);
    }

    private void SaveMeshNMaterial(Mesh mesh, Material material)
    {
        //TODO This doesn't work need to have the whole object I think
        AssetDatabase.CreateAsset(mesh, "Assets" + "/" + meshName + ".fbx");
        //AssetDatabase.CreateAsset(material, Application.dataPath);
        AssetDatabase.SaveAssets();
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
