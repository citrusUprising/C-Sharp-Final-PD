using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//************** use UnityOSC namespace...
using UnityOSC;
//*************

public class MovePlayer : MonoBehaviour {

	public float speed;
	public Text countText;

	private Rigidbody rb;
	private int count;
	private float xVelocity;
	private float zVelocity;
	private bool stop = false;

	//************* Need to setup this server dictionary...
	Dictionary<string, ServerLog> servers = new Dictionary<string, ServerLog> ();
	//*************

	// Use this for initialization
	void Start ()
	{
		Application.runInBackground = true; //allows unity to update when not in focus

		//************* Instantiate the OSC Handler...
	    OSCHandler.Instance.Init ();
		OSCHandler.Instance.SendMessageToClient ("pd", "/unity/trigger", "ready");
		OSCHandler.Instance.SendMessageToClient("pd", "/unity/tempo", 500);
        OSCHandler.Instance.SendMessageToClient("pd", "/unity/playseq", 1);
        //*************

        rb = GetComponent<Rigidbody> ();
		count = 0;
		setCountText ();
	}

	void FixedUpdate()
	{
		float moveHorizontal = Input.GetAxis ("Horizontal");
		float moveVertical = Input.GetAxis ("Vertical");

		xVelocity = Math.Abs(rb.velocity.x);
		zVelocity = Math.Abs(rb.velocity.z);

		Vector3 movement = new Vector3 (moveHorizontal, 0, moveVertical);

		rb.AddForce (movement*speed);

		//************* Routine for receiving the OSC...
		OSCHandler.Instance.UpdateLogs();
		Dictionary<string, ServerLog> servers = new Dictionary<string, ServerLog>();
		servers = OSCHandler.Instance.Servers;

		foreach (KeyValuePair<string, ServerLog> item in servers) {
			// If we have received at least one packet,
			// show the last received from the log in the Debug console
			if (item.Value.log.Count > 0) {
				int lastPacketIndex = item.Value.packets.Count - 1;

				//get address and data packet
				countText.text = item.Value.packets [lastPacketIndex].Address.ToString ();
				countText.text += item.Value.packets [lastPacketIndex].Data [0].ToString ();
			}
		}
		//*************
		if (!stop) StartCoroutine(setTempo());
	}

	private IEnumerator setTempo()
	{
		stop = true;
		OSCHandler.Instance.SendMessageToClient("pd", "/unity/tempo", 500 / ((xVelocity + zVelocity) / 4 + 1));
		yield return new WaitForSeconds(.4f);
		stop = false;
	}

	void OnTriggerEnter(Collider other)
    {
        //Debug.Log("-------- COLLISION!!! ----------");

        if (other.gameObject.CompareTag ("Pick Up"))
		{
			other.gameObject.SetActive (false);
			count = count + 1;
			setCountText ();
			if (count >= 8) OSCHandler.Instance.SendMessageToClient("pd", "/unity/playseq", 0);
        }
        else if(other.gameObject.CompareTag("Wall"))
        {
            //Debug.Log("-------- HIT THE WALL ----------");
            // trigger noise burst whe hitting a wall.
            OSCHandler.Instance.SendMessageToClient("pd", "/unity/colwall", 1);
        }
    }

	void setCountText()
	{
		countText.text = "Count: " + count.ToString ();

		//************* Send the message to the client...
		OSCHandler.Instance.SendMessageToClient ("pd", "/unity/trigger", count);
		//*************
	}
}
