/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///Chunk.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz.
///This is class is used for chunks of voxel cubes and the functionality the chunks need
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
public class Chunk 
{
    //Variables
    #region Variables
    public ChunkCoordinate coordinate;
    GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private int vertexIndex = 0;
    //Mesh and texture/material related
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<int> transparentTriangles = new List<int>();
    private Material[] materials = new Material[2];
    private List<Vector2> uvs = new List<Vector2>();
    private List<Vector3> normals = new List<Vector3>();
    private bool _isActive;
    public Vector3 positionReturn;
    ChunkData chunkData;
    #endregion Variables

    //Method for setting coordinate
    public Chunk (ChunkCoordinate _coord) {
        coordinate = _coord;
    }

    //This function initialises the chunk
    public void Initialisation() {
        chunkObject = new GameObject();
        //Add and set up mesh renderer, filter and correct material
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        //Assign the materials from world
        materials[0] = World.Instance.material; 
        materials[1] = World.Instance.transparentMaterial;
        meshRenderer.materials = materials;
        //Parent this chunk to 
        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coordinate.x * VoxelData.chunkWidth, 0f, coordinate.z * VoxelData.chunkWidth);
        chunkObject.name = "Chunk" + coordinate.x + ", " + coordinate.z;
        positionReturn = chunkObject.transform.position;
            
        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)positionReturn.x, (int)positionReturn.z), true);
        lock (World.Instance.ChunkUpdateThreadLock) {
            World.Instance.chunksToUpdate.Add(this);            
        }
    }

    //This function goes through updating blocks in the chunk
    public void UpdateChunk() {
        //Clear mesh data
        ClearMeshData();
        //Loop through the x y and z axis updating mesh data
        for (int y = 0; y < VoxelData.chunkHeight; y++) {
            for (int x = 0; x < VoxelData.chunkWidth; x++) {
                for (int z = 0; z < VoxelData.chunkWidth; z++) {

                    if (World.Instance.blockTypes[chunkData.map[x,y,z].id].isSolid) {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }
        //
        World.Instance.chunksToDraw.Enqueue(this);
        
    }

    //This function clears the mesh data for the chunk
    void ClearMeshData() {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        normals.Clear();
    }

    //This method returns if this chunk is active and if not sets it active
    public bool IsActive {
        //Getter
        get { 
            return _isActive; 
        }
        //Setter
        set {
            _isActive = value;
            if (chunkObject != null) {
                chunkObject.SetActive(value);
            }
        }
    }

    //This update reutrns if the voxel is in a chunk or not
    bool IsVoxelInChunk(int x,int y, int z) {
        if (x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1) {
            return false;
        }
        else {
            return true;
        }
    }

    //Function to update the voxel cune
    public void EditVoxel (Vector3 position, byte newID) {
        int xCheck = (int)position.x;
        int yCheck = (int)position.y;
        int zCheck = (int)position.z;
        xCheck -= (int)(chunkObject.transform.position.x);
        zCheck -= (int)(chunkObject.transform.position.z);
        //Assign the new id for the voxel cube
        chunkData.map[xCheck, yCheck, zCheck].id = newID;
        World.Instance.worldData.AddToModifiedChunkList(chunkData);
        //Threaded lock for updating of surrounding voxels
        lock (World.Instance.ChunkUpdateThreadLock) {
            World.Instance.chunksToUpdate.Insert(0,this);
            UpdateSurroundingVoxels(xCheck,yCheck,zCheck);
        }
    }

    //This function updates the surrounding voxels
    void UpdateSurroundingVoxels(int x, int y, int z) {
        Vector3 thisVoxel = new Vector3(x, y, z);
        //Loop through each face of the voxel cube checking it's neighbouring voxels
        for (int p = 0; p < 6; p++) {

            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];
            if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z)) {
                World.Instance.chunksToUpdate.Insert(0, World.Instance.GetChunkFromVector3(currentVoxel + positionReturn));
            }
        }
    }

    //This voxel state function checks for a voxel at paramater position
    VoxelState CheckVoxel(Vector3 position) {
        int x = (int)position.x;
        int y = (int)position.y;
        int z = (int)position.z;

        if (!IsVoxelInChunk(x,y,z)) {
            return World.Instance.GetVoxelState(position + positionReturn);
        }
        return chunkData.map[x, y, z];
    }

    //This function returns the voxel state from the global vector3 position given as a parameter
    public VoxelState GetVoxelFromGlobalVector3(Vector3 position) {
        int xCheck = (int)position.x;
        int yCheck = (int)position.y;
        int zCheck = (int)position.z;

        xCheck -= (int)position.x;
        zCheck -= (int)position.z;
        //Return chunkData 
        return chunkData.map[xCheck, yCheck, zCheck];
    }

    //This function updates the data of the voxel mesh
    void UpdateMeshData(Vector3 position) {
        //Set the block id 
        byte blockId = chunkData.map[(int)position.x, (int)position.y, (int)position.z].id;
        //Look through each face (6 faces on cube)
        for (int p = 0; p < 6; p++) {
            VoxelState neighbour = CheckVoxel(position + VoxelData.faceChecks[p]);
            //If the neighbour is not null and the block is block neighbour is transparent
            if (neighbour != null && World.Instance.blockTypes[neighbour.id].isTransparent) {
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);
                //Loop through checking the faces
                for (int i = 0; i < 4; i++) {
                    normals.Add(VoxelData.faceChecks[p]);
                }
                //Add the texture to this block
                AddTexture(World.Instance.blockTypes[blockId].GetTextureId(p));
                
                //If the block is not transparent then add the vertex index values to transparent triangles
                if (!World.Instance.blockTypes[blockId].isTransparent) {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);

                }
                //Else add the vertex index values to transparant triangles
                else {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }
                vertexIndex += 4;
            }
        }
    }

    //This function creates a mesh for a chunk either when being generated or when being updated because of a block change on that
    public void CreateMesh() {
        //Create new mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        //Submesh count is set to zero
        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        //Set new mesh
        meshFilter.mesh = mesh;
        //If a mesh collider not alread on the blocks mesh then add one and the correct physics material onto it too
        if (chunkObject.GetComponent<MeshCollider>() == null) {
            chunkObject.AddComponent<MeshCollider>();
            chunkObject.GetComponent<MeshCollider>().material = World.Instance.physicsMaterial;
        }
        //Else set the material again to update 
        else {
            chunkObject.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
        }
    }

    //This function sets the correct texture from the texture atlas to the uvs
    void AddTexture(int textureId) {
        float y = textureId / VoxelData.textureAtlasSizeInBlocks;
        float x = textureId - (y * VoxelData.textureAtlasSizeInBlocks);
        //Normalise values
        y *= VoxelData.NormalizedBlockTextureSize;
        x *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }
}

