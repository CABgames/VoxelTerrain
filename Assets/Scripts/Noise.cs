/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///Noise.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz, also includes 
///a method based upon an example by carlpilot for 3D noise, and a diamondsquare algorithm method based upon one by awiki01
///This class contains four functions each used in some form to generate procedural voxel terrain in some form.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise {

    //This function gets a 2D perlin noise value based upon paramters of a vector2 position, an offset and scale
    public static float Get2DPerlin(Vector2 position, float offset, float scale) {
        return Mathf.PerlinNoise((position.x + 0.1f) / VoxelData.chunkWidth * scale + offset, (position.y + 0.1f) / VoxelData.chunkWidth * scale + offset);
    }

    //This is a method I created similar to the 2D noise except looping through the amount of layers given as a parameter and randomly selecting a value between offset range to use for offset value
    public static float GetLayered2DPerlin(Vector2 position, float offsetMinimum, float offsetMaximum, float scale, int layers) {
        float perlin = 0;
        scale = (scale / layers);
        for (int i = 0; i < layers; i++)
        {
            perlin += Mathf.PerlinNoise((position.x + 0.1f) / VoxelData.chunkWidth * scale + Mathf.Lerp(offsetMaximum, offsetMaximum,scale) /*Random.Range(offsetMaximum, offsetMaximum)*/, (position.y + 0.1f) / VoxelData.chunkWidth * scale + scale + Mathf.Lerp(offsetMaximum, offsetMaximum, scale) /*Random.Range(offsetMaximum, offsetMaximum)*/);
        }

        return perlin;
    }

    //Credit to carlpilot on YouTube for the c# perlin noise code https://www.youtube.com/watch?v=Aga0TBJkchM
    //This method is used to get 3D perlin noise and is used for caves/blocks under the procedural voxel world terrain surface
    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold) {
        //Axis values assigned based on position plus offsets and multiplied by scale 
        float x = (position.x + offset + 0.1f) * scale;
        float y = (position.y + offset + 0.1f) * scale;
        float z = (position.z + offset + 0.1f) * scale;
        //Noise got for all 6 points
        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);
        //Returns true if greater than the given threshold, else returns false
        if ((AB + BC + AC + BA + CB + CA) / 6f > threshold) {
            return true;
        }
        else {
            return false;
        }
    }

    //Credit to awiki01 for the c# diamond square algorithm code  https://gist.github.com/awilki01/83b65ad852a0ab30192af07cda3d7c0b
    //This code example was based on an earlier example in Java on Stack Overflow by user M. Jessup
    // https://stackoverflow.com/questions/2755750/diamond-square-algorithm?newreg=ee2a40d2fe9f49b9b938151e933860d2
    //This diamond square algorithm method gets data for a two dimensional float array full of elements for height/terrain data 
    public static float[,] GetDiamondSquareAlgorithm(int terrainPoints, float roughness, float seed) {
        //Terrain points variable must always be a power of two
        int dataSize = terrainPoints + 1; 
        //The data is the multidimensional array which is returned from this method after being filled with the diamond square algorithm data needed
        float[,] data = new float[dataSize, dataSize];
        data[0, 0] = data[0, dataSize - 1] = data[dataSize - 1, 0] = data[dataSize - 1, dataSize - 1] = seed;

        //The side length needs to always be more or equal to two and each iteration the variation offset is reducted
        for (int sideLength = dataSize - 1; sideLength >= 2; sideLength /= 2, roughness /= 2.0f) {
            //Half side is equal to half the legth of one square
            int halfSide = sideLength / 2;

            //These for loops generate the new square value, whilst called x and z here in practice it is the x and z axis that this will be used for
            for (int x = 0; x < dataSize - 1; x += sideLength) {

                for (int y = 0; y < dataSize - 1; y += sideLength) {
                    //Average of existing corners is calculated going from top left to lower right
                    float average = data[x, y] + data[x + sideLength, y] + data[x, y + sideLength] +  data[x + sideLength, y + sideLength];//lower right
                    average /= 4.0f;
                    //The center is the average plus random offset
                    data[x + halfSide, y + halfSide] =
                    average + (Random.value * 2 * roughness) - roughness;
                }
            }
            //Generation of diamond values, because diamond staggered movement is only on x
            for (int x = 0; x < dataSize - 1; x += halfSide) {
                //The y axis is the x axis but offset by half a side and moved by the full side length 
                for (int y = (x + halfSide) % sideLength; y < dataSize - 1; y += sideLength) {
                    //The average is x and y centree
                    float average = data[(x - halfSide + dataSize) % dataSize, y] +  data[(x + halfSide) % dataSize, y] + data[x, (y + halfSide) % dataSize] +  data[x, (y - halfSide + dataSize) % dataSize]; 
                    //Then it is divided by 4
                    average /= 4.0f;
                    //The average is then assigned to the average value plus random multiplied by roughness and then subtracted
                    average = average + (Random.value * 2 * roughness) - roughness;
                    //update value for center of diamond
                    data[x, y] = average;

                    //Values are wrapped on edges
                    if (x == 0) {
                        data[dataSize - 1, y] = average;
                    }
                    if (y == 0) {
                        data[x, dataSize - 1] = average;
                    }
                }
            }
        }
        return data;
    }
}
