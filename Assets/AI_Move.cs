using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;  

public enum MODE {
	STRAIGHT, HUNT, HIDE
}

public class AI_Move : MonoBehaviour
{
	public MODE currentMode;
	private GameObject target;
	public float resolution = 0.2f;
	public float maxWaypointDistance = 2.0f;
	public float movementSpeed = 5f;
	public bool drawLines;
	private float raycastHeight = 1f;
	private Vector3 cachedTargetPosition;
	private CharacterController characterController;
	private bool isSearching = false;

	private bool standingStill = true;

	Seeker seeker;

	Path path;
	int currentWaypoint;

	void Start ()
	{
		target = GameObject.Find("Player");
		cachedTargetPosition = new Vector3(target.transform.position.x,target.transform.position.y,target.transform.position.z);
		seeker = GetComponent<Seeker> ();
		characterController = GetComponent<CharacterController>();
		switch (currentMode) {
		case MODE.STRAIGHT:
			seeker.StartPath (this.transform.position, target.transform.position, OnPathComplete);
			isSearching = true;
			break;
		case MODE.HUNT:
			seeker.StartPath (this.transform.position, target.transform.position, OnPathComplete);
			isSearching = true;
			break;
		case MODE.HIDE:
			if(isInLOS()){
				Vector3 hidingPoint = findHidingPoint();
				seeker.StartPath (this.transform.position, hidingPoint, OnPathComplete);
				isSearching = true;
			};
			break;
		default:
			break;
		}
	}

	void FixedUpdate ()
	{
		float changedDistanceFromLastPos = Vector3.Distance(target.transform.position, cachedTargetPosition);
		float distanceToTarget = Vector3.Distance(this.transform.position, target.transform.position);
		//Debug.Log("Moved: " + changedDistanceFromLastPath);
		//Debug.Log ("Need to move: " + distanceToTarget*resolution);

		switch (currentMode) {
		case MODE.STRAIGHT:
			if (changedDistanceFromLastPos > distanceToTarget*resolution && !isSearching) {
				// Find new path
				cachedTargetPosition = new Vector3(target.transform.position.x,target.transform.position.y,target.transform.position.z);
				seeker.ReleaseClaimedPath();
				seeker.StartPath (this.transform.position, cachedTargetPosition, OnPathComplete);
				isSearching = true;
			}
			break;
		case MODE.HUNT:
			movement targetMovement = target.GetComponent<movement>();
			if (changedDistanceFromLastPos > distanceToTarget*resolution && !isSearching) {
				// Find new path
				standingStill = false;
				cachedTargetPosition = new Vector3(target.transform.position.x,target.transform.position.y,target.transform.position.z);
				seeker.ReleaseClaimedPath();
				Vector3 huntPos = cachedTargetPosition + targetMovement.getDirection() * targetMovement.getSpeed();
				seeker.StartPath (this.transform.position, huntPos, OnPathComplete);
				isSearching = true;
			} 
			else if(targetMovement.getSpeed() < 0.1f && !standingStill && !isSearching/*cachedTargetPosition != target.transform.position*/) {
				standingStill = true;
				cachedTargetPosition = new Vector3(target.transform.position.x,target.transform.position.y,target.transform.position.z);
				seeker.ReleaseClaimedPath();
				seeker.StartPath (this.transform.position, cachedTargetPosition, OnPathComplete);
				isSearching = true;
			}
			break;
		case MODE.HIDE:
			if(path == null)
				return;
			if(currentWaypoint >= path.vectorPath.Count && !isSearching) {
				if(isInLOS()){
					Vector3 hidingPoint = findHidingPoint();
					seeker.ReleaseClaimedPath();
					seeker.StartPath (this.transform.position, hidingPoint, OnPathComplete);
					isSearching = true;
				};
			}
			break;
		default:
			break;
		}

		Move();
	}

	public void OnPathComplete (Path p)
	{
		if(p.error)
			return;

		currentWaypoint = 0;
		path = p;
		isSearching = false;
	}

	void Move() {
		if(path == null)
			return;

		if(currentWaypoint >= path.vectorPath.Count)
			return;

		Vector3 dir = (path.vectorPath[currentWaypoint] - this.transform.position).normalized * movementSpeed * Time.fixedDeltaTime;
		characterController.SimpleMove(dir);

		if(Vector3.Distance(this.transform.position, path.vectorPath[currentWaypoint]) < maxWaypointDistance)
			currentWaypoint++;
	}

