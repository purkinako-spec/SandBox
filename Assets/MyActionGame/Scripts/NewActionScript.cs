using UnityEngine;

namespace MyActionGame.NewAction
{
    public class NewActionScript : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
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
