using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    bool isGameOver = false;// ゲームが終了したかどうかを表すbool型の変数
    State state;
    Operator op;
    int[] lastElementsArray; //盤面の各マスの最後の要素を格納する配列
    List<int> availablePositionsList; // 駒を置ける場所のリスト
    private GameObject selectedKoma;//Unityで選択された駒を格納するための変数
    public LayerMask komaLayer; // Koma用のレイヤーマスク
    public LayerMask positionLayer;  // Position用のレイヤーマスク
    private Vector3 originalPosition; // 選択された駒の移動前の位置情報を保持する変数
    public GameObject[] goteKomas; // 駒のプレハブを格納する配列

    // ゲームの結果を表す列挙型
    public enum GameResult {
        None,      // 誰も勝っていない
        SenteWin,  // 先手の勝ち
        GoteWin  // 後手の勝ち
    }

    void Start() {
        isGameOver = false;
        //Stateクラスのインスタンスを生成
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        lastElementsArray = new int[9];

        availablePositionsList = GetAvailablePositonsList(state);
    }

    void Update() {
        if (isGameOver) return;

        if (state.turn % 2 == 1) {
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
        } else {
            // 後手のターン（偶数ターン）
            HandleAITurn();
        }

        // 勝利判定
        if (CheckWinner(state) != GameResult.None) {
            isGameOver = true;
            Debug.Log("勝利判定: " + CheckWinner(state));
        }
    }

    void HandleAITurn() {
        // AIのターン処理
        Debug.Log("AIが駒を置こうとしています");
        GetAvailablePositonsList(state);
        //State newState = getNext(state, 2);
        Node newNode = getNext(state, 2);
        ApplyMove(newNode.state);
        UpdateMochigoma(state, newNode.op);

        //駒オブジェクトを移動させる処理を追加
        MoveAIPiece(newNode.op);

        Debug.Log("AIが駒を置きました");
        //PrintCurrentBanmen(state);
    }

    // ドラッグ開始時に駒を選択する関数
    void HandlePieceSelection() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 5.0f);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, komaLayer)) {
            selectedKoma = hit.collider.gameObject;
            // 選択された駒の現在の位置を保存
            originalPosition = selectedKoma.transform.position;
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

                // 選択した駒が盤面にあり、持ち駒から置ける場所がある場合
                if (komaPos != -1 && canPlaceFromMochigoma) {
                    Debug.Log("持ち駒から置ける場所があるため、盤面の駒は選択できません");
                    selectedKoma = null;
                }
                // 選択した駒が持ち駒で、持ち駒から置ける場所がない場合
                else if (komaPos == -1 && !canPlaceFromMochigoma) {
                    Debug.Log("持ち駒から置ける場所がないため、持ち駒は選択できません");
                    selectedKoma = null;
                }
                //選んだ駒が動かせる場合
                else {
                    //選択した駒が盤面の駒の時
                    if (komaPos != -1) {
                        Debug.Log("選択した駒を持ち駒に追加します");
                        //その時の手番のプレイヤーの持ち駒リストに加える
                        currentPlayerMochigoma.AddKoma(komaSize);
                        //移動元の位置のリストから最後尾の駒を削除
                        List<List<int>> banmen = state.banmen.GetBanmen();
                        banmen[komaPos].RemoveAt(banmen[komaPos].Count - 1);

                        //勝利判定
                        GameResult postMoveResult = CheckWinner(state);
                        if (postMoveResult != GameResult.None) {
                            Debug.Log($"勝利判定の結果: {postMoveResult}");
                            return; //終了
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
        mousePosition.z = 10.0f;
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
            //駒を置いて反映
            GetAvailablePositonsList(state);
            PlaceSelectedKomaOnPosition(hit);
            UpdateMochigoma(state, op);
            //state.NextTurn();
            Debug.Log("ターン数: " + state.turn);
        }
        selectedKoma = null;
    }

    //駒を置く関数
    void PlaceSelectedKomaOnPosition(RaycastHit hit) {

        int komaSize = selectedKoma.GetComponent<Koma>().size;
        int komaPos = selectedKoma.GetComponent<Koma>().pos;
        int positionNumber = hit.collider.gameObject.GetComponent<Position>().number;

        // 現在のターンに基づいて、先手または後手のプレイヤーの持ち駒を操作するための変数を定義
        Mochigoma currentPlayerMochigoma = state.turn % 2 == 1 ? state.sente : state.gote;

        //移動前と後が同じ位置なら、駒を元の位置に戻し、移動処理は行わない
        if (positionNumber == komaPos && komaPos != -1) {
            // 駒を元の位置に戻す処理
            ResetKomaPosition();
            Debug.Log("現在と同じ位置に駒を置くことはできません");
            currentPlayerMochigoma.RemoveKoma(komaSize);
            
            //持ち上げた駒はいったん削除されているので、元の位置に戻す処理を行う
            List<List<int>> banmen = state.banmen.GetBanmen();
            if (komaPos >= 0 && komaPos < banmen.Count) {
                if (banmen[komaPos].Count > 0) {
                    banmen[komaPos][banmen[komaPos].Count - 1] = komaSize;
                }
                else {
                    banmen[komaPos].Add(komaSize); // リストが空の場合、新しい要素を追加
                }
            }
            else {
                Debug.LogError("komaPos is out of range: " + komaPos);
            }
            return;
        }
        //移動先により大きい駒があるなら、駒を置かずに処理を終了
        if (Math.Abs(lastElementsArray[positionNumber]) >= Math.Abs(komaSize)) {
            //lastElementsArray[positionNumber]をログに出力
            Debug.Log("lastElementsArray[positionNumber]: " + lastElementsArray[positionNumber]);
            //選択した駒の元の位置(komaPos)に戻す処理
            ResetKomaPosition();
            Debug.Log("選択した駒より大きい駒の上に置くことはできません");
            currentPlayerMochigoma.RemoveKoma(komaSize);

            //持ち上げた駒はいったん削除されているので、元の位置に戻す処理を行う
            List<List<int>> banmen = state.banmen.GetBanmen();
            if (komaPos >= 0 && komaPos < banmen.Count) {
                if (banmen[komaPos].Count > 0) {
                    banmen[komaPos][banmen[komaPos].Count - 1] = komaSize;
                }
                else {
                    banmen[komaPos].Add(komaSize); // リストが空の場合、新しい要素を追加
                }
            }
            else {
                Debug.LogError("komaPos is out of range: " + komaPos);
            }
            return;
        }

        // 駒をマスの上に配置する処理（駒の底面がマスの上面に来るように調整）
        float komaHeight = selectedKoma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = hit.collider.transform.position;
        newPosition.y += komaHeight / 2;
        selectedKoma.transform.position = newPosition;

        Koma komaComponent = selectedKoma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // 駒が配置された新しい位置に更新

        selectedKoma = null;

        //positionNumberをログに出力
        Debug.Log($"positionNumber: {positionNumber}");
        //現在位置、移動先、駒のサイズを引数にOperatorクラスのインスタンスを生成
        op = new Operator(availablePositionsList, komaPos, positionNumber, komaSize);

        State newState = Put(state, op);
        ApplyMove(newState);
    }

    void MoveAIPiece(Operator op) {
        int komaSize = op.koma;
        int sourceNumber = op.sourcePos;
        int positionNumber = op.targetPos;

        // 位置の範囲チェック
        if (positionNumber < 0 || positionNumber >= 9) {
            Debug.LogError("positionNumber is out of range: " + positionNumber);
            return;
        }

        // 駒を取得
        GameObject koma = FindKoma(komaSize, sourceNumber);
        if (koma == null) {
            Debug.LogError("Koma not found.");
            return;
        }

        // マスのオブジェクトを検索
        GameObject positionObject = GameObject.Find("Position (" + positionNumber + ")");
        if (positionObject == null) {
            Debug.LogError("Position object not found: Position (" + positionNumber + ")");
            return;
        }

        // 駒をマスの上に配置する処理（駒の底面がマスの上面に来るように調整）
        float komaHeight = koma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = positionObject.transform.position;
        newPosition.y += komaHeight / 2;
        koma.transform.position = newPosition;

        Koma komaComponent = koma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // 駒が配置された新しい位置に更新

        // lastElementsArray の更新
        lastElementsArray[positionNumber] = komaSize;
        Debug.Log("Updated lastElementsArray[" + positionNumber + "]: " + lastElementsArray[positionNumber]);
    }

    GameObject FindKoma(int size, int sourcePos) {
        // goteKomas 配列内のすべての GameObject をチェック
        foreach (GameObject komaObject in goteKomas) {
            Koma koma = komaObject.GetComponent<Koma>();
            if (koma != null && koma.pos == sourcePos && koma.size == size && koma.player == -1) {
                return komaObject;
            }
        }

        // 条件に合うKomaが見つからなかった場合
        Debug.LogError("No matching Koma found.");
        return null;
    }

    // 駒を元の位置に戻す処理を共通化
    void ResetKomaPosition() {
        if (selectedKoma != null) {
            selectedKoma.transform.position = originalPosition;
            selectedKoma = null;
        }
    }

    //駒を置ける場所のリストを計算する関数
    public List<int> GetAvailablePositonsList(State state) {
        List<List<int>> banmen = state.banmen.GetBanmen();
        int maxKomaSize = 0;
        List<int> availablePositionsList = new List<int>();

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
                Debug.Log($"banmen[{i}] は空です。");
            }
        }

        // 先手または後手のターンに応じたメッセージを表示
        if (state.turn % 2 == 1) {
            //Debug.Log($"{state.turn}ターン目：先手の方は手を入力してください");
            Debug.Log("先手の持ち駒:" + string.Join(",", state.sente.GetMochigoma()));
        }
        else {
            //Debug.Log($"{state.turn}ターン目：後手の方は手を入力してください");
            Debug.Log("後手の持ち駒:" + string.Join(",", state.gote.GetMochigoma()));
        }
        // 駒を置ける場所のリストを表示
        //Debug.Log("置ける場所: " + string.Join(", ", availablePositionsList));

        return availablePositionsList;
    }


    //駒を置いた後の状態を生成する状態遷移関数
    State Put(State state, Operator op) {
        // オペレータが有効かどうかをチェック
        if (!IsValidMove(state, op)) {
            throw new InvalidOperationException("Invalid move");
        }
        State newState = new State(state);
        // オペレータに基づいて駒を置く処理を行う      
        List<List<int>> banmen = newState.banmen.GetBanmen();
        // 駒を置く処理
        banmen[op.targetPos].Add(op.koma);

        //laseElementsArrayも、グローバル変数じゃなくメンバー変数で持つ必要がある?
        //てかこれいる？
        lastElementsArray[op.targetPos] = op.koma;

        //ゲームの進行処理の中でAIの手が確定してから
        //Put→getNext→ApplyMove→UpdateMochigomaの順で行う？
        //いったんコメントアウト
        //UpdateMochigoma(state, op);

        newState.NextTurn();

        // 現在の盤面の状態をログに出力
        //PrintCurrentBanmen(state);

        return newState;
    }

    void ApplyMove(State newState) {
        // 現在の状態を新しい状態に更新
        this.state = newState;

        // 現在の盤面の状態をログに出力
        PrintCurrentBanmen(state);
    }

    //なんか後手の持ち駒-3が3つあるから初期化か置くときの更新がおかしい
    private void UpdateMochigoma(State state, Operator op) {
        if (state.turn % 2 == 1) { // 先手の場合
            for (int i = 0; i < state.sente.GetMochigoma().Count; i++) {
                if (state.sente.GetMochigoma()[i] == op.koma) {
                    state.sente.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
        else { // 後手の場合
            for (int i = 0; i < state.gote.GetMochigoma().Count; i++) {
                if (state.gote.GetMochigoma()[i] == op.koma) {
                    state.gote.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
    }

    //勝利判定を行う関数
    public GameResult CheckWinner(State state) {
        var (senteArray, goteArray) = CreateBinaryArrays(state);

        // 勝利条件のチェック
        if (HasWinningLine(senteArray)) {
            string banmenOutput = "";
            for (int row = 0; row < 3; row++) {
                for (int col = 0; col < 3; col++) {
                    banmenOutput += senteArray[row, col].ToString() + " ";
                }
                banmenOutput += "\n";
            }
            //探索の際のリーチ潰し判定にも使うので、いったんコメントアウト
            //Debug.Log($"先手の勝利盤面:\n{banmenOutput}");
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
            //探索の際のリーチ潰し判定にも使うので、いったんコメントアウト
            //Debug.Log($"後手の勝利盤面:\n{banmenOutput}");
            return GameResult.GoteWin;
        }

        return GameResult.None;
    }

    //ビンゴラインが揃っているかどうかを判定する関数
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

    //
    List<Operator> GetMochigomaOperators(State state) {
        List<Operator> operators = new List<Operator>();

        // 持ち駒のリストを取得
        List<int> mochigoma = state.gote.GetMochigoma();

        // 盤面の状態を取得
        List<List<int>> board = state.banmen.GetBanmen();

        // 持ち駒を空きマスまたは覆えるマスに置く操作を生成
        foreach (var koma in mochigoma) {
            for (int row = 0; row < 3; row++) {
                for (int col = 0; col < 3; col++) {
                    int targetPiece = board[row][col];
                    if (targetPiece == 0 || CanCoverPiece(koma, targetPiece)) {
                        operators.Add(new Operator(null, row * 3 + col, koma));
                    }
                }
            }
        }
        return operators;
    }

    bool CanCoverPiece(int piece, int targetPiece) {
        // 駒のサイズを比較して、覆えるかどうかを判定
        return Math.Abs(piece) > Math.Abs(targetPiece);
    }

    List<Operator> GetBoardOperators(State state) {
        List<Operator> operators = new List<Operator>();

        // 盤面の状態を取得
        List<List<int>> banmen = state.banmen.GetBanmen();

        // 盤面上の駒を動かす操作を生成
        for (int pos = 0; pos < banmen.Count; pos++) {
            List<int> stack = banmen[pos];
            if (stack.Count > 0) { // 駒が存在する場合
                int piece = stack[stack.Count - 1];
                List<int> possibleMoves = GetPossibleMovesForPiece(piece, pos, state);
                foreach (var targetPos in possibleMoves) {
                    operators.Add(new Operator(pos, targetPos, piece));
                }
            }
        }

        return operators;
    }

    List<int> GetPossibleMovesForPiece(int piece, int currentPos, State state) {
        List<int> possibleMoves = new List<int>();

        // 盤面の状態を取得
        List<List<int>> banmen = state.banmen.GetBanmen();

        // 盤内のすべての位置をチェック
        for (int newPos = 0; newPos < banmen.Count; newPos++) {
            // 現在の位置はスキップ
            if (newPos == currentPos) continue;

            List<int> targetStack = banmen[newPos];

            // 移動先が空きマスまたは覆える駒である場合
            if (targetStack.Count == 0 || CanCoverPiece(piece, targetStack[targetStack.Count - 1])) {
                possibleMoves.Add(newPos);
            }
        }

        return possibleMoves;
    }

    // オペレータが有効かどうかをチェックするメソッド
    public bool IsValidMove(State state, Operator op) {
        // nullチェックを追加
        if (state == null) {
            Debug.LogError("State is null");
            return false;
        }
        if (op == null) {
            Debug.LogError("Operator is null");
            return false;
        }

        // 盤面の状態を取得
        List<List<int>> banmen = state.banmen.GetBanmen();

        // pos<0またはpos>=9の場合、IndexOutOfRangeExceptionをスロー
        if (op.targetPos < 0 || op.targetPos >= banmen.Count) {
            //targetPosをログに出力
            Debug.Log("targetPos: " + op.targetPos);
            Debug.LogError("IndexOutOfRangeException: Index was outside the bounds of the array.");
            return false; // 範囲外の場合は、処理を中断してstateをそのまま返す
        }

        List<int> targetStack = banmen[op.targetPos];

        // 移動先が空きマスまたは覆える駒である場合
        if (targetStack.Count == 0 || CanCoverPiece(op.koma, targetStack[targetStack.Count - 1])) {
            Debug.Log("Valid move: targetStack = " + (targetStack.Count == 0 ? "empty" : targetStack[targetStack.Count - 1].ToString()) + ", op.koma = " + op.koma);
            return true;
        }
        else {
            Debug.Log("Invalid move: targetStack = " + targetStack[targetStack.Count - 1] + ", op.koma = " + op.koma);
            return false;
        }
    }

    //AIがリーチの数を計算する関数
    public int CountReach(State currentState) {
        int reachCount = 0;

        // その状態のgoteArrayを取得
        var (_, goteArray) = CreateBinaryArrays(currentState);

        // goteArrayのリーチの数を計算
        // 縦、横のリーチをチェック
        for (int i = 0; i < 3; i++) {
            int[,] horizontalPositions = { { i, 0 }, { i, 1 }, { i, 2 } };
            int[,] verticalPositions = { { 0, i }, { 1, i }, { 2, i } };

            if (IsReach(goteArray[i, 0], goteArray[i, 1], goteArray[i, 2], currentState, horizontalPositions) ||
                IsReach(goteArray[0, i], goteArray[1, i], goteArray[2, i], currentState, verticalPositions)) {
                reachCount++;
            }
        }

        // 斜めのリーチをチェック
        int[,] diagonalPositions1 = { { 0, 0 }, { 1, 1 }, { 2, 2 } };
        int[,] diagonalPositions2 = { { 0, 2 }, { 1, 1 }, { 2, 0 } };

        if (IsReach(goteArray[0, 0], goteArray[1, 1], goteArray[2, 2], currentState, diagonalPositions1) ||
            IsReach(goteArray[0, 2], goteArray[1, 1], goteArray[2, 0], currentState, diagonalPositions2)) {
            reachCount++;
        }

        // その状態で出来ている敵AIのリーチの数を計算
        return reachCount;
    }

    //3つのうち2つが後手の駒（1）で、残り1つが空き（0）または先手の駒（3未満）である場合をリーチとみなします
    public bool IsReach(int a, int b, int c, State state, int[,] positions) {
        // 3つの位置のうち2つが後手の駒で、残り1つが空きまたは先手の駒（3未満）である場合をリーチとみなす
        int goteCount = 0;
        int emptyOrSenteCount = 0;

        List<List<int>> banmen = state.banmen.GetBanmen();

        for (int i = 0; i < 3; i++) {
            int row = positions[i, 0];
            int col = positions[i, 1];
            int pos = (row == 0 && col == 0) ? a : (row == 1 && col == 1) ? b : c;

            if (pos == 1) {
                goteCount++;
            }
            else if (pos == 0 || (pos == 1 && banmen[row][col] < 3)) {
                emptyOrSenteCount++;
            }
        }

        return goteCount == 2 && emptyOrSenteCount == 1;
    }

    private (int[,], int[,]) CreateBinaryArrays(State state) {
        int[,] senteArray = new int[3, 3];
        int[,] goteArray = new int[3, 3];
        List<List<int>> banmen = state.banmen.GetBanmen();

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

    //AIがその手がプレイヤーの駒の上から被せる手かどうかを判定する関数
    public bool CheckCoveringMove(State currentState, Operator op) {
        //currentStateの盤面の状態を一時変数に格納
        List<List<int>> banmen = currentState.banmen.GetBanmen();
        //op.targetPosのリストを取得
        List<int> targetPosKomas = banmen[op.targetPos];
        //op.targetPosのリストの最後の要素を取得
        int lastElement = targetPosKomas.Count > 0 ? targetPosKomas[targetPosKomas.Count - 1] : 0;
        //op.komaとlastElementの絶対値を比較して、op.komaがlastElementに被せることができるかどうかを判定
        return Math.Abs(op.koma) > Math.Abs(lastElement);
    }

    //AIがその手がプレイヤーのリーチを潰した数を計算する関数
    public int CountBlockedReaches(State parentState, State currentState) {
        // parentStateのリーチ数をカウント
        int parentReachCount = CountReach(parentState);
        // currentStateのリーチ数をカウント
        int currentReachCount = CountReach(currentState);
        // 潰せたリーチの数を計算
        int blockedReaches = parentReachCount - currentReachCount;
        return blockedReaches;
    }


    Node getNext(State state, int depth) {
        // ルートノードを現在の状態で初期化
        Node root = new Node(state, null, null);
        // 偶数ターンはAIプレイヤー
        bool isMaximizingPlayer = state.turn % 2 == 0;
        // ミニマックスアルゴリズムを使用して最適な手を探索
        Minimax(root, depth, isMaximizingPlayer);
        Node bestMove = null;
        int bestEval = int.MinValue; // 最適な評価値を初期化

        // 子ノードをすべて調べて最適な手を見つける
        foreach (Node child in root.children) {
            if (child.eval > bestEval) {
                bestEval = child.eval;
                bestMove = child;
            }
        }

        // 最適な手が見つかった場合、そのノードを返す
        return bestMove != null ? bestMove : root;
    }

    int Minimax(Node node, int depth, bool isMaximizingPlayer) {
        // 探索の深さが0またはゲームが終了している場合、評価値を返す
        if (depth == 0 || IsGameOver(node.state)) {
            node.eval = EvaluateState(node); // そのノードの評価値を評価関数から計算
            return node.eval;
        }

        // 敵AIが得点最大化プレイヤーの場合
        if (isMaximizingPlayer) {
            int maxEval = int.MinValue;
            // すべての可能な次の状態を生成
            foreach ((State childState, Operator op) in GetPossibleMoves(node.state)) {
                // 子ノードを生成し、親ノードのstateとオペレータを渡す
                Node childNode = new Node(childState, node.state, op);
                // 親ノードのchildrenリストに子ノードを追加
                node.children.Add(childNode);
                // 再帰的にMinimaxを呼び出し、評価値を計算
                int eval = Minimax(childNode, depth - 1, false);
                // より大きい方をmaxEvalとし、最大評価値を更新
                maxEval = Math.Max(maxEval, eval);
            }
            node.eval = maxEval;
            return maxEval;
        }
        // 敵AIが得点最小化プレイヤーの場合
        else {
            int minEval = int.MaxValue;
            // すべての可能な次の状態を生成
            foreach ((State childState, Operator op) in GetPossibleMoves(node.state)) {
                // 子ノードを生成し、親ノードのstateとオペレータを渡す
                Node childNode = new Node(childState, node.state, op);
                // 親ノードのchildrenリストに子ノードを追加
                node.children.Add(childNode);
                // 再帰的にMinimaxを呼び出し、評価値を計算
                int eval = Minimax(childNode, depth - 1, true);
                // より小さい方をminEvalとし、最小評価値を更新
                minEval = Math.Min(minEval, eval);
            }
            node.eval = minEval;
            return minEval;
        }
    }

    // ゲームが終了しているかどうかを判定する関数
    bool IsGameOver(State state) {
        return CheckWinner(state) != GameResult.None;
    }

    // 現在の状態から可能なすべての手を生成する関数
    List<(State, Operator)> GetPossibleMoves(State state) {
        List<(State, Operator)> possibleMoves = new List<(State, Operator)>();

        // 置ける場所のリストを取得
        List<int> availablePositions = GetAvailablePositonsList(state);
        // 手持ちの駒を取得
        List<int> mochigomas = state.gote.GetMochigoma();

        // 置ける場所と手持ちの駒を組み合わせて可能な手を生成
        foreach (int pos in availablePositions) {
            foreach (int piece in mochigomas) {
                Operator op = new Operator(new List<int>(), pos, piece);//List, targetPos, koma
                if (IsValidMove(state, op)){
                    // 新しい状態を生成
                    State newState = Put(state, op);
                    possibleMoves.Add((newState, op));
                }
            }
        }
        return possibleMoves;
    }

    // 評価関数
    int EvaluateState(Node node) {
        State currentState = node.state; // 現在の状態を取得
        State parentState = node.parentState; // 親ノードの状態を取得
        int evaluation = 0;

        GameResult result = CheckWinner(currentState);
        //後手のビンゴラインが揃っている場合は1000点を加算
        if (result == GameResult.GoteWin) {
            evaluation += 1000;
        }
        //先手のビンゴラインが揃っている場合は-10000点を加算
        if(result == GameResult.SenteWin) {
            evaluation -= 10000;
        }
        //op.komaが相手の上に被せている場合、+150点
        if (CheckCoveringMove(currentState, node.op)) {
            evaluation += 150;
        }
        // 潰せたリーチの数をカウント
        int blockedReaches = CountBlockedReaches(parentState, currentState);
        evaluation += blockedReaches * 150;
        // currentStateにおける敵AIのリーチ数×25点evaluationに加算
        int reaches = CountReach(currentState);
        evaluation += reaches * 25;

        return evaluation;
    }



}
