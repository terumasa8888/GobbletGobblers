using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static NewBehaviourScript;

public class NewBehaviourScript : MonoBehaviour
{
    // ゲームが終了したかどうかを表すbool型の変数
    bool isGameOver = false;
    State state;
    Operator op;
    int[] lastElementsArray; // サイズ9の配列を定義
    InputField inputSourcePosField; //sourcePosの入力を格納するための変数
    InputField inputTargetPosField; //targetPosの入力を格納するための変数
    InputField inputKomaField; //komaの入力を格納するための変数
    List<int> availablePositionsList; // 駒を置ける場所のリスト

    //Unityで駒を格納するための変数


    public enum GameResult {
        None,      // 誰も勝っていない
        SenteWin,  // 先手の勝ち
        GoteWin  // 後手の勝ち
    }

    public class Banmen {
        // 初期の配列の設定
        List<List<int>> banmen = new List<List<int>>();
        
        //コンストラクタ
        public Banmen() {
            for (int i = 0; i < 9; i++) {
                banmen.Add(new List<int> { 0 });
            }
        }

        public List<List<int>> GetBanmen() {
            return banmen;
        }
    }

    public class Mochigoma {
        //名前mochigomaListにするべき？？
        private List<int> mochigoma;//手持ちの駒
        //コンストラクタ
        public Mochigoma(int koma1, int koma2, int koma3, int koma4, int koma5, int koma6) {
            this.mochigoma = new List<int> { koma1, koma2, koma3, koma4, koma5, koma6 };
        }
        //ゲッター
        public List<int> GetMochigoma() {
            return mochigoma;
        }
        // リストに駒を追加するメソッド（必要に応じて追加）
        public void AddKoma(int koma) {
            mochigoma.Add(koma);
        }

        // リストから駒を削除するメソッド（必要に応じて追加）
        public bool RemoveKoma(int koma) {
            return mochigoma.Remove(koma);
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

        //turnをプラス1するメソッド
        public void NextTurn() {
            turn++;
        }
    }

    public class Operator {
        List<int> availablePositonsList;  //駒を置けるところ
        public int sourcePos;//どこから
        public int targetPos;// どこへ
        public int koma;//どのサイズの

        //持ち駒から置けるところがある場合のコンストラクタ
        public Operator(List<int> availablePositonsList, int targetPos, int koma) {
            this.availablePositonsList = availablePositonsList;
            this.targetPos = targetPos;
            this.koma = koma;
        }

        //持ち駒から置けるところがない場合のコンストラクタ
        public Operator(List<int> availablePositonsList, int sourcePos, int targetPos, int koma) {
            this.availablePositonsList = availablePositonsList;
            this.sourcePos = sourcePos;
            this.targetPos = targetPos;
            this.koma = koma;
        }
    }

    //駒を置ける場所のリストを計算する関数
    public List<int> GetAvailablePositonsList(State state) {
        // 盤面の状態を一時変数に格納
        List<List<int>> banmen = state.banmen.GetBanmen();
        //持ち駒の中で絶対値が最も大きい駒のサイズを格納するint型の変数
        int maxKomaSize = 0;
        List<int> availablePositionsList = new List<int>();

        // 現在のターンに基づいて、先手または後手の持ち駒を取得
        List<int> currentMochigoma = state.turn % 2 == 1 ? state.sente.GetMochigoma() : state.gote.GetMochigoma();
        //現在の持ち駒の中で絶対値が最も大きいもののサイズをmaxKomaSizeに格納
        foreach (int komaSize in currentMochigoma) {
            if (Math.Abs(komaSize) > maxKomaSize) {
                maxKomaSize = Math.Abs(komaSize);
            }
        }
        //各マスの最後の要素を取得し、lastElementsArrayに格納
        for (int i = 0; i < banmen.Count; i++) {
            if (banmen[i].Count > 0) {
                int lastElement = banmen[i][banmen[i].Count - 1];
                lastElementsArray[i] = lastElement;

                if (Math.Abs(lastElement) < maxKomaSize) {
                    availablePositionsList.Add(i);
                }
            }
            else {
                Debug.Log($"banmen[{i}] は空です。");//これが表示されることはない
            }
        }

        // 先手または後手のターンに応じたメッセージを表示
        if (state.turn % 2 == 1) {
            Debug.Log($"{state.turn}ターン目：先手の方は手を入力してください");
            Debug.Log("先手の持ち駒:" + string.Join(",", state.sente.GetMochigoma()));
        }
        else {
            Debug.Log($"{state.turn}ターン目：後手の方は手を入力してください");
            Debug.Log("後手の持ち駒:" + string.Join(",", state.gote.GetMochigoma()));
        }
        // 駒を置ける場所のリストを表示
        Debug.Log("置ける場所: " + string.Join(", ", availablePositionsList));

        return availablePositionsList;
    }




    void Start() {

        //Stateクラスのインスタンスを生成
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        //lastElementを格納するためのサイズ9の配列を定義
        lastElementsArray = new int[9];
        
        inputSourcePosField = GameObject.Find("InputSourcePosField").GetComponent<InputField>();
        inputTargetPosField = GameObject.Find("InputTargetPosField").GetComponent<InputField>();
        inputKomaField = GameObject.Find("InputKomaField").GetComponent<InputField>();

        availablePositionsList = GetAvailablePositonsList(state);

    }

