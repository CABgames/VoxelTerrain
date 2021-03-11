/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///DebugScreen.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz.
///This is debug screen class displays text of debug information.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugScreen : MonoBehaviour {
    //Variables
    private World world;
    [SerializeField]
    private TextMeshProUGUI text;
    private float frameRate;
    private float timer;
    private int halfWorldSizeInVoxels;
    private int halfWorldSizeInChunks;

    //Start function where world and text components assigned along with half world size
    void Start() {
        world = FindObjectOfType<World>();
        text = GetComponent<TextMeshProUGUI>();
        halfWorldSizeInVoxels = (int)(VoxelData.WorldSizeInVoxels * 0.5f);
        halfWorldSizeInVoxels = (int)(VoxelData.WorldSizeInVoxels * 0.5f);
    }

    //Update function in which the debug text and framerate is update
    void Update() {
        //String for debug text displays current chunks, framerate and help with keys for saving, etc
        string debugText = "Debug screen:";
        debugText += "\n";
        debugText += frameRate + "FPS ";
        debugText += "\n\n";
        debugText += "X Y Z " + ((int)world.player.transform.position.x - halfWorldSizeInVoxels) + " / " + (int)world.player.transform.position.y + " / " + ((int)world.player.transform.position.z - halfWorldSizeInVoxels);
        debugText += "\n";
        debugText += "Chunk " + (world.playerChunkCoordinate.x - halfWorldSizeInChunks) + " / " + (world.playerChunkCoordinate.z - halfWorldSizeInChunks);
        debugText += "\n";
        debugText += "Press F4 to save";
        debugText += "\n";
        debugText += "Press F5 to reset save";
        debugText += "\n";
        debugText += "Press Escape to exit";
        //Text mesh pro text assigned as debug text
        text.text = debugText;

        //Framerate
        if (timer > 1f) {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else {
            timer += Time.deltaTime;
        }

    }
}
