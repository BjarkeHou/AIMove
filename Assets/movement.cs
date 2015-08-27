using UnityEngine;
using System.Collections;

public class movement : MonoBehaviour {

	public float speed;
	private float currentSpeed = 0;
	private Vector3 direction;
	private CharacterController characterController;

	// Use this for initialization
	void Start () {
		characterController = GetComponent<CharacterController>();
	}
	
	// Update is called once per frame
	void Update () {
		Movement();
	}

	public float getSpeed() {
		return currentSpeed;
	}

	public Vector3 getDirection() {
		return direction;
	}

	void Movement() {
		Vector3 dir = Vector3.zero;
		if(Input.GetKey(KeyCode.LeftArrow))
			dir += Vector3.left;

		if(Input.GetKey(KeyCode.RightArrow))
			dir += Vector3.right;

		if(Input.GetKey(KeyCode.UpArrow))
			dir += Vector3.forward;

		if(Input.GetKey(KeyCode.DownArrow))
			dir += Vector3.back;

		if(!dir.Equals(Vector3.zero))
			currentSpeed = speed;
		else
			currentSpeed = 0.0f;
		direction = dir;
		characterController.SimpleMove(direction.normalized * currentSpeed);
	}
}
