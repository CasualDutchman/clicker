using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class AnalyticsManager : MonoBehaviour {

    public Text ten, one;
    public Button genderButton;

    Gender gender;
    int birthYear;

    public int numberschanged, numberschangedChaos;

    int age = 0;

    public void AddClick(bool chaos) {
        if (chaos) {
            numberschangedChaos++;
        } else {
            numberschanged++;
        }
    }

    public void SendAnalytics(int difficulty) {
        Analytics.CustomEvent("App Closed", new Dictionary<string, object>
                  {
                    { "Numbers changed", numberschanged },
                    { "Numbers changed Chaos", numberschangedChaos },
                    { "Difficulty", difficulty }
                  });
    }

    public void AddToAge(int amount) {
        age = Mathf.Clamp(age + amount, 0, 99); ;

        string ageString = "";

        if (age < 9) {
            ageString = "0" + age;
        } else {
            ageString = age.ToString();
        }

        char[] str = ageString.ToCharArray();
        ten.text = str[0].ToString();
        one.text = str[1].ToString();
    }

    public void ToggleGender() {
        int i = (int)gender;
        i++;
        if (i >= 3)
            i = 0;

        gender = (Gender)i;

        string str = i == 2 ? "Other" : gender.ToString();

        genderButton.transform.GetChild(0).GetComponent<Text>().text = str;
    }

    public void Continue() {
        birthYear = System.DateTime.Now.Year - age;

        Analytics.SetUserBirthYear(birthYear);
        Analytics.SetUserGender(gender);
        Save();
    }

    public void Save() {
        PlayerPrefs.SetInt("UserBirth", birthYear);
        PlayerPrefs.SetInt("UserGender", (int)gender);
    }

    public void Load() {
        birthYear = PlayerPrefs.GetInt("UserBirth");
        gender = (Gender)PlayerPrefs.GetInt("UserGender");
        Analytics.SetUserBirthYear(birthYear);
        Analytics.SetUserGender(gender);
    }
}
