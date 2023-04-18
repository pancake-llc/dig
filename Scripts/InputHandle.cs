using UnityEngine;
using Vector2i = ClipperLib.IntPoint;
using Vector2f = UnityEngine.Vector2;

public class InputHandle : MonoBehaviour
{
    public ClipType clipType = ClipType.Sub;

    public float radius = 1.2f;

    public int segmentCount = 10;

    public float touchMoveDistance = 0.1f;

    private Camera mainCamera;
    private float cameraZPos;
    private TouchPhase touchPhase;
    private Vector2f currentTouchPoint;
    private Vector2f previousTouchPoint;

    private void Awake()
    {
        mainCamera = Camera.main;
        cameraZPos = mainCamera.transform.position.z;
    }

    private void Update()
    {
        if (TouchUtility.Enabled && TouchUtility.TouchCount > 0)
        {
            Touch touch = TouchUtility.GetTouch(0);
            Vector2 touchPosition = touch.position;

            touchPhase = touch.phase;

            if (touch.phase == TouchPhase.Began)
            {
                currentTouchPoint = mainCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, -cameraZPos));
                // pool
                CircleClipper circleClipper = new CircleClipper(clipType,
                    radius,
                    segmentCount,
                    touchMoveDistance,
                    previousTouchPoint,
                    currentTouchPoint,
                    touchPhase);
                StartCoroutine(circleClipper.IeDig(currentTouchPoint));
                previousTouchPoint = currentTouchPoint;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                currentTouchPoint = mainCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, -cameraZPos));
 
                if ((currentTouchPoint - previousTouchPoint).sqrMagnitude <= touchMoveDistance * touchMoveDistance)
                    return;
               
                CircleClipper circleClipper = new CircleClipper(clipType,
                    radius,
                    segmentCount,
                    touchMoveDistance,
                    previousTouchPoint,
                    currentTouchPoint,
                    touchPhase);
                StartCoroutine(circleClipper.IeMoveDig(previousTouchPoint, currentTouchPoint));

                previousTouchPoint = currentTouchPoint;
            }
        }
    }
}