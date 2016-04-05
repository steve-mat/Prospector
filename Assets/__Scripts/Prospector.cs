using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public enum ScoreEvent {
    DRAW,
    MINE,
    MINEGOLD,
    GAMEWIN,
    GAMELOSS
};

public class Prospector : MonoBehaviour {

	public static Prospector S;
    public static int SCORE_FROM_PREV_ROUND = 0;
    public static int HIGH_SCORE = 0;

    public float reloadDelay = 0.5f;

    public Vector3 fsPosMid = new Vector3(0.5f, 0.90f, 0);
    public Vector3 fsPosRun = new Vector3(0.5f, 0.75f, 0);
    public Vector3 fsPosMid2 = new Vector3(0.5f, 0.5f, 0);
    public Vector3 fsPosEnd = new Vector3(1.0f, 0.65f, 0);

    public Deck deck;
	public TextAsset deckXML;

    public Layout layout;
    public TextAsset layoutXML;
    public Vector3 layoutCenter;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Transform layoutAnchor;

    public CardProspector target;
    public List<CardProspector> tableau;

    public List<CardProspector> discardPile;

    public List<CardProspector> drawPile;

    public int chain = 0;
    public int scoreRun = 0;
    public int score = 0;
    public FloatingScore fsRun;

    public GUIText GTGameOver;
    public GUIText GTRoundResult;

	void Awake(){

		S = this;

        if(PlayerPrefs.HasKey("ProspectorHighScore")) {
            HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
        }

        score = score + SCORE_FROM_PREV_ROUND;
        SCORE_FROM_PREV_ROUND = 0;

        GameObject go = GameObject.Find("GameOver");
        if(go != null) {
            GTGameOver = go.GetComponent<GUIText>();
        }

        go = GameObject.Find("RoundResult");
        if(go != null) {
            GTRoundResult = go.GetComponent<GUIText>();
        }

        ShowResultGTs(false);

        go = GameObject.Find("HighScore");
        string hScore = "High Score: " + Utils.AddCommasToNumber(HIGH_SCORE);
        go.GetComponent<GUIText>().text = hScore;

	}

	void Start() {

        Scoreboard.S.score = score;

		deck = GetComponent<Deck> ();
		deck.InitDeck (deckXML.text);
		Deck.Shuffle (ref deck.cards);

        layout = GetComponent<Layout>();
        layout.ReadLayout(layoutXML.text);

        drawPile = ConvertListCardsToListCardProspectors(deck.cards);
        LayoutGame();

	}

