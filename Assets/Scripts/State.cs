using System;
using System.Collections.Generic;
using UnityEditor.iOS.Xcode;

//ゲームの状態を表すクラス
public class State {
    public Banmen banmen;
    public Mochigoma sente;
    public Mochigoma gote;
    private int turn;
    private int[] lastElementsArray;
    private List<int> availablePositionsList;
    private bool isGameOver;


    // 初期化用のコンストラクタ
    public State(Banmen banmen, Mochigoma sente, Mochigoma gote) {
        this.banmen = banmen;
        this.sente = sente;
        this.gote = gote;
        this.turn = 1;
        this.lastElementsArray = new int[9];
        this.availablePositionsList = new List<int>();
        this.isGameOver = false;
    }

    // コピーコンストラクタ
    public State(State other) {
        this.banmen = new Banmen(other.banmen);
        this.sente = new Mochigoma(other.sente);
        this.gote = new Mochigoma(other.gote);
        this.turn = other.turn;
        this.lastElementsArray = other.lastElementsArray;
        this.availablePositionsList = other.availablePositionsList;
        this.isGameOver = other.isGameOver;
    }

    public void NextTurn() {
        turn++;
    }

    public int Turn() {
        return turn;
    }

    public bool isSenteTurn() {
        return turn % 2 == 1;
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

    public void SetGameOver() {
        isGameOver = true;
    }

    public bool IsGameOver() {
        return isGameOver;
    }

    // 勝者がいるかどうかをチェックし、勝者がいる場合はゲームを終了する
    public GameResult CheckGameOver() {
        GameResult result = CheckWinner();
        if (result != GameResult.None) {
            SetGameOver();
        }
        return result;
    }

    //勝利判定を行う関数
    public GameResult CheckWinner() {
        var (senteArray, goteArray) = CreateBinaryArrays();

        if (HasWinningLine(senteArray)) {
            return GameResult.SenteWin;
        }
        else if (HasWinningLine(goteArray)) {
            return GameResult.GoteWin;
        }
        return GameResult.None;
    }

    //ビンゴラインが揃っているかどうかを判定する関数
    private bool HasWinningLine(int[,] array) {
        for (int i = 0; i < 3; i++) {
            if ((array[i, 0] == 1 && array[i, 1] == 1 && array[i, 2] == 1) ||
                (array[0, i] == 1 && array[1, i] == 1 && array[2, i] == 1)) {
                return true;
            }
        }

        if ((array[0, 0] == 1 && array[1, 1] == 1 && array[2, 2] == 1) ||
            (array[0, 2] == 1 && array[1, 1] == 1 && array[2, 0] == 1)) {
            return true;
        }
        return false;
    }

    //勝利判定のために、自分の駒を1、それ以外を0とした二次元配列を返す関数
    private (int[,], int[,]) CreateBinaryArrays() {
        int[,] senteArray = new int[3, 3];
        int[,] goteArray = new int[3, 3];
        List<List<int>> banmen = this.banmen.GetBanmen();

        for (int i = 0; i < banmen.Count; i++) {
            int lastElement = 0;
            if (banmen[i].Count > 0) {
                lastElement = banmen[i][banmen[i].Count - 1];
            }

            if (lastElement > 0) {
                senteArray[i / 3, i % 3] = 1;
            }
            else if (lastElement < 0) {
                goteArray[i / 3, i % 3] = 1;
            }
        }

        return (senteArray, goteArray);
    }
}
