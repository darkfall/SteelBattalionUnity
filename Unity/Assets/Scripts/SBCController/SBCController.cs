/*********************************************************************************
 *   SBC Unity based on SteelBattalion.NET.
 *
 * 	By Ruiwei Bu
 *   darkfall3@gmail.com
 *                                                                               *
 *********************************************************************************/

﻿using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SBCController: TcpConnectionHandler {

	static SBCController instance;
	public static SBCController Instance {
		get { return instance; }
	}

	public string serverAddr = "127.0.0.1";
	public int serverPort = 8080;
	public int refreshRate = 33;

	public float retryInterval = 3.0f;
	public SBCPacketButton buttonStates;
	public SBCPacketButton buttonStatesCache;
	public SBCPacketJoystick joystickStates;
    public bool connected = false;
public string serverBinaryDir = "Mechgic/SBCController/";

	Process serverProcess = null;
	int receivedSBCButtonUpdate = 0;

	public bool ServerRunning {
		get {
			return serverProcess != null && !serverProcess.HasExited;
		}
	}

	// for testing
//	int currentIntensity = 1;

	void OnDestroy() {
		this.StopSBCServer();
	}

	void Awake() {
		instance = this;
	}

	void Start () {
		Loom c = Loom.Current;
		c.enabled = true;
	}

	void LateUpdate () {
		// received = 1, wait 1 frame = 2, reset up/down status = 3
		if(receivedSBCButtonUpdate > 0) {
			++receivedSBCButtonUpdate;
			buttonStates = buttonStatesCache;
			if(receivedSBCButtonUpdate == 3) {
				for(int i=0; i<(int)SBCButton._Max; ++i) {
					if(buttonStates.buttons[i] == 5)
						buttonStates.buttons[i] = (int)SBCButtonState.Down;
					else if(buttonStates.buttons[i] == 6)
						buttonStates.buttons[i] = (int)SBCButtonState.Up;
				}

				receivedSBCButtonUpdate = 0;
			}
		}
	}

	void OnEnable() {
	#if UNITY_EDITOR
		EditorApplication.playmodeStateChanged += StateChange;
	#endif
	}

	#if UNITY_EDITOR
	void StateChange() {
		if (!EditorApplication.isPlaying) {
			this.StopSBCServer();
		}
	}
	#endif

	public void RunSBCServer() {
		try {
			UnityEngine.Debug.Log("Starting SBC Server...");

			serverProcess = new Process();

			if(Application.platform == RuntimePlatform.OSXEditor ||
			   Application.platform == RuntimePlatform.OSXPlayer) {

				serverProcess.StartInfo = new ProcessStartInfo() {
					WorkingDirectory = Path.Combine(Application.dataPath, "Mechgic/SBCController/"),
					FileName = "/usr/bin/mono",
					Arguments = Path.Combine(Application.dataPath, serverBinaryDir + "SBCController_OSX.exe") + " " + refreshRate.ToString() + " " + serverPort.ToString(),
					RedirectStandardOutput = true,
					//RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,

				};

			} else if(Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer){

				serverProcess.StartInfo = new ProcessStartInfo() {
					WorkingDirectory = Path.Combine(Application.dataPath, serverBinaryDir),
					FileName = Path.Combine(Application.dataPath, serverBinaryDir + "SBCController.exe"),
					Arguments = serverPort.ToString() + " " + refreshRate.ToString(),
					RedirectStandardOutput = true,
					//RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,

				};
			} else {
				UnityEngine.Debug.LogError("Platform " + Application.platform.ToString() + " not supported by SBC controller");
				return;
			}

			serverProcess.Exited += (object sender, System.EventArgs e) => {

			};
			serverProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
				if(e.Data.ToLower().StartsWith("server started")) {
					Loom.QueueOnMainThread(() => {
						TcpConnection.Connect(serverAddr, serverPort, this);
					});
				}
				else if(e.Data.ToLower().StartsWith("error")) {
					Loom.QueueOnMainThread(() => {
						UnityEngine.Debug.LogError("Error running SBC server");
						this.StopSBCServer();
					});
				} else {
					UnityEngine.Debug.Log("SBCServer: " + e.Data);
				}
			};

			serverProcess.Start();
			serverProcess.BeginOutputReadLine();

		} catch(System.Exception ex) {
			UnityEngine.Debug.LogError("Error starting SBC server: " + ex.Message);
		}
	}

	public void StopSBCServer() {
		if(this.ServerRunning) {
			this.Disconnect();
			this.StopAllCoroutines();
			try {
				serverProcess.Kill();
			}
		    catch {

			}
			finally {
				serverProcess = null;
			}
		}
	}

	void OnGUI() {

	}

	IEnumerator TryConnectServer() {
		yield return new WaitForSeconds(retryInterval);
		this.StopCoroutine("TryConnectServer");
		TcpConnection.Connect(serverAddr, serverPort, this);
		UnityEngine.Debug.Log("Trying to connect to the SBC server...");
	}

	public override void OnConnectionFailed(string reason)
	{
		Loom.QueueOnMainThread(() => {
			UnityEngine.Debug.LogError ("Unable to connect to the SBC server");
		});
	}

	public override void OnNetworkError(System.Exception exp)
	{
		Loom.QueueOnMainThread(() => {
			UnityEngine.Debug.LogWarning ("Network error, attempting to reconnect...");
			this.StartCoroutine(TryConnectServer());
		});
	}

	public override void OnConnected(System.Net.Sockets.TcpClient client)
	{
		UnityEngine.Debug.Log("SBC server connected!!!");
	}

	public override void OnDataReceived(SBCPacketType t, byte[] data)
	{
        connected = true;
		switch (t)
		{
		case SBCPacketType.ButtonState:
			if(receivedSBCButtonUpdate > 1) {
				buttonStatesCache = (SBCUtility.Deserialize(SBCPacketType.ButtonState, data)) as SBCPacketButton;
			} else {

				buttonStates = (SBCUtility.Deserialize(SBCPacketType.ButtonState, data)) as SBCPacketButton;
			}
			receivedSBCButtonUpdate = 1;
			break;

		case SBCPacketType.JoystickState:
			joystickStates = (SBCUtility.Deserialize(SBCPacketType.JoystickState, data)) as SBCPacketJoystick;
			break;
		}
	}
}
