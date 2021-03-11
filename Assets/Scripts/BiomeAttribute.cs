/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///BiomeAttribute.cs
///Developed by Charlie Bullock based upon the "Code A Game Like Minecraft In Unity" YouTube series by b3agz.
///This class if the basic attibutes of a biome such as height levels, terrain scale and the lodes for the numerous blocks it uses.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttribute", menuName = "Biome Attribute")]
public class BiomeAttribute : ScriptableObject {
    //Variables
    #region Variables
    public string biomeName;
    public int solidGroundHeight;
    public int terrainHeight;
    public int deepStoneHeight;
    public int deepestStoneHeight;
    public float terrainScale;

    [Header("Vegetation")]
    public float vegetationZoneScale = 1.3f;
    [Range(0.1f,1f)]
    public float vegetationZoneThreshold = 0.6f;
    public float vegetationPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float vegetationPlacementThreshold = 0.8f;
    //Minimum and maximum vegetation height for structures such as trees
    public int maximumVegetationHeight = 12;
    public int minimumVegetationHeight = 5;

    public Lode[] lodes;
    #endregion Variables
}

[System.Serializable]
public class Lode {
    public enum rockLayer {
        ANY,
        ROCK,
        LOWERROCK,
        LOWESTROCK
    };
    //Variables
    public string nodeName;
    public rockLayer layer;
    //Block id for referencing blocktype array in world class
    public byte blockId;
    //Scale of lode
    public float scale;
    public float threshold;
    public float noiseOffset;
}