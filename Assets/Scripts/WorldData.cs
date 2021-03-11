/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///WorldData.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz.
///This is used for loading, adding and requesting chunk data for the procedurally generated voxel world.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class WorldData {
    //Variables
    public string worldName = "World";
    public int seed;
    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>();

    //This function adds a passed in chunks chunkdata to the modified chunks list
    public void AddToModifiedChunkList (ChunkData chunk) {
        if (!modifiedChunks.Contains(chunk)) {
            modifiedChunks.Add(chunk);
        }
    }

    //Function for assigning the seed and world name
    public WorldData (string _worldName,int _seed) {
        worldName = _worldName;
        seed = _seed;
    }

    //Function for assigning the worldname and seed from world data parameter 
    public WorldData(WorldData wD) {
        worldName = wD.worldName;
        seed = wD.seed;
    }

    //Function for requesting a chunk, this is done inside a thread lock
    public ChunkData RequestChunk(Vector2Int coordinate, bool create) {
        //Chunk data to be returned
        ChunkData c;
        //Thread lock with chunk data being assigned inside it
        lock (World.Instance.ChunkListThreadLock) {

            if (chunks.ContainsKey(coordinate)) {
                c = chunks[coordinate];
            }
            else if (!create) {
                c = null;
            }
            else {
                LoadChunk(coordinate);
                c = chunks[coordinate];
            }
        }
        //returning chunk data
        return c;
    }

    //This function loads a chunk at the given coordinate in or otherwise populates a chunk at given positon
    public void LoadChunk (Vector2Int coordinate) {
        //If chunk contains key then return
        if (chunks.ContainsKey(coordinate)) {
            return;
        }
        //Load the chunk in
        ChunkData chunk = SaveSystem.LoadChunk(worldName, coordinate);
        //If the chunk is not null add it to chunks and return
        if (chunk != null) {
            chunks.Add(coordinate, chunk);
            return;
        }
        //Add new chunk otherwise and populate it at position of coordinate
        chunks.Add(coordinate, new ChunkData(coordinate));
        chunks[coordinate].Populate();
    }

    //Function returning if the voxel at the given position is within the world and returning true if so or else returning false
    bool IsVoxelInWorld(Vector3 position) {
        if (position.x >= 0 && position.x < VoxelData.WorldSizeInVoxels && position.y >= 0 && position.y < VoxelData.chunkHeight && position.z >= 0 && position.z < VoxelData.WorldSizeInVoxels) {
            return true;
        }
        else {
            return false;
        }
    }

    //This function sets a voxel blocktype value in a chunk and then adds calls the function to update chunk set voxel is in
    public void SetVoxel (Vector3 position, byte value) {
        //Ignore voxel is outside of world
        if (!IsVoxelInWorld(position)) {
            return;
        }

        //Find out the chunkcoordinate vlaue of voxels chunk
        int x = (int)(position.x / VoxelData.chunkWidth);
        int z = (int)(position.z / VoxelData.chunkWidth);
        //Reverse values to get chunk position
        x *= VoxelData.chunkWidth;
        z *= VoxelData.chunkWidth;
        //Check if chunk alreadt exists and if not create chunk
        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);
        //Create a vector3int from the position of the voxel within the chunk
        Vector3Int voxel = new Vector3Int((int)(position.x - x), (int)position.y, (int)(position.z - z));
        //Set voxel in our chunk
        chunk.map[voxel.x, voxel.y, voxel.z].id = value;
        //Calls function to add the chunk to the modified chunk list
        AddToModifiedChunkList(chunk);
    }

    //Function returns a voxel state from a position parameter passed into it
    public VoxelState GetVoxel(Vector3 position) {
        //Ignore voxel is outside of world
        if (!IsVoxelInWorld(position)) {
            return null;
        }

        //Find out the chunkcoordinate vlaue of voxels chunk
        int x = (int)(position.x / VoxelData.chunkWidth);
        int z = (int)(position.z / VoxelData.chunkWidth);
        //Reverse values to get chunk position
        x *= VoxelData.chunkWidth;
        z *= VoxelData.chunkWidth;
        //Check if chunk alreadt exists and if not create chunk
        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);
        //Create a vector3int from the position of the voxel within the chunk
        Vector3Int voxel = new Vector3Int((int)(position.x - x), (int)position.y, (int)(position.z - z));
        //Set voxel in our chunk
        return chunk.map[voxel.x, voxel.y, voxel.z];
    }
}
