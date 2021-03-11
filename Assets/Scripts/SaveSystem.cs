/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///SaveSytem.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz.
///This class is responsible for saving and loading the procedurally generated voxel world which is done per chunk.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

public class SaveSystem {

    //This function deletes the world save if a save currently exists
    public static void ClearWorld(WorldData world) {
        string savePath = World.Instance.appPath + "/saves/" + world.worldName + "/";
        //Check if directory exists and if so delete save
        if (Directory.Exists(savePath))
        {
            Debug.Log("Deleting" + world.worldName);
            Directory.Delete(savePath,true);
        }
    }

    //This function saves the world
    public static void SaveWorld (WorldData world) {
        //Don't save
        if (World.Instance.noLoadingOrSaving) {
            Debug.Log("Nothing saved");
        }
        //Save the world
        else {
            //Set the saving location and ensure a save folder exists
            string savePath = World.Instance.appPath + "/saves/" + world.worldName + "/";
            //Check if directory does not already exist and create directory if not
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            Debug.Log("Saving" + world.worldName);

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(savePath + "world.world", FileMode.Create);

            formatter.Serialize(stream, world);
            stream.Close();
            //Done on none main thread
            Thread thread = new Thread(() => SaveChunks(world));
            thread.Start();
        }
    }
    //This function saves all the chunks
    public static void SaveChunks(WorldData world) {
        List<ChunkData> chunks = new List<ChunkData>(world.modifiedChunks);
        world.modifiedChunks.Clear();
        int count = 0;
        //Loops through all the chunks saving them
        foreach (ChunkData chunk in chunks) {
            SaveSystem.SaveChunk(chunk, world.worldName);
            count++;
        }

        Debug.Log(count + " chunks saved.");
    }

    //This function loads the world in
    public static WorldData LoadWorld(string worldName,int seed = 0) {
        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/";
        //If world already exists and no loading or saving is false load the world in
        if (File.Exists(loadPath + "world.world") && !World.Instance.noLoadingOrSaving) {
            Debug.Log(worldName + " found. Loading from save.");

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + "world.world", FileMode.Open);
            WorldData world = formatter.Deserialize(stream) as WorldData;
            stream.Close();
            return new WorldData(world);
        }
        //Else create new world and save it
        else {
            Debug.Log(worldName + " not found. Generating new world.");

            WorldData world = new WorldData(worldName, seed);
            SaveWorld(world);

            return world;
        }
    }

    //This function saves a chunk passed into it
    public static void SaveChunk(ChunkData chunk,string worldName) {
        string chunkName = chunk.position.x + "_" + chunk.position.y;

        //Set the saving location and ensure a save folder exists
        string savePath = World.Instance.appPath + "/saves/" + worldName + "/chunks/";
        //If directory does not exist create directory path
        if (!Directory.Exists(savePath)) {
            Directory.CreateDirectory(savePath);
        }

        BinaryFormatter formatter = new BinaryFormatter();
        //Set the file stream to a new filestream at the save path
        FileStream stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create);
        //Serialize formatter
        formatter.Serialize(stream, chunk);
        stream.Close();
    }

    //Function to load chunks
    public static ChunkData LoadChunk(string worldName, Vector2Int position) {
        string chunkName = position.x + "_" + position.y;

        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/chunks/" + chunkName + ".chunk";
        //If the file exists at the directory stream the information in for the chunk being loaded in
        if (File.Exists(loadPath)) {

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath, FileMode.Open);
            ChunkData chunkData = formatter.Deserialize(stream) as ChunkData;
            stream.Close();
            return chunkData;
        }
        return null;
    }
}
