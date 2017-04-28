using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorLocker : MonoBehaviour
{

	private bool _cursorIsLocked;

	// Use this for initialization
	void Start () {
		CursorLockScript.LockCursor();
		_cursorIsLocked = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetButtonDown("Cancel"))
		{
			ToggleCursorLock();
		}
	}

	private void ToggleCursorLock()
	{
		if (_cursorIsLocked) CursorLockScript.UnlockCursor();
		else CursorLockScript.LockCursor();
	}
}
