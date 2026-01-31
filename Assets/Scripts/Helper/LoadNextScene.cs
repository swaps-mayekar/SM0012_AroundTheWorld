using UnityEngine;
using UnityEngine.SceneManagement;

namespace AroundTheWorld
{
    public class LoadNextScene : MonoBehaviour
    {
        void Start()
        {
            LoadScene();
        }

        public void LoadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
