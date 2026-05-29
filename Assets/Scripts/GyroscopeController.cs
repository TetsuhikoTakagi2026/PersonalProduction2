using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 加速度センサーを読んで Physics2D.gravity を更新する。
/// スマホを傾けると重力方向が変わり、ひっくり返すと液体が逆方向に落ちる。
/// エディタでは ↑↓←→ キー、または F キーで上下反転をシミュレート。
/// </summary>
public class GyroscopeController : MonoBehaviour
{
    [SerializeField] float gravityStrength = 20f;
    [SerializeField] float smoothSpeed = 5f;

    Vector2 _targetGravity;

    void Start()
    {
        _targetGravity = Vector2.down * gravityStrength;
        Physics2D.gravity = _targetGravity;

        // モバイル: 加速度センサーを明示的に有効化
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    void Update()
    {
#if UNITY_EDITOR
        EditorInput();
#else
        MobileInput();
#endif
        Physics2D.gravity = Vector2.Lerp(Physics2D.gravity, _targetGravity, Time.deltaTime * smoothSpeed);
    }

    void MobileInput()
    {
        if (Accelerometer.current == null) return;
        var accel = Accelerometer.current.acceleration.ReadValue();
        var dir = new Vector2(accel.x, accel.y);
        if (dir.sqrMagnitude < 0.01f) return;
        _targetGravity = dir.normalized * gravityStrength;
    }

    void EditorInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.upArrowKey.isPressed)    _targetGravity = Vector2.up    * gravityStrength;
        if (kb.downArrowKey.isPressed)  _targetGravity = Vector2.down  * gravityStrength;
        if (kb.leftArrowKey.isPressed)  _targetGravity = Vector2.left  * gravityStrength;
        if (kb.rightArrowKey.isPressed) _targetGravity = Vector2.right * gravityStrength;
        if (kb.fKey.wasPressedThisFrame) _targetGravity = -_targetGravity;
    }
}
