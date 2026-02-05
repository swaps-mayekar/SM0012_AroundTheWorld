using UnityEngine;
using UnityEngine.SceneManagement;

namespace AroundTheWorld
{
    public class BackToMenu : MonoBehaviour
    {
        public void OnClick_BackToMenu()
        {
            SceneManager.LoadScene("1_MenuScene");
        }
    }
}
