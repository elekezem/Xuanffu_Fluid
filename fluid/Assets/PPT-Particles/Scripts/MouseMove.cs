using UnityEngine;
using System.Collections;


public class MouseMove : MonoBehaviour
{
	void Update ()
	{
		Vector3 temp = Input.mousePosition;
		temp.z = transform.parent.transform.position.z - Camera.main.transform.position.z;
		transform.position = Camera.main.ScreenToWorldPoint(temp);
	}
}
