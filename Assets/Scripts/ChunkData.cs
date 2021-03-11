/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///ChunkData.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz, also includes 
///This class is for the chunk data of chunks
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData {
    //16x16 not 1x1
    int x;
    int y;
    //Gets and sets position
    public Vector2Int position {

        get {
            return new Vector2Int(x, y);
        }
        set {
            x = value.x;
            y = value.y;
        }
    }

    //This function assigns position to given vector2int
    public ChunkData(Vector2Int chunkPosition) { 
        position = chunkPosition; 
    }

    //This function assigns position to given x and z parameter
    public ChunkData(int _x, int _z) {
        position = new Vector2Int(_x, _z);
    }

    //Hide all the information from the unity inspector
    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    //This method populates a chunk with voxels
    public void Populate() {
        //Looping through the x,y and z of the chunk being populated
        for (int y = 0; y < VoxelData.chunkHeight; y++) {
            for (int x = 0; x < VoxelData.chunkWidth; x++) {
                for (int z = 0; z < VoxelData.chunkWidth; z++) {
                    //Assign a voxel state at this point in the chunk maps multidimensional array
                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(new Vector3(x + position.x, y, z + position.y)));
                }
            }
        }
        //This chunk is added to the modified chunk list (making it be update in world instance)
        World.Instance.worldData.AddToModifiedChunkList(this);
    }
}
