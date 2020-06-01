using UnityEngine;
using System.Collections;

public class TagChildren : MonoBehaviour {



	// Use this for initialization
	void OnEnable () {
		ChangeLayersRecursively (transform, "PPT - ParticleSystem Render Texure");

		int myLayer = LayerMask.NameToLayer("PPT - ParticleSystem Render Texure");

		gameObject.GetComponent<Camera> ().cullingMask = 1 << myLayer;


		Camera.main.cullingMask &= ~(1 << myLayer);

	}

	public void ChangeLayersRecursively(Transform trans, string name)
	{
		trans.gameObject.layer = LayerMask.NameToLayer(name);
		foreach(Transform child in trans)
		{            
			ChangeLayersRecursively(child, name);
		}
	}

	
}
