using UnityEngine;
using UnityEditor;

/*
 * 
 * 
 * 
 * This script creates -if not already created- a layer called "PPT - ParticleSystem Render Texure"
 * This layer will be assigned to the ojects inside the Camera to Render GO and will be the objects rendered into particles
 * 
 * 
 * 
 * 
 * */

[InitializeOnLoad]
public class Layers
{
	static Layers()
	{
		SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

		SerializedProperty layersProp = tagManager.FindProperty("layers");

		string layerName = "PPT - ParticleSystem Render Texure";

		// --- Unity 5 ---
		bool checkOn = true;

		for (int i = 8; i<31; i++) {
			if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName){
				checkOn = false;
			}
		} 

		int t = 8;
		while (checkOn) {
			SerializedProperty sp = layersProp.GetArrayElementAtIndex(t);
			if (sp.stringValue == "" || sp == null){
				sp.stringValue = layerName;
				tagManager.ApplyModifiedProperties();
				checkOn = false;
			} else {
				t++;
				if (t == 31){
					Debug.LogError("There must be a free slot for a new Layer!");
				}
			}
		}
	}
}
