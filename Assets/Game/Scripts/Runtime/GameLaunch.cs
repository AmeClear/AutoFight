using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameLaunch : MonoBehaviour
{
    public GameObject UIGame;
    public GameObject Player;
    public GameObject Enemy;

    // Start is called before the first frame update
    void Start()
    {
        UIGame.SetActive(true);
        Player.SetActive(true);
        Enemy.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
