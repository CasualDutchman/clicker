using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class AnalyticsManager : MonoBehaviour {

    public int numberschanged, numberschangedChaos;

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
}
