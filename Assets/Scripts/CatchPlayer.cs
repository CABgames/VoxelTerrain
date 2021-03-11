/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///CatchPlayer.cs
///Developed by Charlie Bullock
///This class checks if player enters trigger collider for gameobject script attached to and if so assigns player new position.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//This class is using:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatchPlayer : MonoBehaviour
{
    //When the player enters the trigger collider this class is attached to set the player to a new position based on where they currently are but with a new y axis position of 150
    private void OnTriggerEnter(Collider other) {

        if (other.gameObject.tag == "Player") {        
            other.GetComponent<Rigidbody>().velocity = Vector3.zero;
            other.transform.position = new Vector3(other.transform.position.x, 150, other.transform.position.z);
        }
    }
}