    private List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD) {

        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach(Card tCD in lCD) {
            tCP = (CardProspector)tCD;
            lCP.Add(tCP);
        }
        return lCP;

    }

    private void LayoutGame() {

        if(layoutAnchor == null) {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        CardProspector cp;

        foreach(SlotDef tSD in layout.slotDefs) {
            cp = Draw();
            cp.faceUp = tSD.faceUp;
            cp.transform.parent = layoutAnchor;
            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x, layout.multiplier.y * tSD.y, -tSD.layerID);
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            cp.state = CardState.TABLEAU;
            cp.SetSortingLayerName(tSD.layerName);

            tableau.Add(cp);
        }

        foreach(CardProspector tCP in tableau) {
            foreach(int hidden in tCP.slotDef.hiddenBy) {
                cp = FindCardByLayoutID(hidden);
                tCP.hiddenBy.Add(cp);
            }
        }

        MoveToTarget(Draw());

        UpdateDrawPile();

    }

    private CardProspector Draw() {
        CardProspector cd = drawPile[0];
        drawPile.RemoveAt(0);
        return cd;
    }

    public void CardClicked(CardProspector cd) {

        switch(cd.state) {
            case CardState.TARGET:
                break;
            case CardState.DRAWPILE:
                MoveToDiscard(target);
                MoveToTarget(Draw());
                UpdateDrawPile();
                ScoreManager(ScoreEvent.DRAW);
                break;
            case CardState.TABLEAU:
                bool validMatch = true;

                if(!cd.faceUp) {
                    validMatch = false;
                }

                if(!AdjacentRank(cd, target)) {
                    validMatch = false;
                }

                if(!validMatch) {
                    return;
                }

                tableau.Remove(cd);
                MoveToTarget(cd);
                SetTableauFaces();
                ScoreManager(ScoreEvent.MINE);
                break;
        }

        CheckForGameOver();

    }

    private void MoveToDiscard(CardProspector cd) {

        cd.state = CardState.DISCARD;
        discardPile.Add(cd);
        cd.transform.parent = layoutAnchor;
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);

    }

    private void MoveToTarget(CardProspector cd) {

        if(target != null) {
            MoveToDiscard(target);
        }

        target = cd;
        cd.state = CardState.TARGET;
        cd.transform.parent = layoutAnchor;
        cd.transform.localPosition= new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID);
        cd.faceUp = true;
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);

    }

    private void UpdateDrawPile() {

        CardProspector cd;

        for(int i = 0; i < drawPile.Count; i++) {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x), layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y), -layout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false;
            cd.state = CardState.DRAWPILE;
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }

    }

    public bool AdjacentRank(CardProspector c0, CardProspector c1) {

        if(!c0.faceUp || !c1.faceUp) {
            return false;
        }

        if(Math.Abs(c0.rank - c1.rank) == 1) {
            return true;
        }

        if((c0.rank == 1 && c1.rank == 13) || (c0.rank == 13 && c1.rank == 1)) {
            return true;
        }

        return false;

    }

    private CardProspector FindCardByLayoutID(int layoutID) {
        foreach(CardProspector tCP in tableau) {
            if(tCP.layoutID == layoutID) {
                return tCP;
            }
        }
        return null;
    }

    private void SetTableauFaces() {

        foreach(CardProspector cd in tableau) {
            bool fUP = true;
            foreach(CardProspector cover in cd.hiddenBy) {
                if(cover.state == CardState.TABLEAU) {
                    fUP = false;
                }
            }
            cd.faceUp = fUP;
        }

    }

    private void CheckForGameOver() {
        if(tableau.Count == 0) {
            GameOver(true);
            return;
        }

        if(drawPile.Count > 0) {
            return;
        }

        foreach(CardProspector cd in tableau) {
            if(AdjacentRank(cd, target)) {
                return;
            }
        }

        GameOver(false);
    }

    private void GameOver(bool isOver) {
        if(isOver == true) {
            ScoreManager(ScoreEvent.GAMEWIN);
        } else {
            ScoreManager(ScoreEvent.GAMELOSS);
        }

        Invoke("ReloadLevel", reloadDelay);
        //SceneManager.LoadScene("__Prospector_Scene_0");

    }

    private void ReloadLevel() {
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    private void ScoreManager(ScoreEvent sEvt) {

        List<Vector3> fsPts;
        switch(sEvt) {
            case ScoreEvent.DRAW:
            case ScoreEvent.GAMEWIN:
            case ScoreEvent.GAMELOSS:
                chain = 0;
                score = score + scoreRun;
                scoreRun = 0;
                if(fsRun != null) {
                    fsPts = new List<Vector3>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null;
                }
                break;
            case ScoreEvent.MINE:
                chain++;
                scoreRun = scoreRun + chain;
                FloatingScore fs;
                Vector3 p0 = Input.mousePosition;
                p0.x = p0.x / Screen.width;
                p0.y = p0.y / Screen.height;

                fsPts = new List<Vector3>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(chain, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
                if(fsRun == null) {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                } else {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }

        switch(sEvt) {
            case ScoreEvent.GAMEWIN:
                GTGameOver.text = "Round Over";
                SCORE_FROM_PREV_ROUND = score;
                GTRoundResult.text = "You won this round!\nRound score: " + score;
                ShowResultGTs(true);
                break;
            case ScoreEvent.GAMELOSS:
                GTGameOver.text = "Game Over";
                if(HIGH_SCORE <= score) {
                    string sRR = "You got the high score!\nHigh score: " + score;
                    GTRoundResult.text = sRR;
                    HIGH_SCORE = score;
                    PlayerPrefs.SetInt("ProspectorHighScore", score);
                } else {
                    GTRoundResult.text = "Your final score for the game was: " + score;
                }
                ShowResultGTs(true);
                break;
            default:
                Debug.Log("Score: " + score + " scoreRun: " + scoreRun + " chain: " + chain);
                break;
        }

    }

    private void ShowResultGTs(bool show) {
        GTGameOver.gameObject.SetActive(show);
        GTRoundResult.gameObject.SetActive(show);
    }
}
