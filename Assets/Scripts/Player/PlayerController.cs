/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///PlayerController.cs
///Developed by Charlie Bullock
///This class primarily acts as the main script for controlling the player movement.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //Variables
    #region Variables
    [SerializeField]
    private float velocityClamp = 10f;
    [SerializeField]
    private float jumpVelocity = 50;
    [SerializeField]
    private float speedMultiplier = 0.5f;
    [SerializeField]
    private float walkSpeed = 5f;
    [SerializeField]
    private float runSpeed = 7f;
    [SerializeField]
    private float crouchSpeed = 1.5f;
    //Rotation position
    [SerializeField]
    private Transform rotationPosition;
    [SerializeField]
    private GameObject cameraTransform;
    private PlayerCamera pC;
    private Rigidbody rb;
    private float speed;
    private float downForce = 15;
    private bool grounded = false;
    private CapsuleCollider collider;
    private Vector3 velocityChange;
    private Vector3 velocity;
    #endregion
    //Enums
    #region Enums
    //Enum for player movement type
    private enum pM
    {
        INTERACTING,
        CROUCHING,
        WALKING,
    }
    //Enum variable
    private pM playerMovement;

    #endregion Enum
    //Start function sets up numerous aspects of the player ready for use
    void Start() {
        //Player set to interacting (not moving initialy)
        playerMovement = pM.INTERACTING;
        //Assigning references
        pC = GetComponent<PlayerCamera>();
        rb = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();
        //Rotation is freezed on rigidbody for the capsule collider
        rb.freezeRotation = true;
        //Gravity is applied artificaly
        rb.useGravity = false;
    }

    //Function where collision checking if collider just staying on ground in order to determine if grounded or not is done
    private void OnCollisionStay(Collision collision) {
        //Get the direction the collision point from relative to position of player
        Vector3 dir = collision.contacts[0].point - transform.position;
        //Normalise it
        dir = -dir.normalized;
        //Check the normalised y is greater or equal to 0.9 (0.9 generally sprinting while 1.0 standing stil)
        if (dir.y >= 0.9f) {
            grounded = true;
        }
    }

    //Fixed update function is responsible for jumping and player movement as these need to be done based on fixed update not update
    private void FixedUpdate() {
        //If player movement state isn't iiinteracting
        if (playerMovement != pM.INTERACTING) {
            //Calculate how fast player should be moving
            Vector3 targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            targetVelocity = rotationPosition.TransformDirection(targetVelocity);
            targetVelocity *= speed * speedMultiplier;
            // Apply a force that attempts to reach target velocity
            velocity = rb.velocity;
            velocityChange = (targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -velocityClamp, velocityClamp);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -velocityClamp, velocityClamp);
            velocityChange.y = 0;

            rb.AddForce(velocityChange, ForceMode.VelocityChange);

            // Jump if grounded and input for jump pressed
            if (grounded && Input.GetButton("Jump")) {
                //Jumpine velocity for the player
                rb.velocity = new Vector3(velocity.x, Mathf.Sqrt(jumpVelocity), velocity.z);
            }
            //Increases for pushing downward when up in the air
            else if (grounded == false) {
                downForce = 22;
            }
            else if (downForce != 15) {
                downForce = 15;
            }
        }

        // We apply gravity manually for more tuning control
        rb.AddForce(new Vector3(0, -downForce * rb.mass, 0));

        grounded = false;
    }

    //Update method calls camera type and movement type constantly
    private void Update() {
        rotationPosition.localRotation = Quaternion.Euler (0, cameraTransform.transform.eulerAngles.y, 0);
        //Function checks and manages aspects of player movement relevant to movement type 
        MovementType();
    }

    //Simple public function for changing movement state
    public void SetMovement(int state) {
        //Movement states
        switch (state) {
            //Interacting
            case 0:
                playerMovement = pM.INTERACTING;
                break;
            //Crouching
            case 1:
                playerMovement = pM.CROUCHING;
                break;
            //Walking
            case 2:
                playerMovement = pM.WALKING;
                break;
            default:
                Debug.Log("Given value for ChangeMovement is too high.");
                break;
        }
    }

    //Simple public function for getting type of movement as an integer
    public int GetMovement() {
        return (int)playerMovement;
    }

    //Function for the movement types if the player
    void MovementType() {
        //Switch for different movement types
        switch (playerMovement) {

            case pM.INTERACTING:
                //Checks for player walking
                if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0) {

                    //Unlock cursor
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    playerMovement = pM.WALKING;
                }
                else if (UnityEngine.Cursor.lockState == CursorLockMode.Locked) {
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                }
                break;

            //Crouching
            case pM.CROUCHING:
                speed = crouchSpeed;
                if (Input.GetButtonDown("Crouch") || Input.GetKeyDown(KeyCode.LeftControl)) {
                    speed = walkSpeed;
                    collider.center = new Vector3(0, 0, 0);
                    collider.height = 2;
                    pC.UnCrouch();
                    playerMovement = pM.WALKING;
                }
                break;

            //Walking
            case pM.WALKING:
                if (Input.GetButtonDown("Crouch") || Input.GetKeyDown(KeyCode.LeftControl)) {
                    speed = crouchSpeed;
                    collider.center = new Vector3(0, -0.5f, 0);
                    collider.height = 1;
                    pC.Crouch();
                    playerMovement = pM.CROUCHING;
                }
                else if (Input.GetButton("Sprint")) {
                    speed = runSpeed;
                }
                else if (!Input.GetButton("Sprint")) {
                    speed = walkSpeed;
                }
                break;

            default:
                Debug.Log("Given value for MovementType is too high.");
                break;
        }
    }

}