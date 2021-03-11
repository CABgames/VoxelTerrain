/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///PlayerCamera.cs
///Developed by Charlie Bullock
///This class is responsible for the player camera rotation along with block placing and removal.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerCamera : MonoBehaviour
{
    //Variables
    #region Variables
    [SerializeField]
    private float clampDegree = 70;
    //First person camera
    [SerializeField]
    public Camera firstPersonCamera;
    //Player movement
    private PlayerController pC;
    public Vector3 firstPersonCamPosition;
    public float otherCamSpeed = 2f;
    [SerializeField]
    private float mouseSensitivity;
    private float pitch;
    private float yaw;

    private World world;
    [SerializeField]
    private Transform selectedBlock;
    [SerializeField]
    private Transform placedBlock;
    [SerializeField]
    private TextMeshProUGUI selectedBlockText;
    public byte selectedBlockIndex;
    public float checkIncrement = 0.1f;
    public float reach = 8;
    #endregion Variables

    #region Enums
    //Enum for mouse input type
    private enum mI
    {
        INVERTX,
        INVERTY,
        INVERTBOTH,
        INVERTNONE
    }
    [SerializeField]
    private mI mouseInversion;
    #endregion Enums
    // Assigning audio listeners, setting correct camera state and making sure queriesHitBackfaces is true for raycasting later
    void Start() {
        world = FindObjectOfType<World>();
        Physics.queriesHitBackfaces = true;
        firstPersonCamPosition = firstPersonCamera.transform.position;
        pC = GetComponent<PlayerController>();
        selectedBlockText.text = world.blockTypes[selectedBlockIndex].blockName + " cube selected";
    }

    //Update function calls the camera type, place cursor blocks, remove or add block and selection block functions ensuring they constantly gone through
    void Update() {
        //Function for aspects of the player movement to if the camera is in third or first person mode
        CameraType();
        //This function checks if a voxel space or cube is within the given reach vicinity of the player and places selected and place blocks accordingly 
        PlaceCursorBlocks();
        //Function to allow player left and right click to add or remove blocks when applicable
        RemoveOrAddBlock();
        //A method allowing player to scroll using mouse wheen through the numerous block types and which they select can be placed down
        SelectionBlock();
    }

    //This method checks for player left and right click to add or remove blocks when applicable
    private void RemoveOrAddBlock() {
        if (selectedBlock.gameObject.activeSelf) {

            //Destroy block
            if (Input.GetMouseButtonDown(0) && selectedBlock != null) {
                world.GetChunkFromVector3(selectedBlock.position).EditVoxel(selectedBlock.position, 0);
            }

            //Place block
            if (Input.GetMouseButtonDown(1) && placedBlock != null) {
                world.GetChunkFromVector3(placedBlock.position).EditVoxel(placedBlock.position, selectedBlockIndex);
            }
        }
    }

    //A method for allowing player to scroll using mouse wheen through the numerous block types and which they select can be placed down
    private void SelectionBlock()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0) {

            //Increment up
            if (scroll > 0) {
                selectedBlockIndex++;
            }
            //Increment down
            else {
                selectedBlockIndex--;
            }

            if (selectedBlockIndex > (byte)(world.blockTypes.Length - 1)) {
                selectedBlockIndex = 1;
            }

            if (selectedBlockIndex < 1) {
                selectedBlockIndex = (byte)(world.blockTypes.Length -1);
            }
            //Set the text on the bottom of the screen to display the selected block name
            selectedBlockText.text = world.blockTypes[selectedBlockIndex].blockName + " block selected";
        }
    }

    //This method checks if a voxel space or cube is within the given reach vicinity of the player and places selected and place blocks accordingly 
    private void PlaceCursorBlocks() {
        float step = checkIncrement;
        Vector3 lastPosition = new Vector3();
        //While step is less than reach
        while (step < reach) {
            //Move forward from the position the player camera is looking at
            Vector3 position = firstPersonCamera.transform.position + (firstPersonCamera.transform.forward * step);

            //If there is a solid block at position this will be true and then set the selected block and placed blocks positions
            if (world.CheckForVoxel(position)) {
                selectedBlock.position = new Vector3((int)position.x, (int)position.y, (int)position.z);
                //Placed blocks position is the last position
                placedBlock.position = lastPosition;
                selectedBlock.gameObject.SetActive(true);
                placedBlock.gameObject.SetActive(true);
                return;
            }

            lastPosition = new Vector3((int)position.x, (int)position.y, (int)position.z);
            //Increment step by the amount of check increment
            step += checkIncrement;
        }
        //Otherwise set both block gameobjects inactive
        selectedBlock.gameObject.SetActive(false);
        placedBlock.gameObject.SetActive(false);
    }

    //Called to crouch the player
    public void Crouch() {
        firstPersonCamera.transform.localPosition = new UnityEngine.Vector3(0, 0, 0);
    }

    //Called to uncrouch the player
    public void UnCrouch() {
        firstPersonCamera.transform.localPosition = new UnityEngine.Vector3(0, 0.75f, 0);
    }

    //Camera type function which is responsible for managing the rotation and type of camera which the player utilises
    void CameraType() {

        if (pC.GetMovement() != 0) {

            //Unlocks cursor when escape pressed
            if (Input.GetKeyDown(KeyCode.Escape)) {
                pC.SetMovement(0);
                UnityEngine.Cursor.lockState = CursorLockMode.None;
            }
            //Else if the cursore is not locked it will be locked
            else if (UnityEngine.Cursor.lockState == CursorLockMode.None) {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            }

            //X & Y axis camera can be either inverted or not
            switch (mouseInversion) {
                //InvertX
                case mI.INVERTX:
                    yaw -= mouseSensitivity * Input.GetAxis("Mouse X");
                    pitch += mouseSensitivity * Input.GetAxis("Mouse Y");
                    pitch = Mathf.Clamp(pitch, -clampDegree, clampDegree);
                    break;

                //InvertY
                case mI.INVERTY:
                    yaw += mouseSensitivity * Input.GetAxis("Mouse X");
                    pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
                    pitch = Mathf.Clamp(pitch, -clampDegree, clampDegree);
                    break;

                //Both
                case mI.INVERTBOTH:
                    yaw -= mouseSensitivity * Input.GetAxis("Mouse X" );
                    pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
                    pitch = Mathf.Clamp(pitch, -clampDegree, clampDegree);
                    break;

                //None
                case mI.INVERTNONE:
                    yaw += mouseSensitivity * Input.GetAxis("Mouse X");
                    pitch += mouseSensitivity * Input.GetAxis("Mouse Y");
                    pitch = Mathf.Clamp(pitch, -clampDegree, clampDegree);
                    break;
            }
            //Vector3 currentRotation
            firstPersonCamera.transform.eulerAngles = new UnityEngine.Vector3(pitch, yaw, 0.0f);
        }
    }

}
