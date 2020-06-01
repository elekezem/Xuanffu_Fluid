using UnityEngine;
using System.Collections;

public class CameraRender : MonoBehaviour {

	public PPTGeneratorRender script;



	void OnPostRender(){
		script.OnPostRender ();
	}
}
