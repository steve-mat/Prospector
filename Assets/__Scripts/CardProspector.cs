using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CardState {
    DRAWPILE,
    TABLEAU,
    TARGET,
    DISCARD
}

public class CardProspector : Card {

    public CardState state = CardState.DRAWPILE;

    public List<CardProspector> hiddenBy = new List<CardProspector>();

    public int layoutID;

    public SlotDef slotDef;


    public override void OnMouseUpAsButton() {
        Prospector.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }

}
