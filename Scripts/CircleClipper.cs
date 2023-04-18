using System.Collections;
using UnityEngine;
using Vector2i = ClipperLib.IntPoint;
using Vector2f = UnityEngine.Vector2;

public class CircleClipper : IClip
{
    private ClipType _clipType = ClipType.Sub;

    private struct TouchLineOverlapCheck
    {
        public float a;
        public float b;
        public float c;
        public float angle;
        public float dividend;

        public TouchLineOverlapCheck(Vector2 p1, Vector2 p2)
        {
            Vector2 d = p2 - p1;
            float m = d.magnitude;
            a = -d.y / m;
            b = d.x / m;
            c = -(a * p1.x + b * p1.y);
            angle = Mathf.Rad2Deg * Mathf.Atan2(-a, b);

            float da;
            if (d.x / d.y < 0f)
                da = 45 + angle;
            else
                da = 45 - angle;

            dividend = Mathf.Abs(1.0f / 1.4f * Mathf.Cos(Mathf.Deg2Rad * da));
        }

        public float GetDistance(Vector2 p) { return Mathf.Abs(a * p.x + b * p.y + c); }
    }

    private readonly float _radius;
    private readonly int _segmentCount;
    private readonly float _touchMoveDistance;
    private readonly Vector2f _currentTouchPoint;
    private readonly Vector2f _previousTouchPoint;
    private readonly TouchPhase _touchPhase;
    private TouchLineOverlapCheck _touchLine;
    private Vector2i[] _vertices;
    private Camera _mainCamera;
    private float _cameraZPos;
    private readonly Mesh _mesh;

    public bool CheckBlockOverlapping(Vector2f p, float size)
    {
        if (_touchPhase == TouchPhase.Began)
        {
            float dx = Mathf.Abs(_currentTouchPoint.x - p.x) - _radius - size / 2;
            float dy = Mathf.Abs(_currentTouchPoint.y - p.y) - _radius - size / 2;
            return dx < 0f && dy < 0f;
        }

        if (_touchPhase == TouchPhase.Moved)
        {
            float distance = _touchLine.GetDistance(p) - _radius - size / _touchLine.dividend;
            return distance < 0f;
        }

        return false;
    }

    public ClipBounds GetBounds()
    {
        if (_touchPhase == TouchPhase.Began)
        {
            return new ClipBounds
            {
                lowerPoint = new Vector2f(_currentTouchPoint.x - _radius, _currentTouchPoint.y - _radius),
                upperPoint = new Vector2f(_currentTouchPoint.x + _radius, _currentTouchPoint.y + _radius)
            };
        }

        if (_touchPhase == TouchPhase.Moved)
        {
            Vector2f upperPoint = _currentTouchPoint;
            Vector2f lowerPoint = _previousTouchPoint;
            if (_previousTouchPoint.x > _currentTouchPoint.x)
            {
                upperPoint.x = _previousTouchPoint.x;
                lowerPoint.x = _currentTouchPoint.x;
            }

            if (_previousTouchPoint.y > _currentTouchPoint.y)
            {
                upperPoint.y = _previousTouchPoint.y;
                lowerPoint.y = _currentTouchPoint.y;
            }

            return new ClipBounds
            {
                lowerPoint = new Vector2f(lowerPoint.x - _radius, lowerPoint.y - _radius), upperPoint = new Vector2f(upperPoint.x + _radius, upperPoint.y + _radius)
            };
        }

        return new ClipBounds();
    }

    public Vector2i[] GetVertices() { return _vertices; }

    public Mesh GetMesh() { return _mesh; }


    public CircleClipper(
        ClipType clipType,
        float radius,
        int segmentCount,
        float touchMoveDistance,
        Vector2f previousTouchPoint,
        Vector2f currentTouchPoint,
        TouchPhase touchPhase)
    {
        _clipType = clipType;
        _radius = radius;
        _segmentCount = segmentCount;
        _touchMoveDistance = touchMoveDistance;
        _previousTouchPoint = previousTouchPoint;
        _currentTouchPoint = currentTouchPoint;
        _touchPhase = touchPhase;
        _mesh = new Mesh();
        _mesh.MarkDynamic();
    }

    public IEnumerator IeDig(Vector2 begin)
    {
        var r = 0.6f;
        while (r < _radius)
        {
            r += 5 * Time.deltaTime;
            yield return null;
            Build(begin, r);
            DestructibleTerrainManager.Instance.Clip(this, _clipType);
        }
    }

    public IEnumerator IeMoveDig(Vector2 begin, Vector2 end)
    {
        var r = 0.6f;
        while (r < _radius)
        {
            r += 5 * Time.deltaTime;
            if ((end - begin).sqrMagnitude > _touchMoveDistance * _touchMoveDistance)
            {
                Build(begin, end, r);
                DestructibleTerrainManager.Instance.Clip(this, _clipType);
            }

            yield return null;
        }
    }

    void Build(Vector2 center, float rad)
    {
        Vector3[] meshVertices = new Vector3[_segmentCount];
        Vector3[] meshNormals = new Vector3[_segmentCount];
        _vertices = new Vector2i[_segmentCount];

        for (int i = 0; i < _segmentCount; i++)
        {
            float angle = Mathf.Deg2Rad * (-90f - 360f / _segmentCount * i);

            Vector2 point = new Vector2(center.x + rad * Mathf.Cos(angle), center.y + rad * Mathf.Sin(angle));
            _vertices[i] = point.ToVector2i();

            meshVertices[i] = point.ToVector3f();
            meshNormals[i] = (meshVertices[i] - center.ToVector3f()) / rad;
        }

        _mesh.Clear();
        _mesh.vertices = meshVertices;
        _mesh.normals = meshNormals;
        _mesh.triangles = Triangulate.Execute(meshVertices).ToArray();
    }

    void Build(Vector2 begin, Vector2 end, float rad)
    {
        int halfSegmentCount = _segmentCount / 2;
        _touchLine = new TouchLineOverlapCheck(begin, end);

        Vector3[] meshVertices = new Vector3[_segmentCount + 2];
        Vector3[] meshNormals = new Vector3[_segmentCount + 2];
        _vertices = new Vector2i[_segmentCount + 2];

        for (int i = 0; i <= halfSegmentCount; i++)
        {
            float angle = Mathf.Deg2Rad * (_touchLine.angle + 270f - 360f / _segmentCount * i);

            Vector2 point = new Vector2(begin.x + rad * Mathf.Cos(angle), begin.y + rad * Mathf.Sin(angle));
            _vertices[i] = point.ToVector2i();

            meshVertices[i] = point.ToVector3f();
            meshNormals[i] = (meshVertices[i] - begin.ToVector3f()) / rad;
        }

        for (int i = halfSegmentCount; i <= _segmentCount; i++)
        {
            float angle = Mathf.Deg2Rad * (_touchLine.angle + 270f - 360f / _segmentCount * i);

            Vector2 point = new Vector2(end.x + rad * Mathf.Cos(angle), end.y + rad * Mathf.Sin(angle));
            _vertices[i + 1] = point.ToVector2i();

            meshVertices[i + 1] = point.ToVector3f();
            meshNormals[i + 1] = (meshVertices[i + 1] - end.ToVector3f()) / rad;
        }

        _mesh.Clear();
        _mesh.vertices = meshVertices;
        _mesh.normals = meshNormals;
        _mesh.triangles = Triangulate.Execute(meshVertices).ToArray();
    }
}