//This class contains the chunk coordinate values
public class ChunkCoordinate {
    //Variables
    public int x;
    public int z;

    //Set x and z values to zero
    public ChunkCoordinate() {
        x = 0;
        z = 0;
    }

    //This method assigns the x and z variables of this class the x and z values passed in as parameters
    public ChunkCoordinate(int _x, int _z) {
        x = _x;
        z = _z;
    }

    //This method takes in a vector3 for the position and assigns the x and z to the x and z variables of this class
    public ChunkCoordinate(Vector3 position) {
        int xCheck = (int)position.x;
        int zCheck = (int)position.z;

        x = xCheck / VoxelData.chunkWidth;
        z = zCheck / VoxelData.chunkWidth;
    }

    //This function finds out if the chunk coordinate parameter x and z axis is equal to the chunk coordinate class x and z axis
    public bool Equals(ChunkCoordinate other) {
        //Return false if other is null
        if (other == null) {
            return false;
        }
        //Else if other x and z is equal to x and z of this chunkcoordinate then return true 
        else if (other.x == x && other.z == z) {
            return true;
        }
        //Else return false
        else {
            return false;
        }
    }
}

//This class is for the state of voxels
[System.Serializable]
public class VoxelState {
    //Id for the block type
    public byte id;
    
    public VoxelState() {
        id = 0;
    }
    //Function to set id
    public VoxelState(byte _id) {
        id = _id;
    }

}