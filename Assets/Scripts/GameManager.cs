using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Analytics;

public class GameManager : MonoBehaviour {

    public int level = 1;

    public GameType type;

    public bool chaosMode;

    public Transform slider;
    public Text objectiveText, maxLevelText;
    public Image timerImage;
    Button[] buttons = new Button[9];
    int[] buttonValue = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9};
    int[] objective;
    int nextIndex = 0;

    int maxNumbersAchieved;

    float timer;
    float maxTimer = 1.5f;

    //analytics ----
    int anaHitPlay;
    int anaPlayed;
    int anaPlayedChaos;

    float adTimer;
    bool ableToAd = false;

	void Start () {
        Transform holdingButtons = GameObject.FindObjectOfType<GridLayoutGroup>().transform;
        for (int i = 0; i < 9; i++) {
            buttons[i] = holdingButtons.GetChild(i).GetComponent<Button>();
            int index = i;
            buttons[i].onClick.AddListener(() => HitButton(index));
        }

        Load();

        NewNumber(StartType.Restart);
    }
	
    /// <summary>
    /// Loads from Playerprefs
    /// </summary>
    void Load() {
        maxNumbersAchieved = PlayerPrefs.GetInt("HighScore");
        maxLevelText.text = maxNumbersAchieved.ToString();
    }

    

    /// <summary>
    /// Saves to Playerprefs
    /// </summary>
    void Save() {
        PlayerPrefs.SetInt("HighScore", maxNumbersAchieved);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Rewrites the game's level indicators
    /// </summary>
    /// <param name="newLevel"></param>
    void NewNumber(StartType newLevel) {
        nextIndex = 0;
        timer = 0;
        if (newLevel == StartType.AddOne) {
            level += 1;
            maxTimer += 0.5f;
            if(level > maxNumbersAchieved)
                maxNumbersAchieved = level;
            maxLevelText.text = maxNumbersAchieved.ToString();

            if (chaosMode) {
                anaPlayedChaos++;
            } else {
                anaPlayed++;
            }
        } 
        else if (newLevel == StartType.RemoveOne) {
            level = Mathf.Clamp(level - 1, 1, int.MaxValue);
            maxTimer = Mathf.Clamp(maxTimer - 0.5f, 1.5f, float.MaxValue);

            if (chaosMode) {
                anaPlayedChaos++;
            } else {
                anaPlayed++;
            }
        } 
        else if(newLevel == StartType.Restart) {
            level = 1;
            maxTimer = 1.5f;
        }

        if (chaosMode) {
            changeButtonValue(true);
        }

        objectiveText.text = GetRandomNumber();
    }

    /// <summary>
    /// Sets the objective array
    /// </summary>
    /// <returns></returns>
    string GetRandomNumber() {
        string number = "";
        objective = new int[level];
        for (int i = 0; i < objective.Length; i++) {
            objective[i] = Random.Range(1, 9);
            number += objective[i];
        }
        return number;
    }

    /// <summary>
    /// change the button values for chaosmode
    /// </summary>
    /// <param name="change">true for chaos, false to reset</param>
    void changeButtonValue(bool change) {
        for (int i = 0; i < 9; i++) {
            int temp = 0;
            if (change) {
                int swapper = Random.Range(0, 9);
                temp = buttonValue[swapper];
                buttonValue[swapper] = buttonValue[i];
            }
            buttonValue[i] = change ? temp : i + 1;
        }
        for (int i = 0; i < 9; i++) {
        Text text = buttons[i].transform.GetChild(0).GetComponent<Text>();
        text.text = buttonValue[i].ToString();
        }
    }

    /// <summary>
    /// When the player hits a button
    /// </summary>
    /// <param name="i"></param>
    void HitButton(int i) {
        if (objective[nextIndex] == buttonValue[i]) {
            nextIndex++;

            string temp = "<color=#00ff00ff>";
            for (int j = 0; j < nextIndex; j++) {
                temp += objective[j];
            }
            temp += "</color>";
            for (int k = nextIndex; k < objective.Length; k++) {
                temp += objective[k];
            }
            objectiveText.text = temp;
            
            if (nextIndex >= level) {
                NewNumber(StartType.AddOne);
                return;
            }
        } else {
            NewNumber(StartType.RemoveOne);
        }
    }

	void Update () {
        adTimer += Time.deltaTime;
        if (adTimer >= 60 * 10) {
            ableToAd = true;
        }

        if (Input.GetKey(KeyCode.Escape)) {
            if (type == GameType.Menu) {
                Save();

                Analytics.CustomEvent("App Closed", new Dictionary<string, object>
                  {
                    { "Hit Play", anaHitPlay },
                    { "Numbers changed", anaPlayed },
                    { "Numbers changed Chaos", anaPlayedChaos }
                  });

                Application.Quit();
            }
            else if (type == GameType.Game) {
                type = GameType.ToMenu;
            }
        }

        if (type == GameType.Game) {
            timer += Time.deltaTime;

            timerImage.fillAmount = 1 - (timer / maxTimer);

            if (timer >= maxTimer) {
                NewNumber(StartType.RemoveOne);
            }
        }
        else if (type == GameType.ToGame) {
            timer += Time.deltaTime * 300;
            slider.localPosition = Vector3.Lerp(Vector3.zero, new Vector3(-400, 0, 0), timer / 400);
            if (slider.localPosition.x <= -400) {
                slider.localPosition = new Vector3(-400, 0, 0);
                timer = 0;
                type = GameType.Game;
            }
        } else if (type == GameType.ToMenu) {
            timer += Time.deltaTime * 300;
            slider.localPosition = Vector3.Lerp(new Vector3(-400, 0, 0), Vector3.zero, timer / 400);
            if (slider.localPosition.x >= 0) {
                slider.localPosition = Vector3.zero;
                timer = 0;
                type = GameType.Menu;
            }
        }
    }

    /// <summary>
    /// When the player hits start
    /// </summary>
    public void HitStart() {
        type = GameType.ToGame;
        NewNumber(StartType.Restart);
        timerImage.fillAmount = 1;
        if (!chaosMode)
            changeButtonValue(false);

        anaHitPlay++;

        if (ableToAd) {
            adTimer = 0;
            Debug.Log("run Ad");
            ableToAd = false;
        }
    }

    /// <summary>
    /// When the player hits home
    /// </summary>
    public void HitHome() {
        if (ableToAd) {
            adTimer = 0;
            Debug.Log("run Ad");
            ableToAd = false;
        }
        type = GameType.ToMenu;
    }

    /// <summary>
    /// Toggle chaosmode on/off
    /// </summary>
    public void toggleChoasMode() {
        chaosMode = !chaosMode;
    }
}

public enum StartType {
    Restart, AddOne, RemoveOne
}

public enum GameType {
    Menu, Game, ToGame, ToMenu
}