	bool isInLOS() {
		RaycastHit hitInfo = new RaycastHit();
		if(Physics.Raycast(this.transform.position+new Vector3(0,0.5f,0), (target.transform.position+new Vector3(0,0.5f,0))-(this.transform.position+new Vector3(0,0.5f,0)), out hitInfo)) {
			if(hitInfo.transform.parent == null)
				return false;
			if(hitInfo.transform.parent.name == "Player") {
				Debug.Log("OMG SPOTTED LOL!");
				return true;
			}
			else return false;
		}
		return false;
	}

	Vector3 findHidingPoint() {
		// Look to the left first because reasons.
		float adder = 0.1f;

		List<Vector3> hidingPoints = new List<Vector3>();

		for(int i = 0; i <= 18; i++) {
			//Debug.DrawLine(target.transform.position, (this.transform.position-target.transform.position)*10000, Color.black, 10000);
			Vector3 direction = rotateAroundY(((this.transform.position-target.transform.position)).normalized, -0.9f+adder*i).normalized;

			//Using RaycastAll to look through NPC.
			Vector3 origin = new Vector3(target.transform.position.x, target.transform.position.y + raycastHeight, target.transform.position.z);
			RaycastHit[] hits = Physics.RaycastAll(origin, direction);
			// We cant be sure they are in order, so we have to sort them
			RaycastHit[] sortedHits = sortHits(hits);

			foreach(RaycastHit hit in sortedHits) {
				if(drawLines)Debug.DrawLine(target.transform.position, hit.point, Color.red, 10000);
				// First hit expected to be a wall
				if(hit.transform.name == "level-mesh2") {
					RaycastHit newWall = new RaycastHit();
					// If second raycast hits, it should also be a wall.. Othervise we have reached the edge of the map
					if(Physics.Raycast(hit.point+direction, direction, out newWall)) { // Means we didnt go off the edge of the map
						if(drawLines)Debug.DrawLine(hit.point, newWall.point, Color.blue, 10000);
						RaycastHit hidingPoint = new RaycastHit();
						//Fires a raycast back, to hit the backside of the cliff we wanna hide behind.
						if(Physics.Raycast(newWall.point, -direction, out hidingPoint)) {
							if(hidingPoint.distance > 10) {
								if(drawLines)Debug.DrawLine(newWall.point, hidingPoint.point, Color.green, 10000);
								hidingPoints.Add(hidingPoint.point+(direction*2));
							}
						}
					}
				}
			}
		}

		float distanceToBestCandidate = 10000000;
		Vector3 bestCandidate = hidingPoints[0];
		foreach(Vector3 hidingPoint in hidingPoints) {
			if(Vector3.Distance(this.transform.position, hidingPoint)<distanceToBestCandidate) {
				bestCandidate = hidingPoint;
				distanceToBestCandidate = Vector3.Distance(this.transform.position, hidingPoint);
			}
		}

		// Raycast from 5 degrees left and right, then 10, then 15
		// Use raycast all
		// if only one hit, we know its the edge of the map
		// if 3 or more hits, we know we went throug a wall
		// find the second hit, and add a couple of units, and make that position the place to go.
		Debug.Log ("HidingPoint: X = " + bestCandidate.x + " Y = " + bestCandidate.y + " Z = " + bestCandidate.z);
		return bestCandidate;
	}

	// Credit to Unity Forum for this calculation.
	Vector3 rotateAroundY(Vector3 v, float angle) {
		float sin = Mathf.Sin( angle );
		float cos = Mathf.Cos( angle );
		
		float tx = v.x;
		float tz = v.z;
		v.x = (cos * tx) + (sin * tz);
		v.z = (cos * tz) - (sin * tx);

		return v;
	}

	RaycastHit[] sortHits(RaycastHit[] hits) {
		RaycastHit[] h = new RaycastHit[hits.Length];
		for(int i = 0; i < hits.Length; i++) {
			h[i] = hits[i];
			for(int j = i; j>0; j--) {
				if(h[j].distance < h[j-1].distance) {
					RaycastHit buffer = h[j-1];
					h[j-1] = h[j];
					h[j] = buffer;
				}
			}
		}
		return h;
	}

}