    void Update() {

    }
    
    //駒を置く関数
    State Put(State state, Operator op) {

        //絶対値を比較して、op.komaがop.targetPosに置けるかどうかを判定
        if(lastElementsArray[op.targetPos] < Math.Abs(op.koma)) {
            // 盤面の状態を取得
            List<List<int>> banmen = state.banmen.GetBanmen();
            // 目標位置の駒リストを取得
            List<int> targetPosKomas = banmen[op.targetPos];
            // 目標位置に駒を置く
            targetPosKomas.Add(op.koma);
            //lastElementsArrayに加える
            lastElementsArray[op.targetPos] = op.koma;//更新

            //持ち駒の更新
            if (state.turn % 2 == 1) {//先手の場合
                for (int i = 0; i < state.sente.GetMochigoma().Count; i++) {
                    if (state.sente.GetMochigoma()[i] == op.koma) {
                        //
                        state.sente.GetMochigoma()[i] = 0;
                        break;
                    }
                }
            }
            else {//後手の場合
                for (int i = 0; i < state.gote.GetMochigoma().Count; i++) {
                    if (state.gote.GetMochigoma()[i] == op.koma) {
                        state.gote.GetMochigoma()[i] = 0;
                        break;
                    }
                }
            }
            //ターンを進める
            state.NextTurn();
        }
        else {
            Debug.Log("その駒は置けません");
        }
        return state;
    }

    //入力フォームからテキスト情報を取得する関数
    public int GetInputField(InputField inputField) {
        // InputFieldからテキスト情報を取得し、入力の前後の空白を削除
        string inputText = inputField.text.Trim(); 

        // 入力が空の場合の処理
        if (string.IsNullOrEmpty(inputText)) {
            Debug.LogError("入力が空です。値を入力してください。");
            return 0; // または適切なエラー値
        }

        // inputTextをint型に安全に変換する試み
        if (int.TryParse(inputText, out int inputNumber)) {
            inputField.text = "";
            return inputNumber;
        }
        else {
            // 変換に失敗した場合の処理（エラーメッセージの表示など）
            Debug.LogError("入力された値が整数として解釈できません: " + inputText);
            return 0; // または適切なエラー値
        }
    }


    //ボタンが押された時に呼び出される関数
    public void OnClickButton() {
        // 駒を置ける場所のリストを計算

        //int inputSourcePos;

        // 入力フォームからtargetPosとkomaを取得
        int inputTargetPos = GetInputField(inputTargetPosField);
        int inputKoma = GetInputField(inputKomaField);

        // 現在のターンに基づいて、先手または後手の持ち駒を取得
        List<int> currentMochigoma = state.turn % 2 == 1 ? state.sente.GetMochigoma() : state.gote.GetMochigoma();

        // currentMochigomaの中で、盤面に置ける場所があるかどうかをbool型で判定する処理
        //持ち駒リストの最小の絶対値が、盤面の最小の絶対値以下の場合は盤面から動かすしかない
        // 盤面から動かす場合、駒を持ち上げた段階で勝利判定を挟む。このとき持ち駒なし、もしくは、、

        if (currentMochigoma.Count == 0 ) {
            int inputSourcePos = GetInputField(inputSourcePosField);
            op = new Operator(availablePositionsList, inputSourcePos, inputTargetPos, inputKoma);
            
            // 盤面から動かす予定の駒を持ち駒リストに加える
            if (state.turn % 2 == 1) { // 先手の場合
                state.sente.AddKoma(inputKoma);
            }
            else { // 後手の場合
                state.gote.AddKoma(inputKoma);
            }

            // 駒を持ち上げる前の勝利判定
            GameResult preMoveResult = CheckWinner(state);
            if (preMoveResult != GameResult.None) {
                Debug.Log($"勝利判定の結果: {preMoveResult}");
                return; // 勝敗が決まった場合はここで処理を終了
            }

            // 状態表現stateとオペレータopを使って状態遷移関数Putを実行
            Put(state, op);
        }
        else {
            // 持ち駒から置ける場合
            op = new Operator(availablePositionsList, inputTargetPos, inputKoma);
            Put(state, op);
        }

        // 駒を置いた後の勝利判定
        GameResult postMoveResult = CheckWinner(state);
        if (postMoveResult != GameResult.None) {
            Debug.Log($"勝利判定の結果: {postMoveResult}");
            return; // 勝敗が決まった場合はここで処理を終了
        }

        /*
        //持ち駒から置けるところがある場合
        if (currentMochigoma.Count != 0) {
            op = new Operator(availablePositionsList, inputTargetPos, inputKoma);
        }
        //持ち駒から置けるところがない場合(つまり盤面から動かす場合)
        else {
            //InputSourcePosをactiveにする

            int inputSourcePos = GetInputField(inputSourcePosField);
            op = new Operator(availablePositionsList, inputSourcePos, inputTargetPos, inputKoma);

            // 盤面から動かす予定の駒を持ち駒リストに加える
            if (state.turn % 2 == 1) { // 先手の場合
                state.sente.AddKoma(inputKoma);
            }
            else { // 後手の場合
                state.gote.AddKoma(inputKoma);
            }
            //InputSourcePosをinactiveにする

            // 現在の盤面の状態をログに出力
            PrintCurrentBanmen(state);

            // 駒を置いた後の勝利判定
            GameResult postMoveResult = CheckWinner(state);
            if (postMoveResult != GameResult.None) {
                Debug.Log($"勝利判定の結果: {postMoveResult}");
                return; // 勝敗が決まった場合はここで処理を終了
            }
        }*/

        // 駒を置ける場所のリストを再計算して表示
        availablePositionsList = GetAvailablePositonsList(state);
        //Debug.Log("再計算された置ける場所: " + string.Join(", ", availablePositionsList));
    }

