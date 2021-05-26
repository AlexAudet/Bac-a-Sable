using UnityEngine;
using UnityEngine.SceneManagement;


public class BoiteEtFusilGameManager : MonoBehaviour
{
    bool gameHasEnded = false;
    public float restartTime = 1f;

    // Start is called before the first frame update
    void Start()
    {
        TargetManager.instance.SpawnTarget();
    }

    public void GameOver()
    {
        if (gameHasEnded==false)
        {
            gameHasEnded = true;
            Debug.Log("GAME OVER");
            Invoke("Restart",restartTime);
        }
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
