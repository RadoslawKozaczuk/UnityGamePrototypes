﻿using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    public static bool Locked
    {
        set => _instance.enabled = !value;
    }

    static HexMapCamera _instance;
    
    [SerializeField] float _stickMinZoom = -250, _stickMaxZoom = -45;
    [SerializeField] float _swivelMinZoom = 90, _swivelMaxZoom = 45;
    [SerializeField] float _moveSpeedMinZoom = 400, _moveSpeedMaxZoom = 100;
    [SerializeField] float _rotationSpeed = 180;
    [SerializeField] HexGrid _grid;

    Transform _swivel, _stick;
    float _zoom = 1f;
    float _rotationAngle;

    void Awake()
    {
        _instance = this;

        _swivel = transform.GetChild(0);
        _stick = _swivel.GetChild(0);

        // start position
        AdjustZoom(10);
        AdjustPosition(10, 10);
    }

    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
            AdjustZoom(zoomDelta);

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f)
            AdjustRotation(rotationDelta);

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
            AdjustPosition(xDelta, zDelta);
    }

    public static void ValidatePosition() => _instance.AdjustPosition(0f, 0f);

    void AdjustRotation(float delta)
    {
        _rotationAngle += delta * _rotationSpeed * Time.deltaTime;

        if (_rotationAngle < 0f)
            _rotationAngle += 360f;
        else if (_rotationAngle >= 360f)
            _rotationAngle -= 360f;

        transform.localRotation = Quaternion.Euler(0f, _rotationAngle, 0f);
    }

    void AdjustPosition(float xDelta, float zDelta)
    {
        // vector need to be normalized in otder to prevent the camera moving faster diagonally
        var direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;

        // slow the camera movement proportionally to the key's delays
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));

        // zoom is interpolated based on the current zoom
        float distance = Mathf.Lerp(_moveSpeedMinZoom, _moveSpeedMaxZoom, _zoom) * damping * Time.deltaTime;

        var position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (_grid.CellCountX * HexMetrics.ChunkSizeX - 0.5f) * (2f * HexMetrics.InnerRadius);
        position.x = Mathf.Clamp(position.x, 0f, xMax + 10);

        float zMax = (_grid.CellCountZ * HexMetrics.ChunkSizeZ -1) * (1.5f * HexMetrics.OuterRadius);
        position.z = Mathf.Clamp(position.z, 40f, zMax);

        return position;
    }

    void AdjustZoom(float delta)
    {
        _zoom = Mathf.Clamp01(_zoom + delta);

        float distance = Mathf.Lerp(_stickMinZoom, _stickMaxZoom, _zoom);
        _stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(_swivelMinZoom, _swivelMaxZoom, _zoom);
        _swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}