﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityStandardAssets.CrossPlatformInput;
using Utility;


// Requires a game object with the Player tag to work; otherwise will just throw exceptions.
// Watch out for having focal point above the player, as the raycasting might cause sudden jumps.
//TODO: _verticalOffsetSize is buggy at the moment.
//TODO: create [HideInInspector] public properties for private variables so that they can be accessed by static methods
[RequireComponent(typeof(Camera))]
public class CameraMovementScript : MonoBehaviour
{
    #region Fields

	private Transform _focalPoint; //what the camera looks at
	private Transform _pivot; // camera rig pivot point
	private Transform _hardPos; // point where camera should be
	private Transform _softPos; // Lerps to hardPos for smooth movemnt;
	private GameObject _mainCam; // main camera
	private Transform _playerTransform; //the player
	private int _playerLayer;
	private LayerMask _playerLayerMask;

	private bool _resettingCamera;
	private bool _isOverridden;
	private float _currentCooldown;


	[SerializeField] private string _unobtrusiveObjectTag = "Unobtrusive";
//    TODO consider renaming or merging the next two fields to make their uses clearer.
    [SerializeField] private float _spherecastRadius = 0.01f; //the "size" of the camera, adjusted to avoid wall clipping
//    [SerializeField] private float _cameraSize = .5f; // the "size" of the camera in positioning calculations. Possible duplicate of the above field, but they might also have different uses.
    [SerializeField] private float _overrideCooldownTime = 3;

	[Range(0,1)] public const float FocalPointLerpAmount = 0.1f; //how quickly the focal point and pivot position themselves
	[Range(0,1)] public const float GeneralLerpAmount = 0.5f; //controls most lerping
	[Range(0, 1)] public const float CameraCenterLerpAmount = 0.1f; //controls how quickly the camera centers on rest
	[SerializeField] private float _horizontalRotationSpeed = 2; // Camera horizontal rotation speed
//	[SerializeField] private float _verticalRotationSpeed = 2; // Camera horizontal rotation speed
	[SerializeField] private float _maxFocalPointVerticalOffset = .5f; //how much above the character the focal point should be able to move
	private float _verticalOffsetStepSize;

//    Zoom
	private float _zoomDistance;
	private float _zoomStepLength;
    [SerializeField] [Range(1,5)] private float _defaultZoomLevel = 2.5f;
	[SerializeField] private float _zoomSpeed = 1;
    [SerializeField] [Range(1, 5)] private int _numberOfZoomLevels = 3;
    [HideInInspector]private int CurrentZoomStep { get { return (int) ((_mainCam.transform.position -_focalPoint.position).magnitude / _zoomStepLength); } }
    [SerializeField] private float _defaultLookRotation = 15;

//    Field of View
    [SerializeField] private float _defaultFieldOfView = 60;
	[SerializeField] private float _maxFieldOfView = 100;
    [SerializeField] private float _fovAndZoomModificationStartPoint = 90; // the amount of degrees at which we start modifying FoV and zoom distances.
	private float _fieldOfViewStepSize;
    private Camera _cameraComponent;

	[SerializeField] public float WallDetectionAngle = 45;

	[SerializeField] public float FocalPointHeight = 1.65f; // how far above the character the focal point should be

    //	Constraints
	[SerializeField] private float _maxXRotation = 75; //maximum angle for vertical movement
	[SerializeField] private float _minXRotation = -60; //minimum angle for vertical movement
    [SerializeField] private float _maxZoomDistance = 5;
    private float _maxModifiedZoomDistance;
    private float MaxModifiedZoomDistance
    {
        get { return _maxModifiedZoomDistance;}
        set
        {
            _maxModifiedZoomDistance = value >= _maxZoomDistance ? _maxZoomDistance : value <= _minZoomDistance
                ? _minZoomDistance : value;
        }
    }

    [SerializeField] private float _minZoomDistance  = 1;

//	For greater player customizability
	[SerializeField] private bool _invertYAxis  = true;
	[SerializeField] private bool _invertXAxis;
	[SerializeField] private bool _invertZoom = true;

    #endregion


    #region Initialization

	// Use this for initialization
	private void Start ()
	{
		InitializeVariables();
		InstantiateFocalPoint();
		InitializeRig();
		InitializePositions();
	}

