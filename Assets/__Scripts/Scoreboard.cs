using UnityEngine;
using System.Collections.Generic;

public class Scoreboard : MonoBehaviour {

    public static Scoreboard S;

    public GameObject prefabFloatingScore;

    [SerializeField] private int _score = 0;
    public string _scoreString;

    public int score {
        get {
            return _score;
        }

        set {
            _score = value;
            scoreString = Utils.AddCommasToNumber(_score);
        }
    }

    public string scoreString {
        get {
            return _scoreString;
        }

        set {
            _scoreString = value;
            GetComponent<GUIText>().text = _scoreString;
        }
    }

    void Awake() {
        S = this;
    }

    public void FSCallBack(FloatingScore fs) {
        score = score + fs.score;
    }

    public FloatingScore CreateFloatingScore(int amt, List<Vector3> pts) {

        GameObject go = Instantiate(prefabFloatingScore);
        FloatingScore fs = go.GetComponent<FloatingScore>();

        fs.score = amt;
        fs.reportFinishTo = gameObject;
        fs.Init(pts);

        return fs;
    }

}
