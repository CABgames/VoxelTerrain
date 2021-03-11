/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///World.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz.
///The world class is responsible for initialising and managing the procedurally generated voxel world, this includes settings.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public class World : MonoBehaviour
{
    //Variables
    #region variables
    [Header("World Generation Values")]
    //Seed used for random
    public int seed;
    //Get a reference to biom attribute to be utilised
    public BiomeAttribute biome;
    //Enum for the type of noise, can be selected in the inspector by the user
    public enum noiseType {
        PERLIN,
        LAYEREDPERLIN,
        DIAMONDSQUARE,
        LAYEREDPERLINEANDDIAMONDSQUARE
    };
    public noiseType typeOfNoise;
    [Header("Performance")]
    [SerializeField]
    private bool use3DPerlinInGeneration;
    [SerializeField]
    private bool enableThreading;
    public bool noLoadingOrSaving;
    [SerializeField]
    private bool noVegetation;

    //View distance in chunks
    [SerializeField]
    private static readonly int viewDistanceInChunks = 4;
    [SerializeField]
    //Load distance in chunks
    private int loadDistanceInChunks = 8;   
    [SerializeField]
    private float offsetValueMinimum = 0.01f;
    [SerializeField]
    private float offsetValueMaximum = 0.1f;
    //Amount of layers for layered perlin noise 
    [SerializeField]
    private int layers = 3;

    //Player transform
    public Transform player;
    //Spawning position
    public Vector3 spawnPosition;
    //Material for non transparent textures material
    public Material material;
    //Material for transparent textures material
    public Material transparentMaterial;
    //Physics material (this stops player sticking to block and vegetation walls)
    public PhysicMaterial physicsMaterial;
    //Text for f3 debug screen
    public GameObject debugText;
    //Array of blocktypes, set up in the inspector
    public BlockType[] blockTypes;
    //Multidimensional array for chunks on x and z axis (y is not done)
    Chunk[,] chunks = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];
    //List of currently active chunks (chunks around the player)
    List<ChunkCoordinate> activeChunks = new List<ChunkCoordinate>();
    //Current chunk coordinate that the player is in
    public ChunkCoordinate playerChunkCoordinate;
    //The last chunk coordinate that the player was in
    ChunkCoordinate playerLastChunkCoordinate;
    //A list of the chunks needing to be created/generated
    List<ChunkCoordinate> chunksToCreate = new List<ChunkCoordinate>();
    //A list of the chunks needing to be updated 
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    //A list of the chunks needing to be drawn
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    //This boolean simply stops multithreading being called whilst already being done
    private bool applyingModifications = false;
    //A que of voxel modifications
    Queue <Queue<VoxelModification>> modifications = new Queue<Queue<VoxelModification>>();
    //Thread for chunk updating
    Thread chunkUpdateThread;
    public object ChunkUpdateThreadLock = new object ();
    public object ChunkListThreadLock = new object();
    //App path is used for the loading and saving 
    public string appPath;
    private PlayerCamera playerCamera;
    //Reference for the world instance of this class
    private static World _instance;
    //Array of floats for all the diamond square positions to be filled into
    private float[] diamondSquarepositions;

    //Method for getting instance of this world class
    public static World Instance {
        get {
            return _instance;
        }
    }
    //Reference for world data (loading/saving related)
    public WorldData worldData;
    #endregion variables

    //This awake function will get the diamond square array filled before when it is needed later when needed additionally will set up correct instance for world and assign application path
    private void Awake() {

        //If diamond square at all being used in noise generation then fill the multidimensional array and convert to one dimensional array
        if (typeOfNoise == noiseType.DIAMONDSQUARE || typeOfNoise == noiseType.LAYEREDPERLINEANDDIAMONDSQUARE) {

            //Assigning a two dimensional array for the values from the get diamond square algorithms function
            float[,] diamondSquare = Noise.GetDiamondSquareAlgorithm(16 * VoxelData.worldSizeInChunks, 1.5f, seed);
            int index = 0;
            //Getting the no of rows of 2d array 
            int numberOfRows = diamondSquare.GetLength(0);
            //Getting the no of columns of the 2d array
            int numberOfColumns = diamondSquare.GetLength(1);
            //Creating 1d Array by multiplying NoOfRows and NoOfColumns
            diamondSquarepositions = new float[numberOfRows * numberOfColumns];
            
            //Assigning the elements to the array from the multidimensional array
            //Looping through the y axis (in practice this information will be for the z axis)
            for (int y = 0; y < numberOfColumns; y++) {
                //Loop through x axis
                for (int x = 0; x < numberOfRows ; x++) {
                    diamondSquarepositions[index] = diamondSquare[x, y];
                    index++;
                }
            }
        }
        //If the instance is already assigned and not as this instance destroy this
        if (_instance != null && _instance != this) {
            Destroy(this.gameObject);
        }
        //Else assign this as the instance
        else {
            _instance = this;
        }

        //Persistant path for saving/loading, accessed here before threading begins 
        appPath = Application.persistentDataPath;
    }

    //Start function where the
    private void Start() {
        //World! is the current name of a world save 
        worldData = SaveSystem.LoadWorld("World!");
        //The random which will be utilised is set base upon the seed value passed into it
        //This seed int being the same is what allow replacable procedural voxel terrain generation
        UnityEngine.Random.InitState(seed);
        //Player camera is assigned
        playerCamera = player.GetComponent<PlayerCamera>();
        //Call the load world function
        LoadWorld();
        //Assign the spawning position of the player manually
        spawnPosition = new Vector3((VoxelData.worldSizeInChunks * VoxelData.chunkWidth) * 0.5f, VoxelData.chunkHeight + 180, (VoxelData.worldSizeInChunks * VoxelData.chunkWidth) * 0.5f);
        GenerateWorld();
        playerLastChunkCoordinate = GetChunkCoordFromVector3(player.position);

        //If the debug text isn't null then set it to false initially 
        if (debugText != null) {
            debugText.SetActive(false);
        }
        //Output that the text if missing otherwise
        else {
            Debug.LogWarning("Missing debug text gameobject!");
        }

        //If enable multithreading then the chunkUpdateThread is assigned and will begin
        if (enableThreading) {
            chunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            chunkUpdateThread.Start();
        }
    }

    //On disable stop the multithreading if it is enabled
    private void OnDisable() {
        if (enableThreading) {
            chunkUpdateThread.Abort();
        }
    }

    //Update function for the prcoedural voxel world
    private void Update() {
        //Playerchunk coordinate is set to the chunk coordinate vector for the players current position
        playerChunkCoordinate = GetChunkCoordFromVector3(player.position);
        //If player  coordinate does not equal player coordinate
        if (!playerChunkCoordinate.Equals(playerLastChunkCoordinate)) {
            CheckViewDistance();    
        }

        //If chunks to create is greater than zero call the create chunk method
        if (chunksToCreate.Count > 0) {
            CreateChunk();
        }

        //If chunks to draw is greater than zero create mesh
        if (chunksToDraw.Count > 0) {
            chunksToDraw.Dequeue().CreateMesh();
        }

        //If threading not enabled this will be done on main thread
        if(!enableThreading) {

            if (!applyingModifications) {
                ApplyModifications();
            }

            if (chunksToUpdate.Count > 0)
            {
                UpdateChunks();
            }
        }

        //If press f3 open up settings/debug 
        if (Input.GetKeyDown(KeyCode.F3)) {
            debugText.SetActive(!debugText.activeSelf);
        }

        //If press escape exit game/application
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit();
        }

        //If press f4 save game
        if (Input.GetKeyDown(KeyCode.F4) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Plus)) {
            SaveSystem.SaveWorld(worldData);
        }

        //If press f5 delete save game
        if (Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus)) {
            SaveSystem.ClearWorld(worldData);
        }
    }

    //This function returns a chunk coordinate based on the position given as a parameter
    ChunkCoordinate GetChunkCoordFromVector3 (Vector3 position) {
        int x = (int)(position.x / VoxelData.chunkWidth);
        int z = (int)(position.z / VoxelData.chunkWidth);
        return new ChunkCoordinate(x, z);
    }

    //This function returns a chunk based on the position given as a parameter
    public Chunk GetChunkFromVector3 (Vector3 position) {
        int x = (int)(position.x / VoxelData.chunkWidth);
        int z = (int)(position.z / VoxelData.chunkWidth);
        return chunks[x, z];
    }

    //This function ensures only chunks within the view distance render and others are set inactive (primitive chunk culling)
    void CheckViewDistance() {
        ChunkCoordinate coordinate = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoordinate = playerChunkCoordinate;
        List<ChunkCoordinate> previouslyActiveChunks = new List<ChunkCoordinate>(activeChunks);
        activeChunks.Clear();

        //Loop through coordinated within the view distance
        for (int x = coordinate.x - viewDistanceInChunks; x < coordinate.x + viewDistanceInChunks;x++) {
            for (int z = coordinate.z - viewDistanceInChunks; z < coordinate.z + viewDistanceInChunks; z++) {
                ChunkCoordinate thisChunkCoordinate = new ChunkCoordinate(x, z);

                if (IsChunkInWorld (thisChunkCoordinate)) {
                    //If this chunks is null create a new chunk there and add it to chunks to create
                    if (chunks[x,z] == null) {
                        chunks[x, z] = new Chunk(thisChunkCoordinate);
                        chunksToCreate.Add(thisChunkCoordinate);
                    }
                    //Else is the chunk is not active then set it active
                    else if (!chunks[x,z].IsActive) {
                        chunks[x, z].IsActive = true;
                    }
                        //Add chunk to to active chunks
                        activeChunks.Add(thisChunkCoordinate);
                }
                //Loop through previously active chunks and is one is thischunkcoordinate then remove it
                for (int i = 0;i < previouslyActiveChunks.Count;i++) {
                    if(previouslyActiveChunks[i].Equals(thisChunkCoordinate)) {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }
        //Foreach previously active chunk set it false
        foreach (ChunkCoordinate c in previouslyActiveChunks) {
            chunks[c.x, c.z].IsActive = false;
        }
    }

    //Called to load world
    private void LoadWorld() {
        //Loops through loading chunks
        for (int x = (int)(VoxelData.worldSizeInChunks * 0.5f) - loadDistanceInChunks; x < (int)(VoxelData.worldSizeInChunks * 0.5f) + loadDistanceInChunks; x++) {

            for (int z = (int)(VoxelData.worldSizeInChunks * 0.5f) - loadDistanceInChunks; z < (int)(VoxelData.worldSizeInChunks * 0.5f) + loadDistanceInChunks; z++) {
                worldData.LoadChunk(new Vector2Int(x, z));
            }
        }
    }

    //Called once to generate world
    private void GenerateWorld() {
        //Loops through nearby chunks
        for (int x = (int)(VoxelData.worldSizeInChunks * 0.5f) - viewDistanceInChunks;x < (int)(VoxelData.worldSizeInChunks * 0.5f) + viewDistanceInChunks; x++) {

            for (int z = (int)(VoxelData.worldSizeInChunks * 0.5f) - viewDistanceInChunks; z < (int)(VoxelData.worldSizeInChunks * 0.5f) + viewDistanceInChunks; z++) {
                //Chunks added to chunks to create
                ChunkCoordinate newChunk = new ChunkCoordinate(x, z);
                chunks[x, z] = new Chunk(newChunk);
                chunksToCreate.Add(newChunk);
            }
        }
        //Set player position to spawn position
        player.position = spawnPosition;
        CheckViewDistance();
    }

    //This method returns if the voxel is solid at given position
    public bool CheckForVoxel(Vector3 position) {
        byte voxel = GetVoxel(position);
        //If block is solid return true and otherwise return false
        if (blockTypes[voxel].isSolid) {
            return true;
        }
        else {
            return false;
        }
    }

    //This method checks if the given positions voxel is transparant
    public bool CheckIfVoxelTransparent(Vector3 position) {
        ChunkCoordinate thisChunk = new ChunkCoordinate(position);

        if (!IsChunkInWorld(thisChunk) || position.y < 0 || position.y > VoxelData.chunkHeight) {
            return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null) {
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(position).id].isTransparent;
        }
        //Return if voxel is transparant or not
        return blockTypes[GetVoxel(position)].isTransparent;
    }

    //This methos returns the voxel state for a given position
    public VoxelState GetVoxelState(Vector3 position) {
        return worldData.GetVoxel(position);
    }

    //Responsible for world generation
    public byte GetVoxel(Vector3 position) {
        //Set y position to the y axis of position parameter
        int yPosition = (int)position.y;
        
        //Immutable pass
        //If outside world then return air
        if (!IsVoxelInWorld(position)) {
            return 0;
        }

        //If at bottom return bedrock
        if (yPosition == 0) {
            return 1;
        }

        //Integers for the terrain height and number of digits to constrain diamond square values to
        int terrainHeight;
        int numberOfDigits;
        //First pass (overworld)
        switch (typeOfNoise) {
            //2D perlin noise
            case noiseType.PERLIN:
                terrainHeight = (int)(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.terrainScale)) + biome.solidGroundHeight;
                break;

            //Layered 2D perlin noise
            case noiseType.LAYEREDPERLIN:
                //25 is deducted from the terrain height when using layered perlin to offset height intensity and ensure more of vegetation can spawn at it's heights
                terrainHeight = (int)(biome.terrainHeight * Noise.GetLayered2DPerlin(new Vector2(position.x, position.z), offsetValueMinimum, offsetValueMaximum, biome.terrainScale, layers)) + (biome.solidGroundHeight - 25);
                break;
            
            //Diamond square algorithm
            case noiseType.DIAMONDSQUARE:
                //Heifgr is assigned as correct value from the diamond square positions array
                terrainHeight = (int)diamondSquarepositions[((int)position.x * (int)position.z)];
                //This is reduced till the number of digits is two
                numberOfDigits = (int)Math.Floor(Math.Log10(terrainHeight) + 1);
                // check if input number has more digits than the required get first N digits
                if (numberOfDigits >= 2) {
                    terrainHeight = (int)Math.Truncate((terrainHeight / Math.Pow(10, numberOfDigits - 2)));
                }
                break;
            
            //Layered perlin noise combine with diamond square algorithm
            case noiseType.LAYEREDPERLINEANDDIAMONDSQUARE:
                terrainHeight = (int)(biome.terrainHeight * Noise.GetLayered2DPerlin(new Vector2(position.x, position.z), offsetValueMinimum, offsetValueMaximum, biome.terrainScale, layers)) + (biome.solidGroundHeight - 25);
                numberOfDigits = (int)Math.Floor(Math.Log10(terrainHeight) + 1);
                // check if input number has more digits than the required get first N digits
                if (numberOfDigits >= 2) {
                    numberOfDigits = (int)Math.Truncate((terrainHeight / Math.Pow(10, numberOfDigits - 2)));
                }
                //Values for both algoriths added together and then halfed
                terrainHeight = ((numberOfDigits + terrainHeight) / 2);
                break;

            //If another value set terrain to 100 simply
            default:
                terrainHeight = 100;
                break;
        }

        //Air
        if (yPosition > terrainHeight) {
            return 0;
        }

        //Second pass (underground)
        byte voxelValue = 0;
        
        //If 3D perlin generation is true do advanced generation for sub surface terrain otherwise do simplified generation for this terrain
        if (!use3DPerlinInGeneration) {
            voxelValue = SimplifiedGeneration(terrainHeight,yPosition);
        }
        else {
            voxelValue = AdvancedGeneration(terrainHeight, yPosition, position);
        }

        //Third pass (vegatation)
        if (yPosition == terrainHeight && !noVegetation ) {
            if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.vegetationZoneScale) > biome.vegetationZoneThreshold) {
                switch (terrainHeight) {
                    //Spruce trees and snow blocks generate at this height range
                    case int n when (n > 50 && n <= 54):
                        voxelValue = 6;
                        if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.vegetationPlacementScale) > biome.vegetationPlacementThreshold) {
                            modifications.Enqueue(Structure.MakeTree(position, biome.minimumVegetationHeight, biome.maximumVegetationHeight,34,33));
                        }
                        break;

                    //Redwood tree and yellow grass blocks can generate at this range of height
                    case int n when (n > 54 && n <= 60):
                        voxelValue = 41;
                        if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.vegetationPlacementScale) > biome.vegetationPlacementThreshold) {
                            modifications.Enqueue(Structure.MakeTree(position, biome.minimumVegetationHeight, biome.maximumVegetationHeight, 36, 35));
                        }
                        break;

                    //Cactus and sand blocks can generate at this height range
                    case int n when (n > 60 && n <= 66):
                        voxelValue = 5;
                        if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.vegetationPlacementScale) > biome.vegetationPlacementThreshold) {
                            modifications.Enqueue(Structure.MakeCactus(position));
                        }
                        break;

                    //Bush and clay can generate above this height
                    case int n when (n > 66):
                        voxelValue = 28;
                        if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.vegetationPlacementScale) > biome.vegetationPlacementThreshold) {
                            modifications.Enqueue(Structure.MakeBush(position));
                        }
                        break;
                    //If incorrect value given or oak tree value ignore
                    default:
                        if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.vegetationPlacementScale) > biome.vegetationPlacementThreshold) {
                            modifications.Enqueue(Structure.MakeTree(position, biome.minimumVegetationHeight, biome.maximumVegetationHeight, 32, 31));
                        }
                        break;
                }
            }
        }

        //Return the voxel value for the type of block which is in place where the parameter position is checking
        return voxelValue;
    }

    //Create a chunk at at given coordinate positions
    void CreateChunk() {
        ChunkCoordinate c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[c.x, c.z].Initialisation();
    }

    //Update chunks function for updating the chunks without doing so on the main thread
    void UpdateChunks() {      
        //Thread lock for this function
        lock (ChunkUpdateThreadLock) {
            //Update through chunks
            chunksToUpdate[0].UpdateChunk();

            if (!activeChunks.Contains(chunksToUpdate[0].coordinate)) {
                activeChunks.Add(chunksToUpdate[0].coordinate);
            }
            chunksToUpdate.RemoveAt(0);
        }
    }

    //This method loops through while being called applying modifications and updating chunks without it being done on the main thread
    void ThreadedUpdate() {

        while (true) {
            //If not applying modifications call the apply modifications function
            if (!applyingModifications) {
                ApplyModifications();
            }
            //If the chunks to update is greater than zero then call the update chunks functions
            if (chunksToUpdate.Count > 0) {
                UpdateChunks();
            }
        }
    }

    //This method goes through applying modifications in the queue
    void ApplyModifications() {
        //Set to true so to stop the method being called until complete
        applyingModifications = true;
        //Looping through the modifications whilst more than 0
        while (modifications.Count > 0) {
            Queue<VoxelModification> queue = modifications.Dequeue();

            while (queue.Count > 0) {

                VoxelModification v = queue.Dequeue();
                worldData.SetVoxel(v.position, v.id);
            }

        }
        //Set to false as modifications all applied
        applyingModifications = false;
    }

    //Simplified generation generates basic height based subsurface blocks but doesn't use 3D perlin noise (more efficient)
    private byte SimplifiedGeneration(int terrainHeight, int yPosition) {

        //Top layer
        if (yPosition == terrainHeight) {
            return 3;
        }
        //Lower layer
        else if (yPosition < terrainHeight && yPosition > terrainHeight - 4) {
            return 4;
        }
        //Deep layer
        else if (yPosition > biome.deepStoneHeight) {
            return 2;
        }
        //Deeper layer
        else if (yPosition > biome.deepestStoneHeight) {
            return 8;
        }
        //Deepest layer
        else if (yPosition > 0)
        {
            return 9;
        }
        //If none then return 0
        return 0;
    }

    //This advanced generation method utilises 3D noise for generating subsurface blocks 
    private byte AdvancedGeneration(int terrainHeight, int yPosition, Vector3 position) {
        byte voxelValue = 0;

        //Top layer
        if (yPosition == terrainHeight) {
            return 3;
        }
        //Lower layer
        else if (yPosition < terrainHeight && yPosition > terrainHeight - 4) {
            return 4;
        }
        //Deeper layers
        else {
            //Looping through the lodes for the different blocks which can be generated unde the terrain surface 
            foreach(Lode lode in biome.lodes) {

                if (Noise.Get3DPerlin(position,lode.noiseOffset,lode.scale,lode.threshold)) {
                    switch (lode.layer) {
                        //Any layer
                        case Lode.rockLayer.ANY:
                            return voxelValue = lode.blockId;

                        //Deep layer
                        case Lode.rockLayer.ROCK:
                            if (yPosition > biome.deepStoneHeight && yPosition <= terrainHeight - 4) {
                                return voxelValue = lode.blockId;
                            }
                            else {
                                return 2;
                            }

                        //Deeper layer
                        case Lode.rockLayer.LOWERROCK:
                            if (yPosition > biome.deepestStoneHeight && yPosition <= biome.deepStoneHeight) {
                                return voxelValue = lode.blockId;
                            }
                            else {
                                return 8;
                            }

                        //Deepest layer
                        case Lode.rockLayer.LOWESTROCK:
                            if (yPosition > 0 && yPosition <= biome.deepestStoneHeight) {
                                return voxelValue = lode.blockId;
                            }
                            else {
                                return 9;
                            }
                    }
                }
            }
                return 0;
        }
    }

    //This function is responsible for returning if the chunk given as parameter is within the world 
    bool IsChunkInWorld (ChunkCoordinate coordinate) {
        if (coordinate.x > 0 && coordinate.x < VoxelData.worldSizeInChunks - 1 && coordinate.z > 0 && coordinate.z < VoxelData.worldSizeInChunks - 1) {
            return true;
        }
        else {
            return false;
        }
    }

    //This function returns if the parameter position is within the world size or not
    bool IsVoxelInWorld(Vector3 position) {
        if (position.x >= 0 && position.x < VoxelData.WorldSizeInVoxels && position.y >= 0 && position.y < VoxelData.chunkHeight && position.z >= 0 && position.z < VoxelData.WorldSizeInVoxels) {
            return true;
        }
        else {
            return false;
        }
    }
}

//The block type class contains the various aspects and information for each block type and is used for the array of block types in the world class above
[System.Serializable]
public class BlockType {
    //Variables
    #region Variables
    public string blockName;
    public bool isSolid;
    public bool isTransparent;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;
    #endregion Variables

    //This function gets the texture id for the face passed in as a paramter
    public int GetTextureId(int faceIndex) {
        //Back,Front,Top,Bottom,Left,Right
        switch (faceIndex) {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in getting textureId, faceIndex invalid!");
                return 0;
        }
    }

}
//Voxel modification class used for the voxels and allows access to block information such as the id (block type) and position 
public class VoxelModification {
    //Variables
    public Vector3 position;
    public byte id;

    public VoxelModification() {
        position = new Vector3();
        id = 0;
    }

    //This function allows the setting of position and voxel id/block type based upon parameter values
    public VoxelModification (Vector3 _position, byte _id) {
        position = _position;
        id = _id;
    }
}