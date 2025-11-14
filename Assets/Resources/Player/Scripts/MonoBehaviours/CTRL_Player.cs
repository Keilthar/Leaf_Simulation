using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class CTRL_Player : MonoBehaviour
{
    float3 _MousePositionKO = new float3(-999, -999, -999);
    [Header("Model")]
    [SerializeField] Animator _Anim;
    [SerializeField] Transform _Model;

    [Header("Movement")]
    [SerializeField] float _MoveSpeed;
    [SerializeField] float _RotationSpeed;

    [Header("Spell : Cast self")]
    [SerializeField] float _CastSelfDuration;
    [SerializeField] float _CastSelfRadius;
    [SerializeField] float _CastSelfForce;
    bool _IsCasting;

    [Header("Spell : Cast sphere")]
    [SerializeField] float _CastSphereMoveSpeed;
    [SerializeField] float _CastSphereRadius;
    [SerializeField] float _CastSphereForce;
    void Start()
    {
        
    }

    void Update()
    {
        Check_Input_Move();
        Check_Input_Rotation();
        Check_Input_SpellCast();
    }

    void Check_Input_Move()
    {
        Vector3 _MoveDirection = Vector3.zero;
        bool _IsMovingForward = false;
        if (Keyboard.current.wKey.isPressed)
        {
            _MoveDirection += transform.forward;
            _IsMovingForward = true;
        }
        if (Keyboard.current.sKey.isPressed)
            _MoveDirection -= transform.forward;
        if (Keyboard.current.aKey.isPressed)
            _MoveDirection -= transform.right;
        if (Keyboard.current.dKey.isPressed)
            _MoveDirection += transform.right;
        
        transform.position += Time.deltaTime * _MoveSpeed * _MoveDirection;
        if (Get_MousePosition(out float3 _MousePosition) == true && (Vector3)_MousePosition != _Model.position)
            _Model.LookAt(_MousePosition);

        if (_MoveDirection != Vector3.zero && _IsCasting == false)
        {
            _Anim.SetBool("IsMoving", true);
            _Anim.SetBool("Moving_Forward", _IsMovingForward);
        }
        else
        {
            _Anim.SetBool("IsMoving", false);
            _Anim.SetBool("Moving_Forward", false);
        }
    }

    void Check_Input_Rotation()
    {
        if (Mouse.current.middleButton.isPressed == false)
            return;
        float2 _MouseMove = Mouse.current.delta.value;
        float _YRotation = _MouseMove.x * Time.deltaTime * _RotationSpeed;
        Quaternion _DeltaRotation = Quaternion.Euler(0f, _YRotation, 0f);
        transform.rotation = _DeltaRotation * transform.rotation;
    }

    void Check_Input_SpellCast()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame == false)
            return;

        if (Get_MousePosition(out float3 _MousePosition) == true)
            StartCoroutine(Cast_Self(transform.position, _MousePosition));
    }

    bool Get_MousePosition(out float3 _MousePosition)
    {
        Vector2 _MousePos = Mouse.current.position.ReadValue();
        _MousePosition = new float3(_MousePos.x, _MousePos.y, 0);
        Camera _Camera = Camera.main;
        Ray _Ray = _Camera.ScreenPointToRay(_MousePosition);
        Plane _Plane = new Plane(Vector3.up, Vector3.zero);
        if (_Plane.Raycast(_Ray, out float _Distance))
        {
            _MousePosition = _Ray.GetPoint(_Distance);
            return true;
        }
        else 
            return false;
    }

    IEnumerator Cast_Self(float3 _PositionStart, float3 _PositionEnd)
    {
        _IsCasting = true;
        _Anim.SetBool("IsCasting", true);
        yield return new WaitForSecondsRealtime(0.3f);

        EntityManager _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        float _Timer = 0f;
        while (_Timer < _CastSelfDuration)
        {
            Entity _ForceEntity = _EntityManager.CreateEntity();
            _EntityManager.AddComponentData(_ForceEntity, new COMP_Force_Sphere
            {
                _Position = transform.position,
                _Radius = 1.5f * _Timer / _CastSelfDuration * _CastSelfRadius,
                _Strength = _Timer / _CastSelfDuration * _CastSelfForce
            });

            yield return new WaitForEndOfFrame();
            _Timer += Time.deltaTime;
        }

        StartCoroutine(Cast_Sphere(_PositionStart, _PositionEnd));

        _IsCasting = false;
        _Anim.SetBool("IsCasting", false);
        yield return null;
    }
    
    IEnumerator Cast_Sphere(float3 _PositionStart, float3 _PositionEnd)
    {
        EntityManager _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        float _DistanceMax = math.distance(_PositionStart, _PositionEnd);
        float _Distance = 0;
        float3 _PositionCurrent = _PositionStart;
        float3 _MoveDirection = math.normalize(_PositionEnd - _PositionStart);

        while (_Distance < _DistanceMax)
        {
            Entity _ForceEntity = _EntityManager.CreateEntity();
            _EntityManager.AddComponentData(_ForceEntity, new COMP_Force_Sphere
            {
                _Position = _PositionCurrent,
                _Radius = _CastSphereRadius,
                _Strength = _CastSphereForce
            });

            yield return new WaitForEndOfFrame();
            _PositionCurrent += Time.deltaTime * _CastSphereMoveSpeed * _MoveDirection;
            _Distance = math.distance(_PositionStart, _PositionCurrent);
        }

        yield return null;
    }
}
