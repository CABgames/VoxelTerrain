/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///Structure.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz.
///This class contains the information for reading in the correct order vertices, triangles and faces for.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure {

    //This function places a tree when called on the surface terrain, the type of wood and leaves is passed in as paramaters along with the height minimum and maximum of the trees 
    public static Queue<VoxelModification> MakeTree (Vector3 position, int minimumTrunkHeight, int maximumTrunkHeight,byte woodType,byte leafType) {
        Queue<VoxelModification> queue = new Queue<VoxelModification>();
        int height = (int)(maximumTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 250f, 3f));

        //If the height is less than the minimum height then assign it that value 
        if (height < minimumTrunkHeight) {
            height = minimumTrunkHeight;
        }

        //Add a wooden block until the height is met
        for (int i = 1; i < height;i++) {
            queue.Enqueue(new VoxelModification(new Vector3(position.x, position.y + i, position.z), woodType));
        }

        //Loop through creating the leaves
        for (int x = -3; x < 4; x++) {
            for (int y = 0; y < 7; y++) {
                for (int z = -3; z < 4; z++) {
                    queue.Enqueue(new VoxelModification(new Vector3(position.x + x, position.y + height + y, position.z + z), leafType));
                }
            }
        }
        return queue;
    }

    //This function places a cactus when called on the terrains surface
    public static Queue<VoxelModification> MakeCactus(Vector3 position) {
        Queue<VoxelModification> queue = new Queue<VoxelModification>();

        for (int i = 1; i < 4; i++) {
            queue.Enqueue(new VoxelModification(new Vector3(position.x, position.y + i, position.z), 38));
        }

        return queue;
    }

    //This function simply places a bush block above given position
    public static Queue<VoxelModification> MakeBush(Vector3 position) {
        Queue<VoxelModification> queue = new Queue<VoxelModification>();
        queue.Enqueue(new VoxelModification(new Vector3(position.x, position.y + 1, position.z), 39));

        return queue;
    }
}