	private void InitializeVariables()
	{
	    MaxModifiedZoomDistance = _maxZoomDistance;
		var totalLength = _maxZoomDistance - _minZoomDistance;
		_zoomStepLength = totalLength / (_numberOfZoomLevels-1);
		_zoomDistance = _defaultZoomLevel;
		_currentCooldown = 0;
		_isOverridden = false;
		_resettingCamera = false;
		_fieldOfViewStepSize = (_maxFieldOfView - _defaultFieldOfView) / Math.Abs(_minXRotation);
	    _verticalOffsetStepSize = _maxFocalPointVerticalOffset / Math.Abs(_minXRotation);
		_playerLayer = LayerMask.NameToLayer("Player");
		_playerLayerMask = 1 << _playerLayer;
	}

    //	Find and set the various game objects related to the rig
    private void InitializeRig()
    {
        _pivot = new GameObject("CameraPivot").transform;
        _pivot.SetParent(gameObject.transform);

        _hardPos = new GameObject("CameraHardPos").transform;
        _hardPos.SetParent(_pivot);

        _softPos = new GameObject("CameraSoftPos").transform;
        _softPos.SetParent(gameObject.transform);

        _mainCam = Camera.main.gameObject;
        if (_mainCam == null) //if there is no camera, create one!
        {
            _mainCam = new GameObject("MainCamera");
            _mainCam.AddComponent<Camera>();
            _mainCam.tag = "MainCamera";
        }

        _cameraComponent = _mainCam.GetComponent<Camera>();

        _cameraComponent.nearClipPlane = 0.01f;
        _mainCam.transform.SetParent(gameObject.transform);
    }

//	Find the player and create a focal point above them
    private void InstantiateFocalPoint()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        if (_playerTransform == null)
        {
            throw new Exception("There is no object with the Player tag in the scene. This script will not work without one.");
        }

        _playerTransform.gameObject.layer = _playerLayer;
        _focalPoint = new GameObject("CameraFocalPoint").transform;
        _focalPoint.position = _playerTransform.position + _playerTransform.up * FocalPointHeight;
        _focalPoint.rotation = _playerTransform.rotation;
        _focalPoint.SetParent(_playerTransform);
    }

