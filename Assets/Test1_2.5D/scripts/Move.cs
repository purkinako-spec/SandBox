using UnityEngine;

public class Move : MonoBehaviour
{
    [SerializeField] private float speed = 5.0f;

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // オブジェクトの「前」と「右」のベクトルを基準に移動方向を計算
        Vector3 movement = (transform.forward * vertical + transform.right * horizontal).normalized;

        // Time.deltaTimeを掛けることでタイムスケールの影響を受ける
        transform.Translate(movement * speed * Time.deltaTime, Space.World);
    }
}
