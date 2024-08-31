using System;
using System.Collections.Generic;
using UnityEditor.iOS.Xcode;

//ゲームの状態を表すクラス
public class State {
    public Banmen banmen;
    public Mochigoma sente;
    public Mochigoma gote;
    public int turn;
    private int[] lastElementsArray;
    private List<int> availablePositionsList;

    // 初期化用のコンストラクタ
    public State(Banmen banmen, Mochigoma sente, Mochigoma gote) {
        this.banmen = banmen;
        this.sente = sente;
        this.gote = gote;
        this.turn = 1;
        this.lastElementsArray = new int[9];
        this.availablePositionsList = new List<int>();
    }

    // コピーコンストラクタ
    public State(State other) {
        this.banmen = new Banmen(other.banmen);
        this.sente = new Mochigoma(other.sente);
        this.gote = new Mochigoma(other.gote);
        this.turn = other.turn;
        this.lastElementsArray = other.lastElementsArray;
        this.availablePositionsList = other.availablePositionsList;
    }

    public void NextTurn() {
        turn++;
    }

    public void UpdateLastElementsArray() {
        List<List<int>> banmen = this.banmen.GetBanmen();
        for (int i = 0; i < banmen.Count; i++) {
            if (banmen[i].Count > 0) {
                lastElementsArray[i] = banmen[i][banmen[i].Count - 1];
            }
            else {
                lastElementsArray[i] = 0;
            }
        }
    }

    public int[] LastElementsArray() {
        return lastElementsArray;
    }

    public void UpdateAvailablePositionsList() {
        int maxKomaSize = 0;
        List<int> currentMochigoma = this.turn % 2 == 1 ? this.sente.GetMochigoma() : this.gote.GetMochigoma();
        //現在の持ち駒の中で絶対値が最も大きいもののサイズをmaxKomaSizeに格納
        foreach (int komaSize in currentMochigoma) {
            if (Math.Abs(komaSize) > maxKomaSize) {
                maxKomaSize = Math.Abs(komaSize);
            }
        }

        availablePositionsList = new List<int>();
        int[] lastElementsArray = this.LastElementsArray();
        for (int i = 0; i < lastElementsArray.Length; i++) {
            if (Math.Abs(lastElementsArray[i]) < maxKomaSize) {
                availablePositionsList.Add(i);
            }
        }
    }

    public List<int> AvailablePositionsList() {
        return availablePositionsList;
    }
}
