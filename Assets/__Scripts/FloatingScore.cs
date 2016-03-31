using UnityEngine;
using System.Collections.Generic;

public enum FSState {
    IDLE,
    PRE,
    ACTIVE,
    POST
}

public class FloatingScore : MonoBehaviour {

    public FSState state = FSState.IDLE;
    [SerializeField] private int _score = 0;
    public string scoreString;

    public int score {
        get {
            return _score;
        }

        set {
            _score = value;
            scoreString = Utils.AddCommasToNumber(_score);
            GetComponent<GUIText>().text = scoreString;
        }
    }

    public List<Vector3> bezierPts;
    public List<float> fontSizes;
    public float timeStart = -1f;
    public float timeDuration = 1f;
    public string easingCurve = Easing.InOut;

    public GameObject reportFinishTo = null;

    public void Init(List<Vector3> ePts, float eTimeS = 0, float eTimeD = 1) {

        bezierPts = new List<Vector3>(ePts);
        if(ePts.Count == 1) {
            transform.position = ePts[0];
            return;
        }

        if(eTimeS == 0) {
            eTimeS = Time.time;
        }
        timeStart = eTimeS;
        timeDuration = eTimeD;

        state = FSState.PRE;

    }

    public void FSCallback(FloatingScore fs) {
        score = score + fs.score;
    }

    void Update() {

        if(state == FSState.IDLE) {
            return;
        }

        float u = (Time.time - timeStart) / timeDuration;
        float uC = Easing.Ease(u, easingCurve);

        if(u < 0) {
            state = FSState.PRE;
            transform.position = bezierPts[0];
        } else {
            if(u >= 1) {
                uC = 1;
                state = FSState.POST;
                if(reportFinishTo != null) {
                    reportFinishTo.SendMessage("FSCallback", this);
                    Destroy(this.gameObject);
                } else {
                    state = FSState.IDLE;
                }
            } else {
                state = FSState.ACTIVE;
            }

            Vector3 pos = Utils.Bezier(uC, bezierPts);
            transform.position = pos;

            if(fontSizes != null && fontSizes.Count > 0) {
                int size = Mathf.RoundToInt(Utils.Bezier(uC, fontSizes));
                GetComponent<GUIText>().fontSize = size;
            }
        }

    }

}
