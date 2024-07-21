using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    bool isGameOver = false;// ゲームが終了したかどうかを表すbool型の変数
    State state;
    Operator op;
    int[] lastElementsArray; // サイズ9の配列を定義
    List<int> availablePositionsList; // 駒を置ける場所のリスト
    private GameObject selectedKoma;//Unityで選択された駒を格納するための変数
    public LayerMask komaLayer; // Koma用のレイヤーマスク
    public LayerMask positionLayer;  // Position用のレイヤーマスク

    // ゲームの結果を表す列挙型
    public enum GameResult {
        None,      // 誰も勝っていない
        SenteWin,  // 先手の勝ち
        GoteWin  // 後手の勝ち
    }

    void Start() {

        //Stateクラスのインスタンスを生成
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        //lastElementを格納するためのサイズ9の配列を定義
        lastElementsArray = new int[9];

        availablePositionsList = GetAvailablePositonsList(state);

    }

    void Update() {
        // ドラッグ開始
        if (Input.GetMouseButtonDown(0)) {
            HandlePieceSelection();
        }
        // ドラッグ中に駒をマウスに追従させる
        if (Input.GetMouseButton(0) && selectedKoma != null) {
            FollowCursor();
        }
        // ドロップ（マウスを離した時）
        if (Input.GetMouseButtonUp(0) && selectedKoma != null) {
            HandlePieceDrop();
        }
    }


    // ドラッグ開始時に駒を選択する関数
    void HandlePieceSelection() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 5.0f);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, komaLayer)) {
            selectedKoma = hit.collider.gameObject;
            Koma selectedKomaComponent = selectedKoma.GetComponent<Koma>();
            int currentPlayer = state.turn % 2 == 1 ? 1 : -1;
            // 現在のターンに基づいて、先手または後手のプレイヤーの持ち駒を操作するための変数を定義
            Mochigoma currentPlayerMochigoma = state.turn % 2 == 1 ? state.sente : state.gote;
            int komaSize = 0;
            int komaPos = -1;

            if (selectedKomaComponent.player == currentPlayer) {
                // 駒のサイズ情報と位置情報を取得
                komaSize = selectedKoma.GetComponent<Koma>().size;
                komaPos = selectedKoma.GetComponent<Koma>().pos;

                // 持ち駒から置ける場所があるかどうかを判定
                bool canPlaceFromMochigoma = availablePositionsList.Count > 0;

                //この条件はAIの実装時にも使える
                // 選択した駒が盤面にあり、持ち駒から置ける場所がある場合、盤面の駒は選択できない
                if (komaPos != -1 && canPlaceFromMochigoma) {
                    Debug.Log("持ち駒から置ける場所があるため、盤面の駒は選択できません");
                    selectedKoma = null;
                }
                // 選択した駒が持ち駒で、持ち駒から置ける場所がない場合、持ち駒は選択できない
                else if (komaPos == -1 && !canPlaceFromMochigoma) {
                    Debug.Log("持ち駒から置ける場所がないため、持ち駒は選択できません");
                    selectedKoma = null;
                }
                else {//選んだ駒が動かせる場合
                    //選択した駒が盤面の駒の時、その時の手番のプレイヤーの持ち駒リストに加える
                    if (komaPos != -1) {
                        currentPlayerMochigoma.AddKoma(komaSize);
                        //移動元の位置のリストから最後尾の駒を削除
                        List<List<int>> banmen = state.banmen.GetBanmen();
                        banmen[komaPos][banmen[komaPos].Count - 1] = 0;
                        //盤面更新

                        //勝利判定
                        GameResult postMoveResult = CheckWinner(state);
                        if (postMoveResult != GameResult.None) {
                            Debug.Log($"勝利判定の結果: {postMoveResult}");
                            return; // 勝敗が決まった場合はここで処理を終了
                        }
                    }
                }
            }
            else {
                Debug.Log("この駒は現在のプレイヤーのものではありません");
                selectedKoma = null;
            }
        }
    }

    // マウスカーソルの位置に駒を追従させる関数
    void FollowCursor() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // マウスカーソルの位置に駒を追従させるために、カメラからの固定距離を保つ
        Vector3 mousePosition = Input.mousePosition;
        // カメラからの固定距離をスクリーン座標でのZ値として設定
        mousePosition.z = 10.0f; // 例: カメラから10ユニットの距離
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        // CapsuleColliderを取得し、駒の高さを取得する
        CapsuleCollider komaCapsuleCollider = selectedKoma.GetComponent<CapsuleCollider>();
        if (komaCapsuleCollider != null) {
            float komaHeight = komaCapsuleCollider.height;
            // CapsuleColliderの中心から底部までの距離を考慮して位置を調整
            newPosition.y += komaHeight * selectedKoma.transform.localScale.y / 2.0f;
        }
        else {
            newPosition.y += 0.5f; // CapsuleColliderがない場合はデフォルトのオフセットを使用
        }
        selectedKoma.transform.position = newPosition;
    }

    // マウスを離した時に駒を置く関数
    void HandlePieceDrop() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.blue, 5.0f);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, positionLayer)) {
            PlaceSelectedKomaOnPosition(hit);
        }
        selectedKoma = null; // 選択を解除
    }

    //駒を置く関数
    void PlaceSelectedKomaOnPosition(RaycastHit hit) {

        int komaSize = selectedKoma.GetComponent<Koma>().size;
        int komaPos = selectedKoma.GetComponent<Koma>().pos;
        int positionNumber = hit.collider.gameObject.GetComponent<Position>().number;
        Debug.Log($"選択されたマスの番号: {positionNumber}");

        // 駒をマスの上に配置する処理（駒の底面がマスの上面に来るように調整）
        float komaHeight = selectedKoma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = hit.collider.transform.position;
        newPosition.y += komaHeight / 2;
        selectedKoma.transform.position = newPosition;

        // Komaコンポーネントのposプロパティを更新
        Koma komaComponent = selectedKoma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // 駒が配置された新しい位置に更新

        selectedKoma = null;

        // Operatorクラスのインスタンスを生成
        op = new Operator(availablePositionsList, komaPos, positionNumber, komaSize);
        // stateを更新する処理
        Put(state, op);
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



    //駒を置く状態遷移関数
    State Put(State state, Operator op) {
        // op.targetPosがlastElementsArrayの範囲内にあるかどうかを確認
        if (op.targetPos < 0 || op.targetPos >= lastElementsArray.Length) {
            Debug.LogError("IndexOutOfRangeException: Index was outside the bounds of the array.");
            //op.targetPosを表示
            Debug.Log($"op.targetPos: {op.targetPos}");
            return state; // 範囲外の場合は、処理を中断してstateをそのまま返す
        }

        //絶対値を比較して、op.komaがop.targetPosに置けるかどうかを判定
        if (lastElementsArray[op.targetPos] < Math.Abs(op.koma)) {
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
        // 現在の盤面の状態をログに出力
        PrintCurrentBanmen(state);

        // 駒を置いた後の勝利判定
        GameResult postMoveResult = CheckWinner(state);
        if (postMoveResult != GameResult.None) {
            Debug.Log($"勝利判定の結果: {postMoveResult}");
            return state; // 勝敗が決まった場合はここで処理を終了
        }

        // 駒を置ける場所のリストを再計算して表示
        availablePositionsList = GetAvailablePositonsList(state);
        return state;
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
            Debug.Log($"先手の勝利盤面:\n{banmenOutput}");
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
            Debug.Log($"後手の勝利盤面:\n{banmenOutput}");
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

    //盤面を更新する関数

}
