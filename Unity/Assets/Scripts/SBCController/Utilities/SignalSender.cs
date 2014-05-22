/*********************************************************************************
 *   SBC Unity based on SteelBattalion.NET.
 *
 * 	By Ruiwei Bu
 *   darkfall3@gmail.com
 *                                                                               *
 *********************************************************************************/

﻿using UnityEngine;
using System.Collections;

// Signal Sender
//  send event using SendMessage to multiple receivers

[System.Serializable]
public class SignalReceiver {
	public GameObject[] receivers;
	public string     action;
	public float      delay;

	public IEnumerator SendWithDelay(MonoBehaviour sender) {
		if(delay > 0.0f)
			yield return new WaitForSeconds(delay);
		foreach(GameObject receiver in receivers) {
			receiver.SendMessage(action, sender, SendMessageOptions.DontRequireReceiver);
		}
	}
}

[System.Serializable]
public class SignalSender {
	public bool once;
	public SignalReceiver[] receivers;

	bool hasFired = false;

	public void SendSignals(MonoBehaviour sender) {
		if(!hasFired || !once) {
			foreach(SignalReceiver receiver in this.receivers) {
				if(sender.gameObject.activeInHierarchy) {
					sender.StartCoroutine(receiver.SendWithDelay(sender));
				} else
					Debug.Log("[SignalSender] Receiver not active, cannot send action: " + receiver.action);
			}
			hasFired = true;
		}
	}
}