//	Hard sets the camera's position for initialization
    private void InitializePositions()
    {
        _pivot.position = _focalPoint.position;
        _pivot.rotation = _focalPoint.rotation;
        _pivot.RotateAround(_pivot.position, _pivot.right, _defaultLookRotation);
        _hardPos.position = _pivot.position - _pivot.forward * _zoomDistance;
        _softPos.position = _hardPos.position;
        _mainCam.transform.position = _softPos.position;
    }

    #endregion


    private void LateUpdate () {
//		move pivot to behind focal point
		_pivot.position = Vector3.Lerp(_pivot.position, _focalPoint.position, FocalPointLerpAmount);

//		Checks to see if the player has changed the camera, and if so, leaves that angle
		if (_isOverridden && _currentCooldown <= 0)
		{
			_isOverridden = false;
		} else if (_isOverridden && _currentCooldown > 0)
		{
			_currentCooldown -= Time.deltaTime;
		}

		//If the player is using the automatic camera
		if (!_isOverridden)
		{
			CenterCamera();
			CheckWalls(); // this should be redundant, but somehow feels like it makes a difference.
		}

		if (CrossPlatformInputManager.GetButton("Center Camera") && !_resettingCamera)
		{
			StartCoroutine(ResetCamera());
		}

		RotateHorizontal();
		RotateVertical();

//	    TODO: refactor this into a separate Zoom-method that checks whether the player is using a keyboard/mouse or pad
		if (CrossPlatformInputManager.GetButtonDown("Zoom In"))
		{
			ZoomIn();
		}
		else if (CrossPlatformInputManager.GetButtonDown("Zoom Out"))
		{
			ZoomOut();
		} else
		{
			ZoomScrollWheel();
		}


        PositionRig();
        ModifyZoomDistance();

		if (Math.Abs(CrossPlatformInputManager.GetAxis("Mouse Y")) > 0.01 && Math.Abs(CrossPlatformInputManager.GetAxis("Mouse X")) > 0.01)
		{
			EngageManualOverride();
		}
	}


    #region Zoom

    private void ZoomOut()
    {
        Zoom(_zoomDistance + _zoomStepLength);
    }

    private void ZoomIn()
    {
        Zoom(_zoomDistance - _zoomStepLength);
    }

    private void ZoomScrollWheel()
    {
        //Get scroll wheel input
        var zoom = CrossPlatformInputManager.GetAxisRaw("Mouse ScrollWheel") * _zoomSpeed;
        zoom = _invertZoom ? -zoom : zoom;

        //calculate total resulting zoom
        var zoomAmount = zoom + _zoomDistance;

        Zoom(zoomAmount);
    }

    private void Zoom(float zoomAmount)
    {
        //check that zoom amount is within specified bounds and zoom
        if (zoomAmount <= _maxZoomDistance && zoomAmount >= _minZoomDistance)
        {
            _zoomDistance = zoomAmount;
        }
        else if (zoomAmount > _maxZoomDistance)
        {
            _zoomDistance = _maxZoomDistance;
        }
        else if (zoomAmount < _minZoomDistance)
        {
            _zoomDistance = _minZoomDistance;
        }
    }

    private void ModifyZoomDistance()
    {
        //Get the amount of degrees off from looking straight ahead we are
        var angle = Vector3.Angle(_focalPoint.up, _pivot.forward);
        if (angle <= _fovAndZoomModificationStartPoint)
        {
/*            var adjacentCathetusLength = Vector3.Project(
                _hardPos.position - _focalPoint.position.normalized, -_focalPoint.up).magnitude * _maxZoomDistance;*/
            var adjacentCathetusLength = Vector3.Project(
                 _hardPos.position , -_focalPoint.up).magnitude;
            var hypLength = UtilsTrigonometry.HypotenuseLength(angle, adjacentCathetusLength);
            MaxModifiedZoomDistance = hypLength;

//            print(hypLength);
        }

        UpdateFieldOfView(angle);
//        TODO: add distance modifier for looking down on player
    }

    #endregion

    #region Automatic behavior

	private void CenterCamera()
	{
//		//Rotate camera to face player
		_pivot.rotation = Quaternion.Lerp(_pivot.rotation,
			Quaternion.LookRotation(
				Vector3.ProjectOnPlane(_focalPoint.position - _mainCam.transform.position, _pivot.up),
				Vector3.up), FocalPointLerpAmount);

//			Sets the vertical alignment to behind the player
		_pivot.rotation = Quaternion.Lerp(_pivot.rotation,
			Quaternion.LookRotation(_focalPoint.position - _mainCam.transform.position, _pivot.up), FocalPointLerpAmount);
	}

	private IEnumerator ResetCamera()
	{
		_resettingCamera = true;

		var elapsedTime = 0f;

		while (Vector3.Angle(_pivot.forward, _focalPoint.forward) > 1 && elapsedTime < .5f)
     		{
     			_pivot.rotation = _focalPoint.rotation;
     			PositionRig(CameraCenterLerpAmount);
     			elapsedTime += Time.deltaTime;
     			yield return new WaitForEndOfFrame();
     		}
     		_resettingCamera = false;
     	}

    #endregion

    #region Rotation

	private void RotateHorizontal()
	{
		//Rotate horizontal pivot around focal point according to input
		var rotationY = CrossPlatformInputManager.GetAxis("Mouse X") * _horizontalRotationSpeed;
		rotationY = _invertXAxis ? -rotationY : rotationY;

		_pivot.RotateAround(_focalPoint.position, Vector3.up, rotationY);
	}

	//Rotate around x axis
	private void RotateAroundX(float rotationAmount)
	{
		_pivot.RotateAround(_focalPoint.position, _pivot.right, rotationAmount);
	}

	private void RotateVertical()
	{
		//Rotates camera along the vertical axis.
		var rotationX = CrossPlatformInputManager.GetAxis("Mouse Y") * _horizontalRotationSpeed;
		rotationX = _invertYAxis ? -rotationX : rotationX; // Invert direction if true

		var angle = _pivot.localEulerAngles.x + rotationX; // Calculate angle
		angle = angle > 180 ? angle - 360 : angle; // If angle is more than 180, return negative values

		if (angle <= _maxXRotation && angle >= _minXRotation) // Check that we're within the allowed range
		{
			RotateAroundX(rotationX);
		}
	}
    #endregion


	private void UpdateFieldOfView(float angle)
	{
	    var offsetModifier = _fovAndZoomModificationStartPoint - angle;
	    if (offsetModifier < 0) offsetModifier = 0;

	    //TODO: make this work properly. Is supposed to raise the focal point on looking up.

/*		var defaultFocalPointPosition = _playerTransform.position + _playerTransform.up * FocalPointHeight;
	    _focalPoint.transform.position = defaultFocalPointPosition + _focalPoint.up * (offsetModifier * _verticalOffsetStepSize);*/

	    var newFieldOfView = offsetModifier * _fieldOfViewStepSize + _defaultFieldOfView;
	   	SetFieldOfView(newFieldOfView);
	}

    private void SetFieldOfView(float newFieldOfView)
    {
        newFieldOfView = newFieldOfView < _defaultFieldOfView
            ? _defaultFieldOfView
            : newFieldOfView > _maxFieldOfView
                ? _maxFieldOfView
                : newFieldOfView;

        _cameraComponent.fieldOfView =
            Mathf.Lerp(_cameraComponent.fieldOfView, newFieldOfView, FocalPointLerpAmount);

    }

    #region Positioning


    private void PositionRig(float lerpAmount = GeneralLerpAmount)
    {
        PositionHardPos();
        PositionSoftPos(lerpAmount);
        PositionCamera(lerpAmount);
    }


