using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    // ゲームが終了したかどうかを表すbool型の変数
    bool isGameOver = false;
    State state;
    Operator op;
    int[] lastElementsArray; // サイズ9の配列を定義
    InputField inputPosField; //posの入力を格納するための変数
    InputField inputKomaField; //komaの入力を格納するための変数


    public class Banmen {
        // 初期の配列の設定
        List<List<int>> banmen = new List<List<int>>();
        
        //コンストラクタ
        public Banmen() {
            /*for (int i = 0; i < 9; i++) {
                banmen.Add(new List<int> { 0 });
            }*/
            banmen.Add(new List<int> { 0, 1, 2 });
            banmen.Add(new List<int> { 0});
            banmen.Add(new List<int> { 0, 1, 2 });
            banmen.Add(new List<int> { 0, 1, -2 });
            banmen.Add(new List<int> { 0, 1 });
            banmen.Add(new List<int> { 0, 1, -3 });
            banmen.Add(new List<int> { 0, 1, 3 });
            banmen.Add(new List<int> { 0, 3 });
            banmen.Add(new List<int> { 0, -1, -2 });

        }

        public List<List<int>> GetBanmen() {
            return banmen;
        }
    }

    public class Mochigoma {
        private int[] mochigoma;//手持ちの駒

        //コンストラクタ
        public Mochigoma(int koma1, int koma2, int koma3, int koma4, int koma5, int koma6) {
            this.mochigoma = new int[6];
            mochigoma[0] = koma1;
            mochigoma[1] = koma2;
            mochigoma[2] = koma3;
            mochigoma[3] = koma4;
            mochigoma[4] = koma5;
            mochigoma[5] = koma6;
        }
        public int[] GetMochigoma() {
            return mochigoma;
        }
    }

    public class State {
        public Banmen banmen;
        public Mochigoma sente;
        public Mochigoma gote;
        public int turn;//経過ターン数

        public State(Banmen banmen, Mochigoma sente, Mochigoma gote) {
            this.banmen = new Banmen();
            this.sente = new Mochigoma(3,3,2,2,1,1);
            this.gote = new Mochigoma(-3,-3,-2,-2,-1,-1);
            this.turn = 1;
        }
    }

    public class Operator {
        //uteru teban;  //駒を置けるところ
        List<int> availablePositonsList;  //駒を置けるところ
        public int pos;   // どこに
        public int koma;  // どのサイズの

        //コンストラクタ
        public Operator(List<int> availablePositonsList, int pos, int koma) {
            this.availablePositonsList = availablePositonsList;
            this.pos = pos;
            this.koma = koma;
        }

    }

    //駒を置ける場所のリストを計算する関数
    public List<int> GetAvailablePositonsList(State state) {
        int maxKomaSize = 0;//持ち駒の中で絶対値が最も大きい駒のサイズを格納するint型の変数
        
        List<int> availablePositonsList = new List<int>();

        //if(state.turn % 2 == 1) {
        //state.senteにある駒の絶対値が最も大きいものをmaxKomaSizeに格納
        foreach (int komaSize in state.sente.GetMochigoma()) {
            if (Math.Abs(komaSize) > maxKomaSize) {
                    maxKomaSize = Math.Abs(komaSize);
            }
        }

        for (int i = 0; i < state.banmen.GetBanmen().Count; i++) {
            if (state.banmen.GetBanmen()[i].Count > 0) {
                int lastElement = state.banmen.GetBanmen()[i][state.banmen.GetBanmen()[i].Count - 1];
                //lastElementを格納するためのサイズ9の配列を定義してlastElementを格納
                lastElementsArray[i] = lastElement; // 配列にlastElementを格納
                
                if (Math.Abs(lastElement) < maxKomaSize) {
                availablePositonsList.Add(i);
            }
        }
            else {
                Debug.Log($"state.banmen.GetBanmen()[{i}] は空です。");
            }
        }
        //state.turn++;

        //駒を置ける場所のリストを返す
        return availablePositonsList; 
    }




    void Start() {

        // Stateクラスのインスタンスを生成
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        lastElementsArray = new int[9];

        
        inputPosField = GameObject.Find("InputPosField").GetComponent<InputField>();
        inputKomaField = GameObject.Find("InputKomaField").GetComponent<InputField>();
    }

    void Update() {
        //どちらかのプレイヤーが勝利するまで繰り返すwhile文
        //while (!isGameOver) {
        /*while (true) {

            //入力を受け付ける
            Debug.Log("駒を置く場所を入力してください:");

        }    */ 

    }
    
    //駒を置く関数
    State Put(State state, Operator op) {
        //絶対値を比較して、op.komaがop.posに置けるかどうかを判定
        if(lastElementsArray[op.pos] < Math.Abs(op.koma)) {
            state.banmen.GetBanmen()[op.pos].Add(op.koma);
            lastElementsArray[op.pos] = op.koma;//更新
        }

        //持ち駒を更新
        if (state.turn % 2 == 1) {//先手の場合
            for (int i = 0; i < state.sente.GetMochigoma().Length; i++) {
                if (state.sente.GetMochigoma()[i] == op.koma) {
                    //
                    state.sente.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
        else {//後手の場合
            for (int i = 0; i < state.gote.GetMochigoma().Length; i++) {
                if (state.gote.GetMochigoma()[i] == op.koma) {
                    state.gote.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
        //ターンを進める
        state.turn++;
        return state;
    }

    public int GetInputField(InputField inputField) {
        //InputFieldからテキスト情報を取得する
        string inputText = inputField.text;
        //inputTextをint型に変換する
        int inputNumber = int.Parse(inputText);
        Debug.Log(inputNumber);
        //入力フォームのテキストを空にする
        inputField.text = "";

        return inputNumber;
    }

    //ボタンが押された時に呼び出される関数
    public void OnClickButton() {
        int inputPos = GetInputField(inputPosField);
        int inputKoma = GetInputField(inputKomaField);
        
        // 駒を置ける場所のリストを計算
        List<int> availablePositionsList = GetAvailablePositonsList(state);

        
        // Operatorクラスのインスタンスを生成
        op = new Operator(availablePositionsList, inputPos, inputKoma);
        //状態表現とオペレータを使って状態遷移関数を実行
        Put(state, op);
        
        for (int i = 0; i < state.banmen.GetBanmen().Count; i++) {
            Debug.Log($"state.banmen.GetBanmen()[{i}] の最後の要素: {lastElementsArray[i]}");
        }
        // 駒を置ける場所のリストを再計算して表示
        availablePositionsList = GetAvailablePositonsList(state);
        Debug.Log(string.Join(", ", availablePositionsList));
    }

}
