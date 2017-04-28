using UnityStandardAssets.CrossPlatformInput;

namespace DefaultNamespace
{
    public class CameraZoom
    {
        #region Zoom
//        TODO: take a CameraMovementScript instead and access properties? See ZoomIn() for general idea.
        public static void ZoomOut(float minZoomDistance, int currentZoomStep, float zoomStepLength, out float zoomDistance, float maxZoomDistance)
        {
            var newDistance =minZoomDistance + (currentZoomStep +1) * zoomStepLength;
            zoomDistance = newDistance < maxZoomDistance - minZoomDistance ? newDistance : maxZoomDistance;
        }
/*
        private void ZoomIn(CameraMovementScript rig)
        {
            var newDistance = rig._minZoomDistance + (rig.CurrentZoomStep-1) * rig._zoomStepLength;
            rig._zoomDistance = newDistance >= rig._minZoomDistance ? newDistance : rig._minZoomDistance;
        }

        private void Zoom()
        {
            //Get scroll wheel input
            var zoom = CrossPlatformInputManager.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
            zoom = _invertZoom ? -zoom : zoom;

            //calculate total resulting zoom
            var zoomAmount = zoom + _zoomDistance;

            //check that zoom amount is within specified bounds and zoom
            if (zoomAmount <= _maxZoomDistance && zoomAmount >= _minZoomDistance)
            {
                _zoomDistance = zoomAmount;
            } else if (zoomAmount > _maxZoomDistance)
            {
                _zoomDistance = _maxZoomDistance;
            } else if (zoomAmount < _minZoomDistance)
            {
                _zoomDistance = _minZoomDistance;
            }
        }*/
        #endregion
    }
}