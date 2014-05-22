using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SBCController))]
class SBCControllerEditor: Editor {

	public override void OnInspectorGUI()  {
		GUILayout.Space(8);
		GUILayout.BeginHorizontal();

		GUILayout.FlexibleSpace();

		SBCController controller = this.target as SBCController;

		GUI.enabled = Application.isPlaying;
		if(controller.ServerRunning) {
			GUI.skin.button.normal.textColor = Color.red;
			if(GUILayout.Button("Stop SBC Server", GUILayout.Width(200))) {
				controller.StopSBCServer();
			}
			GUI.skin.button.normal.textColor = Color.white;
		} else {
			GUI.skin.button.normal.textColor = Color.green;
			if(GUILayout.Button("Run SBC Server", GUILayout.Width(200))) {
				controller.RunSBCServer();
			}
			GUI.skin.button.normal.textColor = Color.white;
		}
		GUI.enabled = true;

		GUILayout.FlexibleSpace();

		GUILayout.EndHorizontal();
		GUILayout.Space(8);
		
		DrawDefaultInspector();
	}

}
