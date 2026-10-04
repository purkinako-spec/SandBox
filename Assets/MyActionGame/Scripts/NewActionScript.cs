using UnityEngine;

namespace MyActionGame.NewAction
{
    public class NewActionScript : MonoBehaviour
    {
        [Header("移動設定")]
        [Tooltip("移動速度 (メートル/秒)")]
        public float moveSpeed = 5f;

        private CharacterController controller;

        // Start is called before the first frame update
        void Start()
        {
            controller = GetComponent<CharacterController>();
        }

        // Update is called once per frame
        void Update()
        {
            // 水平・垂直入力を取得
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            // ローカル空間での移動方向を計算
            Vector3 move = transform.right * h + transform.forward * v;
            move.Normalize(); // 対角移動時の速度を一定に保つ
            move *= moveSpeed * Time.deltaTime;

            // CharacterController があればそれを使い、無ければ Transform で移動
            if (controller != null && controller.enabled)
            {
                controller.Move(move);
            }
            else
            {
                transform.position += move;
            }
        }

        /// <summary>
        /// アクションを実行するメソッド
        /// </summary>
        public void PerformAction()
        {
            Debug.Log($"{name} がアクションを実行しました！");
        }
    }
}
