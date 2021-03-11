/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///VoxelData.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz.
///This class contains the information for reading in the correct order vertices, triangles and faces for voxels, additionally 
///information for chunks and texture atlas size is contained in this class.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelData {
    //Variables
    //Width
    public static readonly int chunkWidth = 16;
    //Height
    public static readonly int chunkHeight = 128;
    //Texture atlas elements size
    public static readonly int textureAtlasSizeInBlocks = 16;
    //World size in chunks
    public static readonly int worldSizeInChunks = 128;

    //Function for getting world size in voxels
    public static int WorldSizeInVoxels {
        get { return worldSizeInChunks * chunkWidth; }
    }

    //Function which normalises the texture atlas size
    public static float NormalizedBlockTextureSize {
        get { 
            return 1f / (float)textureAtlasSizeInBlocks; 
        }
    }

    //The lookup table for verts/vertices
    public static readonly Vector3[] voxelVerts = new Vector3[8] {
        new Vector3(0.0f,0.0f,0.0f),
        new Vector3(1.0f,0.0f,0.0f),
        new Vector3(1.0f,1.0f,0.0f),
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,0.0f,1.0f),
        new Vector3(1.0f,0.0f,1.0f),
        new Vector3(1.0f,1.0f,1.0f),
        new Vector3(0.0f,1.0f,1.0f)
    };

    //The lookup table for voxel faces
    public static readonly Vector3[] faceChecks = new Vector3[6]
    {
        new Vector3(0.0f,0.0f,-1.0f),
        new Vector3(0.0f,0.0f,1.0f),
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,-1.0f,0.0f),
        new Vector3(-1.0f,0.0f,0.0f),
        new Vector3(1.0f,0.0f,0.0f)
    };

    //The lookup table for tris/triangles
    public static readonly int[,] voxelTris = new int[6, 4] {
        //Back,Front,Top,Bottom,Left,Right
        //0 1 2 2 1 3
        //Back face
        {0,3,1,2},
        //Front face
        {5,6,4,7},
        //Top face
        {3,7,2,6},
        //Bottom face
        {1,5,0,4},
        //Left face
        {4,7,0,3},
        //Right face
        {1,2,5,6}
    };

    //The lookup table for voxel uvs
    public static readonly Vector2[] voxelUvs = new Vector2[4] {
        new Vector2(0.0f,0.0f),
        new Vector2(0.0f,1.0f),
        new Vector2(1.0f,0.0f),
        new Vector2(1.0f,1.0f)
    };
}
