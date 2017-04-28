using UnityEngine;

public class CursorLockScript : MonoBehaviour
{


	private static bool _isLocked;

	//Lock cursor
	public static void LockCursor()
	{

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
		//hide cursor
		_isLocked = true;
	}

	//Release cursor
	public static void UnlockCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		_isLocked = false;
	}

	public static void ToggleCursorLock()
	{
	    if (!_isLocked) LockCursor();
	    else UnlockCursor();
	}
}