    //勝利判定を行う関数
    public GameResult CheckWinner(State state) {
        int[,] senteArray = new int[3, 3];
        int[,] goteArray = new int[3, 3];
        // 盤面の状態を一時変数に格納
        List<List<int>> banmen = state.banmen.GetBanmen();

        // 盤面の状態を解析してsenteArrayとgoteArrayを更新
        for (int i = 0; i < banmen.Count; i++) {
            //banmen[i][banmen[i].Count - 1](各マスの最後の要素)が0でないならlastElementにそのまま代入、0なら0を代入
            int lastElement = banmen[i][banmen[i].Count - 1] != 0 ? banmen[i][banmen[i].Count - 1] : 0;

            if (lastElement > 0) {
                senteArray[i / 3, i % 3] = 1;
            }
            else if (lastElement < 0) {
                goteArray[i / 3, i % 3] = 1;
            }
        }

        // 勝利条件のチェック
        if (HasWinningLine(senteArray)) {
            string banmenOutput = "";
            for (int row = 0; row < 3; row++) {
                for (int col = 0; col < 3; col++) {
                    banmenOutput += senteArray[row, col].ToString() + " ";
                }
                banmenOutput += "\n";
            }
            Debug.Log($"勝利盤面:\n{banmenOutput}");
            return GameResult.SenteWin;
        }
        else if (HasWinningLine(goteArray)) {
            string banmenOutput = "";
            for (int row = 0; row < 3; row++) {
                for (int col = 0; col < 3; col++) {
                    banmenOutput += goteArray[row, col].ToString() + " ";
                }
                banmenOutput += "\n";
            }
            return GameResult.GoteWin;
        }

        return GameResult.None;
    }

    //
    private bool HasWinningLine(int[,] array) {
        // 縦、横の勝利条件をチェック
        for (int i = 0; i < 3; i++) {
            if ((array[i, 0] == 1 && array[i, 1] == 1 && array[i, 2] == 1) ||
                (array[0, i] == 1 && array[1, i] == 1 && array[2, i] == 1)) {
                return true;
            }
        }

        // 斜めの勝利条件をチェック
        if ((array[0, 0] == 1 && array[1, 1] == 1 && array[2, 2] == 1) ||
            (array[0, 2] == 1 && array[1, 1] == 1 && array[2, 0] == 1)) {
            return true;
        }
        return false;
    }

    // 現在の盤面の状態を3×3の二次元配列に変換し、ログに出力する関数
    void PrintCurrentBanmen(State state) {
        int[,] currentBanmen = new int[3, 3];
        // 盤面の状態を一時変数に格納
        List<List<int>> banmen = state.banmen.GetBanmen();

        for (int i = 0; i < banmen.Count; i++) {
            int row = i / 3;
            int col = i % 3;
            List<int> stack = banmen[i];
            // リストの最後の要素をとってきてcurrentBanmen[row, col]に代入。空の場合は0を代入
            currentBanmen[row, col] = stack.Count > 0 ? stack[stack.Count - 1] : 0;
        }

        // 変換した盤面の状態を3×3の形でログに出力
        string banmenOutput = "";
        for (int row = 0; row < 3; row++) {
            for (int col = 0; col < 3; col++) {
                banmenOutput += currentBanmen[row, col].ToString() + " ";
            }
            banmenOutput += "\n";
        }
        Debug.Log($"現在の盤面:\n{banmenOutput}");
    }

    // currentMochigomaの中で、盤面に置ける場所があるかどうかをbool型で判定する処理
    bool CanPlaceFromMochigoma(List<int> currentMochigoma, List<int> availablePositionsList) {
        // 盤面に置ける場所がない、または持ち駒がない場合、falseを返す
        if (availablePositionsList.Count == 0 || currentMochigoma.Count == 0) {
            return false;
        }

        // 持ち駒の中で、盤面に置ける駒があるかどうかをチェック
        foreach (int koma in currentMochigoma) {
            if (koma != 0) { // 持ち駒リストに駒が存在する（0以外）
                             // 盤面に置ける場所がある場合、trueを返す
                return true;
            }
        }

        // 持ち駒の中に盤面に置ける駒がない場合、falseを返す
        return false;
    }


    //盤面を更新する関数

}
