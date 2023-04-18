using UnityEngine;
using Vector2i = ClipperLib.IntPoint;
using Vector2f = UnityEngine.Vector2;

public class InputHandle : MonoBehaviour
{
    public ClipType clipType = ClipType.Sub;

    public float radius = 1.2f;

    public int segmentCount = 10;

    public float touchMoveDistance = 0.1f;

    private Camera _mainCamera;
    private float _cameraZPos;
    private TouchPhase _touchPhase;
    private Vector2f _currentTouchPoint;
    private Vector2f _previousTouchPoint;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _cameraZPos = _mainCamera.transform.position.z;
    }

    private void Update()
    {
        if (TouchUtility.Enabled && TouchUtility.TouchCount > 0)
        {
            Touch touch = TouchUtility.GetTouch(0);
            Vector2 touchPosition = touch.position;

            _touchPhase = touch.phase;

            if (touch.phase == TouchPhase.Began)
            {
                _currentTouchPoint = _mainCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, -_cameraZPos));
                // todo: optimize by using pool to cached object
                var circleClipper = new CircleClipper(clipType,
                    radius,
                    segmentCount,
                    touchMoveDistance,
                    _previousTouchPoint,
                    _currentTouchPoint,
                    _touchPhase);
                StartCoroutine(circleClipper.IeDig(_currentTouchPoint));
                _previousTouchPoint = _currentTouchPoint;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                _currentTouchPoint = _mainCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, -_cameraZPos));

                if ((_currentTouchPoint - _previousTouchPoint).sqrMagnitude <= touchMoveDistance * touchMoveDistance)
                    return;

                // todo: optimize by using pool to cached object
                var circleClipper = new CircleClipper(clipType,
                    radius,
                    segmentCount,
                    touchMoveDistance,
                    _previousTouchPoint,
                    _currentTouchPoint,
                    _touchPhase);
                StartCoroutine(circleClipper.IeMoveDig(_previousTouchPoint, _currentTouchPoint));

                _previousTouchPoint = _currentTouchPoint;
            }
        }
    }
}