//	Move _hardPos into desired position
    private void PositionHardPos()
    {
        var zoomedDistance = _zoomDistance - (_maxZoomDistance - MaxModifiedZoomDistance);
        if (zoomedDistance < _minZoomDistance) zoomedDistance = _minZoomDistance;

        _hardPos.position = Vector3.Lerp(_hardPos.position,
            _pivot.position - _pivot.forward*zoomedDistance,
            FocalPointLerpAmount);
    }

    private void PositionSoftPos(float lerpAmount = GeneralLerpAmount)
	{
		_softPos.position = Vector3.Lerp(_softPos.position, _hardPos.position, lerpAmount);
		_softPos.rotation = Quaternion.Lerp(_softPos.rotation, _hardPos.rotation, lerpAmount);
	}

	private void PositionCamera(float lerpAmount = GeneralLerpAmount)
	{
	    var softPosDirection = (_softPos.position - _focalPoint.position).normalized;

	    var newCameraPosition =
			IsObstructed(_focalPoint.position, softPosDirection,
				Vector3.Distance(_focalPoint.position, _softPos.position));

	    _mainCam.transform.position = Vector3.Lerp(
	        _mainCam.transform.position, newCameraPosition, lerpAmount);


		_mainCam.transform.rotation = Quaternion.Lerp(
			_mainCam.transform.rotation,
			Quaternion.LookRotation(_focalPoint.position- _mainCam.transform.position),
			lerpAmount);
	}

    #endregion

    private void EngageManualOverride()
    {
        _isOverridden = true;
        _currentCooldown = _overrideCooldownTime;
    }


    #region Collision checks

	private Vector3 IsObstructed(Vector3 origin, Vector3 direction, float distance)
	{
		//LINQ based on: http://answers.unity3d.com/questions/282165/raycastall-returning-results-in-reverse-order-of-c-1.html
	    var hitpoints =
			Physics.SphereCastAll(origin, _spherecastRadius, direction, distance, ~_playerLayerMask)
				.OrderBy(hit => hit.distance)
				.TakeWhileInclusive(hit => hit.transform.CompareTag(_unobtrusiveObjectTag))
				.ToArray().Reverse();

		return FindSpace(hitpoints, direction);
	}

	private Vector3 FindSpace(IEnumerable<RaycastHit> hitpoints, Vector3 direction)
	{
//	    TODO: refactor and beautify this
	    var wallHit = default(RaycastHit);
	    var hitWall = false;
		//TODO: Find out if there is space between objects so that the camera can fit
	    foreach (var hit in hitpoints)
	    {
	        if (!hit.transform.CompareTag(_unobtrusiveObjectTag))
	        {
	            hitWall = true;
	            wallHit = hit;
	            continue;
	        }
//	        TODO test that this still works. Minor refactoring job :)
//	        var spherecastCenter = UtilsVector3.ShapecastCenter(_focalPoint.position, direction, hit.distance);
	        var spherecastCenter = hit.ShapecastCenter(_focalPoint.position, direction);
	        if (Physics.OverlapSphere(hit.point, _spherecastRadius).Length <= 0 || !InspectPosition(hit, direction, spherecastCenter)) continue;
	        return spherecastCenter;
	    }

	    return hitWall? wallHit.ShapecastCenter(_focalPoint.position, direction) : _softPos.position;
	}

    private bool InspectPosition(RaycastHit hit, Vector3 direction, Vector3 spherecastCenter)
    {
        var obstaclePosition = hit.collider.transform.position;
        var cameraToObstacleDistance = Vector3.Distance(spherecastCenter, obstaclePosition);
        return hit.distance + cameraToObstacleDistance + _spherecastRadius >= _zoomDistance;
    }


    private void CheckWalls()
    {
        var hitL = Physics.Raycast(_focalPoint.position,
            Quaternion.AngleAxis(WallDetectionAngle, Vector3.up) * -_focalPoint.forward,
            Vector3.Distance(_focalPoint.position, _mainCam.transform.position));

        var hitR = Physics.Raycast(_focalPoint.position,
            Quaternion.AngleAxis(-WallDetectionAngle, Vector3.up) * -_focalPoint.forward,
            Vector3.Distance(_focalPoint.position, _mainCam.transform.position));
        if (hitR || hitL)
        {
            CenterCamera();
        }
    }
    #endregion
}