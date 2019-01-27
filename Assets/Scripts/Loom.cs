using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loom : MonoBehaviour {

    public Texture2D texture;
    [Range(1,16)]
    public int segmentResolution;
    [Tooltip("How big, in world-space units, each segment is")]
    public float segmentSize;

    private float[,] imageSegments;
    private Vector3[] vertices;

    private void Awake()
    {
        Debug.Log(texture.name + ":" + texture.width + "x" + texture.height);
        Debug.Log(texture.GetPixel(0, 0).a);

        Debug.Log(CalculateSegmentDimensions(segmentResolution, texture));
        CalculateImageSegments(ref imageSegments, segmentResolution, texture);

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

    private void CalculateImageSegments(ref float[,] imageSegments, int segmentResolution, Texture2D texture)
    {
        Vector2 segDimensions = CalculateSegmentDimensions(segmentResolution, texture);
        imageSegments = new float[(int)segDimensions.x, (int)segDimensions.y];

        //Texture coordinates start at lower left corner.
        for(int i = 0; i < texture.width; i++)
        {
            for(int j = 0; j < texture.height; j++)
            {
                float alpha = texture.GetPixel(i, j).a;
                imageSegments[i / segmentResolution, j / segmentResolution] += alpha;
            }
        }

    }
}
