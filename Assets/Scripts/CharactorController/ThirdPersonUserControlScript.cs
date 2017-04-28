using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class ThirdPersonUserControlScript : MonoBehaviour
{
    private CharacterControllerPhysics _character; // A reference to the ThirdPersonCharacter on the object
    private Transform _cam; // A reference to the main camera in the scenes transform
    private Vector3 _camForward; // The current forward direction of the camera
    private Vector3 _move;
    private bool _jump; // the world-relative desired move direction, calculated from the camForward and user input.

    [SerializeField] [Range(0, 1)] private float _walkSpeed = 0.5f;


    private void Start()
    {
        // get the transform of the main camera
        if (Camera.main != null)
        {
            _cam = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning(
                "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.",
                gameObject);
            // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
        }

        // get the third person character ( this should never be null due to require component )
        _character = GetComponent<CharacterControllerPhysics>();
    }


    private void Update()
    {
        if (!_jump)
        {
            _jump = CrossPlatformInputManager.GetButtonDown("Jump");
        }
    }


    // Fixed update is called in sync with physics
    private void FixedUpdate()
    {
        // read inputs
        var horizontal = CrossPlatformInputManager.GetAxisRaw("Horizontal");
        var vertical = CrossPlatformInputManager.GetAxisRaw("Vertical");
        var crouch = CrossPlatformInputManager.GetButton("Crouch");

        // calculate move direction to pass to character
        // calculate camera relative direction to move:
        _camForward = Vector3.Scale(_cam.forward, new Vector3(1, 0, 1)).normalized;
        _move = vertical * _camForward + horizontal * _cam.right;

//        TODO: create button mapping for this (if necessary)
        // walk speed multiplier
        if (Input.GetKey(KeyCode.LeftShift)) _move *= _walkSpeed;

        // pass all parameters to the character control script
        _character.Move(_move, crouch, _jump);
        _jump = false;
    }
}
