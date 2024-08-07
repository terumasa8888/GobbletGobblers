using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    bool isGameOver;// ゲームが終了したかどうかを表すbool型の変数
    State state;
    Operator op;
    int[] lastElementsArray; //盤面の各マスの最後の要素を格納する配列
    List<int> availablePositionsList; // 駒を置ける場所のリスト
    private GameObject selectedKoma;//Unityで選択された駒を格納するための変数
    public LayerMask komaLayer; // Koma用のレイヤーマスク
    public LayerMask positionLayer;  // Position用のレイヤーマスク
    private Vector3 originalPosition;
    public GameObject[] goteKomas;
    public Text resultText;

    // ゲームの結果を表す列挙型
    public enum GameResult {
        None,      // 誰も勝っていない
        SenteWin,  // 先手の勝ち
        GoteWin  // 後手の勝ち
    }

    void Start() {
        isGameOver = false;
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        lastElementsArray = new int[9];

        availablePositionsList = GetAvailablePositonsList(state);
        // goteKomas 配列の初期化確認
        if (goteKomas == null || goteKomas.Length == 0) {
            Debug.LogError("goteKomas array is not initialized or empty.");
        }
        else {
            Debug.Log($"goteKomas array initialized with {goteKomas.Length} elements.");
        }
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
    }

    void HandleAITurn() {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        GetAvailablePositonsList(state);
        Node newNode = getNext(state, 3);

        if (newNode != null && newNode.op != null) {
            MoveAIPiece(newNode);
        }
        else {
            Debug.LogError("HandleAITurn: bestMove or bestMove.op is null");
        }

        //EvaluateStateWithDebug(newNode);//デバッグ用
        Debug.Log("評価値: " + newNode.eval);
        ApplyMove(newNode.state);
        UpdateMochigoma(state, newNode.op);
        PrintCurrentBanmen(state);

        stopwatch.Stop();
        Debug.Log($"HandleAITurn 実行時間: {stopwatch.ElapsedMilliseconds} ms");
        //勝利判定
        GameResult postMoveResult = CheckWinner(state);
        if (postMoveResult != GameResult.None) {
            Debug.Log($"勝利判定: {postMoveResult}");
            resultText.text = postMoveResult.ToString();
            PrintCurrentBanmen(state);
            isGameOver = true;
        }
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

                //置けるところリストの更新
                availablePositionsList = GetAvailablePositonsList(state);
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
                            Debug.Log($"勝利判定: {postMoveResult}");
                            resultText.text = postMoveResult.ToString();
                            PrintCurrentBanmen(state);
                            isGameOver = true;
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

        Vector3 mousePosition = Input.mousePosition;
        // カメラからの固定距離をスクリーン座標でのZ値として設定
        mousePosition.z = 10.0f;
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        CapsuleCollider komaCapsuleCollider = selectedKoma.GetComponent<CapsuleCollider>();
        if (komaCapsuleCollider != null) {
            float komaHeight = komaCapsuleCollider.height;
            newPosition.y += komaHeight * selectedKoma.transform.localScale.y / 2.0f;
        }
        else {
            newPosition.y += 0.5f;
        }
        selectedKoma.transform.position = newPosition;
    }

    // マウスを離した時に駒を置く関数
    void HandlePieceDrop() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.blue, 5.0f);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, positionLayer)) {
            GetAvailablePositonsList(state);
            PlaceSelectedKomaOnPosition(hit);
            UpdateMochigoma(state, op);
            Debug.Log("ターン数: " + state.turn);
        }
        selectedKoma = null;
    }

    //駒を置く具体的な処理を行う関数
    void PlaceSelectedKomaOnPosition(RaycastHit hit) {

        int komaSize = selectedKoma.GetComponent<Koma>().size;
        int komaPos = selectedKoma.GetComponent<Koma>().pos;
        int positionNumber = hit.collider.gameObject.GetComponent<Position>().number;

        Mochigoma currentPlayerMochigoma = state.turn % 2 == 1 ? state.sente : state.gote;

        //移動前と後が同じ位置なら、駒を元の位置に戻し、移動処理は行わない
        if (positionNumber == komaPos && komaPos != -1) {
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
                    banmen[komaPos].Add(komaSize);
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

        //現在位置、移動先、駒のサイズを引数にOperatorクラスのインスタンスを生成
        op = new Operator(availablePositionsList, komaPos, positionNumber, komaSize);

        State newState = Put(state, op);
        ApplyMove(newState);

        //勝利判定
        GameResult preMoveResult = CheckWinner(state);
        if (preMoveResult != GameResult.None) {
            Debug.Log($"勝利判定: {preMoveResult}");
            resultText.text = preMoveResult.ToString();
            PrintCurrentBanmen(newState);
            isGameOver = true;
        }
    }

    void MoveAIPiece(Node newNode) {
        int komaSize = newNode.op.koma;
        int sourceNumber = newNode.op.sourcePos;
        int positionNumber = newNode.op.targetPos;

        List<List<int>> banmen = newNode.state.banmen.GetBanmen();

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

        // 駒がAIのものであることを確認
        Koma komaComponent = koma.GetComponent<Koma>();
        if (komaComponent.player != -1) {
            Debug.LogError("Koma does not belong to AI.");
            return;
        }

        // マスのオブジェクトを検索
        GameObject positionObject = GameObject.Find("Position (" + positionNumber + ")");
        if (positionObject == null) {
            Debug.LogError("Position object not found: Position (" + positionNumber + ")");
            return;
        }

        // sourceNumberが盤面の範囲内である場合にのみ、駒を削除し、下の駒を代入
        if (sourceNumber >= 0 && sourceNumber < 9) {
            // sourceNumberの位置から駒を削除することをログに出力
            Debug.Log("Removing Koma from sourceNumber: " + sourceNumber);
            RemoveKomaFromLastElementsArray(sourceNumber);//要は盤面からけせばいいんだから、いらんくね
            //banmenのsourceNumber行の最後尾の駒を削除
            banmen[sourceNumber].RemoveAt(banmen[sourceNumber].Count - 1);
        }

        // 駒をマスの上に配置する処理（駒の底面がマスの上面に来るように調整）
        float komaHeight = koma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = positionObject.transform.position;
        newPosition.y += komaHeight / 2;
        koma.transform.position = newPosition;

        komaComponent.pos = positionNumber; // 駒が配置された新しい位置に更新

        lastElementsArray[positionNumber] = komaSize;
        GetAvailablePositonsList(state);

        // stateの盤面情報を更新
        banmen[positionNumber].Add(komaSize);
        newNode.state.banmen.SetBanmen(banmen);

    }

    void RemoveKomaFromLastElementsArray(int position) {
        List<List<int>> banmen = state.banmen.GetBanmen();
        if (position < 0 || position >= banmen.Count) {
            Debug.LogError("Position is out of range: " + position);
            return;
        }

        List<int> column = banmen[position];
        if (column.Count > 0) {
            int removedKoma = column[column.Count - 1];
            column.RemoveAt(column.Count - 1); // 最後の駒を削除
            Debug.Log("Removed koma: " + removedKoma + " from position: " + position);

            if (column.Count > 0) {
                lastElementsArray[position] = column[column.Count - 1]; // 下の駒を代入
                Debug.Log("Updated lastElementsArray[" + position + "]: " + lastElementsArray[position]);
            }
            else {
                lastElementsArray[position] = 0; // 駒がなくなった場合は0を代入
                Debug.Log("No more pieces at position: " + position);
            }

            // stateの盤面情報を更新
            state.banmen.SetBanmen(banmen);
            //banmenは9つのリストからなるので、9行に分けてログに出力
            for (int i = 0; i < banmen.Count; i++) {
                Debug.Log($"banmen[{i}]: " + string.Join(", ", banmen[i]));
            }
        }
        else {
            Debug.LogError("No pieces to remove at position: " + position);
        }

        GetAvailablePositonsList(state);
    }

    GameObject FindKoma(int size, int sourcePos) {
        // goteKomas 配列内のすべての GameObject をチェック
        foreach (GameObject komaObject in goteKomas) {
            Koma koma = komaObject.GetComponent<Koma>();
            //koma.posとsourcePos、koma.sizeとsizeを全てログに出力
            //Debug.Log($"koma.pos: {koma.pos}, sourcePos: {sourcePos}, koma.size: {koma.size}, size: {size}");
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

        // 盤面上の移動の際、駒を移動元から削除する処理を追加
        if (op.sourcePos >= 0 && op.sourcePos < banmen.Count) {
            if (banmen[op.sourcePos].Count > 0) {
                banmen[op.sourcePos].RemoveAt(banmen[op.sourcePos].Count - 1);
            }
            else {
                Debug.LogWarning("No pieces to remove at source position: " + op.sourcePos);
            }
        }
        /*else {
            Debug.LogWarning("Source position is out of range: " + op.sourcePos);
        }*/
        // 駒を置く処理
        banmen[op.targetPos].Add(op.koma);

        //laseElementsArrayも、グローバル変数じゃなくメンバー変数で持つ必要がある?
        //てかこれいる？
        //lastElementsArray[op.targetPos] = op.koma;

        newState.NextTurn();

        return newState;
    }

    void ApplyMove(State newState) {
        // 現在の状態を新しい状態に更新
        this.state = newState;
        lastElementsArray = new int[9];
        for (int i = 0; i < 9; i++) {
            List<int> column = state.banmen.GetBanmen()[i];
            if (column.Count > 0) {
                lastElementsArray[i] = column[column.Count - 1];
            }
            else {
                lastElementsArray[i] = 0;
            }
        }

        // 現在の盤面の状態をログに出力
        PrintCurrentBanmen(newState);
        //ここでlastElementsArrayもログに出力
        Debug.Log("lastElementsArray: " + string.Join(", ", lastElementsArray));
    }

    private void UpdateMochigoma(State state, Operator op) {
        if (op.koma > 0) {
            state.sente.RemoveKoma(op.koma);
        }
        else {
            state.gote.RemoveKoma(op.koma);
        }
        // 先手と後手の持ち駒を一つのログメッセージとして表示
        string senteMochigoma = "Current Sente Mochigoma: " + string.Join(", ", state.sente.GetMochigoma());
        string goteMochigoma = "Current Gote Mochigoma: " + string.Join(", ", state.gote.GetMochigoma());
        Debug.Log(senteMochigoma + "\n" + goteMochigoma);
    }

    //勝利判定を行う関数
    public GameResult CheckWinner(State state) {
        var (senteArray, goteArray) = CreateBinaryArrays(state);

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
    List<Operator> GetMochigomaOperators(State state, bool isMaximizingPlayer) {
        List<Operator> operators = new List<Operator>();

        // プレイヤーの持ち駒のリストを取得
        List<int> mochigoma = isMaximizingPlayer ? state.gote.GetMochigoma() : state.sente.GetMochigoma();

        // 盤面の状態を取得
        List<List<int>> board = state.banmen.GetBanmen();

        // 持ち駒を空きマスまたは覆えるマスに置く操作を生成
        foreach (var koma in mochigoma) {
            for (int pos = 0; pos < board.Count; pos++) {
                List<int> cell = board[pos];
                int targetPiece = cell[cell.Count - 1]; // 最後の要素が現在の駒のサイズ

                if (targetPiece == 0 || CanCoverPiece(koma, targetPiece)) {
                    operators.Add(new Operator(null, pos, koma));//これのせい？？
                }
            }
        }
        return operators;
    }

    bool CanCoverPiece(int piece, int targetPiece) {
        // 駒のサイズを比較して、覆えるかどうかを判定
        return Math.Abs(piece) > Math.Abs(targetPiece);
    }

    List<Operator> GetBoardOperators(State state, bool isMaximizingPlayer) {

        List<Operator> operators = new List<Operator>();
        List<List<int>> banmen = state.banmen.GetBanmen();
        int player = isMaximizingPlayer ? -1 : 1;//ここが逆だった

        // lastElementsArrayを更新
        int[] lastElementsArray = new int[banmen.Count];
        for (int i = 0; i < banmen.Count; i++) {
            if (banmen[i].Count > 0) {
                lastElementsArray[i] = banmen[i][banmen[i].Count - 1];
            }
            else {
                lastElementsArray[i] = 0; // 空の場合は0を設定
            }
        }

        for (int i = 0; i < lastElementsArray.Length; i++) {
            int piece = lastElementsArray[i];
            if ((player > 0 && piece > 0) || (player < 0 && piece < 0)) {//これのせい？
                List<int> possibleMoves = GetPossibleMovesForPiece(piece, i, state);
                foreach (int move in possibleMoves) {
                    operators.Add(new Operator(i, move, piece));
                }
            }
        }
        return operators;
    }

    List<int> GetPossibleMovesForPiece(int piece, int currentPos, State state) {
        List<int> possibleMoves = new List<int>();
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
        int targetPiece = targetStack[targetStack.Count - 1];
        if (targetPiece == 0 || CanCoverPiece(op.koma, targetPiece)) {
            return true;
        }
        return false;

        /*// 移動先が空きマスまたは覆える駒である場合
        if (targetStack.Count == 0 || CanCoverPiece(op.koma, targetStack[targetStack.Count - 1])) {
            //いったんコメントアウト
            //Debug.Log("Valid move: targetStack = " + (targetStack.Count == 0 ? "empty" : targetStack[targetStack.Count - 1].ToString()) + ", op.koma = " + op.koma);
            return true;
        }
        else {
            //いったんコメントアウト
            //Debug.Log("Invalid move: targetStack = " + targetStack[targetStack.Count - 1] + ", op.koma = " + op.koma);
            return false;
        }*/
    }

    //両プレイヤーのリーチの数をカウントする関数
    public (int senteReachCount, int goteReachCount) CountReach(State currentState) {
        int senteReachCount = 0;
        int goteReachCount = 0;

        // リーチ判定に使用するポジションのリストを定義
        List<List<int>> positionsList = new List<List<int>> {
            new List<int> { 0, 1, 2 },
            new List<int> { 3, 4, 5 },
            new List<int> { 6, 7, 8 },
            new List<int> { 0, 3, 6 },
            new List<int> { 1, 4, 7 },
            new List<int> { 2, 5, 8 },
            new List<int> { 0, 4, 8 },
            new List<int> { 2, 4, 6 }
        };

        foreach (var positions in positionsList) {
            if (IsReach(1, positions, currentState)) {
                senteReachCount++;
            }
            if (IsReach(-1, positions, currentState)) {
                goteReachCount++;
            }
        }

        return (senteReachCount, goteReachCount);
    }

    //ある一列がリーチかどうかを判定する関数
    public bool IsReach(int player, List<int> positions, State state) {
        List<List<int>> banmen = state.banmen.GetBanmen();
        int[] lastElementsArray = new int[banmen.Count];

        // 各リストの最後の要素を取得して配列に格納
        for (int i = 0; i < banmen.Count; i++) {
            if (banmen[i].Count > 0) {
                lastElementsArray[i] = banmen[i][banmen[i].Count - 1];
            }
            else {
                lastElementsArray[i] = 0; // リストが空の場合は0を格納
            }
        }

        int count = 0;
        int emptyCount = 0;
        int enemyCount = 0;

        foreach (int pos in positions) {
            int piece = lastElementsArray[pos];
            if ((player > 0 && piece > 0) || (player < 0 && piece < 0)) {
                count++;
            }
            else if (piece == 0) {
                emptyCount++;
            }
            else if ((player > 0 && piece < 0 && Math.Abs(piece) < 3) || (player < 0 && piece > 0 && Math.Abs(piece) < 3)) {
                enemyCount++;
            }
        }

        // 2つの駒が揃っていて、1つの空きマスがあるか、敵の駒がある場合にリーチとみなす
        return count == 2 && (emptyCount == 1 || enemyCount == 1);
    }


    //勝利判定のために、1を自分の駒とした二次元配列を返す関数
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

        // currentStateがnullでないことを確認
        if (currentState == null) {
            Debug.LogError("currentState is null");
            return false;
        }

        // opがnullでないことを確認
        if (op == null) {
            Debug.LogError("op is null");
            return false;
        }

        //currentStateの盤面の状態を一時変数に格納
        List<List<int>> banmen = currentState.banmen.GetBanmen();

        // banmenがnullでないことを確認
        if (banmen == null) {
            Debug.LogError("banmen is null");
            return false;
        }

        // targetPosが有効なインデックスであることを確認
        if (op.targetPos < 0 || op.targetPos >= banmen.Count) {
            Debug.LogError("targetPos is out of range");
            return false;
        }

        //op.targetPosのリストを取得
        List<int> targetPosKomas = banmen[op.targetPos];
        // targetPosKomasがnullでないことを確認
        if (targetPosKomas == null) {
            Debug.LogError("targetPosKomas is null");
            return false;
        }
        //op.targetPosのリストの最後の要素を取得
        int lastElement = targetPosKomas.Count > 0 ? targetPosKomas[targetPosKomas.Count - 1] : 0;
        //op.komaとlastElementの絶対値を比較して、op.komaがlastElementに被せることができるかどうかを判定
        return Math.Abs(op.koma) > Math.Abs(lastElement);
    }

    //AIがその手がプレイヤーのリーチを潰した数を計算する関数
    public int CountBlockedReaches(State parentState, State currentState) {
        // parentStateの先手のリーチ数をカウント
        var (parentSenteReachCount, _) = CountReach(parentState);

        // currentStateの先手のリーチ数をカウント
        var (currentSenteReachCount, _) = CountReach(currentState);

        // 潰せた先手のリーチの数を計算
        int blockedSenteReaches = parentSenteReachCount - currentSenteReachCount;

        return blockedSenteReaches;
    }

    int GetPositionScore(int position) {
        switch (position) {
            case 0:
            case 2:
            case 6:
            case 8:
                return 100; // 四隅
            case 4:
                return 200; // 中央
            case 1:
            case 3:
            case 5:
            case 7:
                return 50;  // その他
            default:
                return 0;   // 無効な位置
        }
    }


    Node getNext(State state, int depth) {

        Node root = new Node(state, null, null);
        // 偶数ターンはAIプレイヤー
        bool isMaximizingPlayer = state.turn % 2 == 0;
        int bestValue = Minimax(root, depth, isMaximizingPlayer);

        Node bestMove = null;

        // 子ノードをすべて調べて最適な手を見つける
        foreach (Node child in root.children) {
            if (child.eval == bestValue) {
                bestMove = child;
                break;
            }
        }

        if (bestMove == null) {
            Debug.LogError("getNext: bestMove is null");
        }
        else if (bestMove.op == null) {
            Debug.LogError("getNext: bestMove.op is null");
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
            var possibleMoves = GetPossibleMoves(node.state, isMaximizingPlayer);

            // すべての可能な次の状態を生成
            foreach ((State childState, Operator op) in possibleMoves) {
                if (op == null) {
                    Debug.LogError("op is null in Minimax for maximizing player");
                    continue;
                }
                Node childNode = new Node(childState, node.state, op);
                node.children.Add(childNode);

                // 勝利条件を満たす手が見つかった場合、その手を即座に返す
                if (CheckWinner(childState) == GameResult.GoteWin) {
                    node.eval = EvaluateState(childNode);
                    return node.eval;
                }
                // 再帰的にMinimaxを呼び出し、評価値を計算
                int eval = Minimax(childNode, depth - 1, false);
                maxEval = Math.Max(maxEval, eval);
                
            }
            node.eval = maxEval;
            return maxEval;
        }
        // 得点最小化プレイヤーの場合
        else {
            int minEval = int.MaxValue;
            var possibleMoves = GetPossibleMoves(node.state, isMaximizingPlayer);

            foreach ((State childState, Operator op) in possibleMoves) {
                if (op == null) {
                    Debug.LogError("op is null in Minimax for minimizing player");
                    continue;
                }
                Node childNode = new Node(childState, node.state, op);
                node.children.Add(childNode);

                // 勝利条件を満たす手が見つかった場合、その手を即座に返す
                if (CheckWinner(childState) == GameResult.SenteWin) {
                    node.eval = EvaluateState(childNode);
                    return node.eval;
                }
                // 再帰的にMinimaxを呼び出し、評価値を計算
                int eval = Minimax(childNode, depth - 1, true);
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
    List<(State, Operator)> GetPossibleMoves(State state, bool isMaximizingPlayer) {//////////
        List<(State, Operator)> possibleMoves = new List<(State, Operator)>();

        // 持ち駒から置ける場所のリストを取得
        List<int> availablePositions = GetAvailablePositonsList(state);

        if (availablePositions.Count > 0) {
            // 持ち駒から置ける場合
            List<Operator> mochigomaOperators = GetMochigomaOperators(state, isMaximizingPlayer);
            foreach (var op in mochigomaOperators) {
                if (IsValidMove(state, op)) {
                    State newState = Put(state, op);
                    possibleMoves.Add((newState, op));
                }
            }
        }
        else {
            // 盤面から動かす場合
            List<Operator> boardOperators = GetBoardOperators(state, isMaximizingPlayer);
            foreach (var op in boardOperators) {
                if (IsValidMove(state, op)) {
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
        // 後手のビンゴラインが揃っている場合は1000点を加算
        if (result == GameResult.GoteWin) {
            evaluation += 1000;
        }
        // 先手のビンゴラインが揃っている場合は-10000点を加算
        if (result == GameResult.SenteWin) {
            evaluation -= 10000;
        }
        // op.komaが相手の上に被せている場合、+150点
        if (CheckCoveringMove(currentState, node.op)) {
            evaluation += 150;
        }

        // 潰せたプレイヤーのリーチの数をカウント
        int blockedReaches = CountBlockedReaches(parentState, currentState);
        evaluation += blockedReaches * 150;

        // currentStateにおける敵AIのリーチ数×25点evaluationに加算
        int goteReachCount = CountReach(currentState).goteReachCount;
        evaluation += goteReachCount * 25;

        // 置いたマス目ごとに点数を付ける
        int positionScore = GetPositionScore(node.op.targetPos);
        evaluation += positionScore;

        // 最終評価値を返す
        node.eval = evaluation;
        return evaluation;
    }

    int EvaluateStateWithDebug(Node node) {
        State currentState = node.state; // 現在の状態を取得
        State parentState = node.parentState; // 親ノードの状態を取得
        int evaluation = 0;

        // opがnullでないことを確認
        if (node.op == null) {
            Debug.LogError("op is null in EvaluateStateWithDebug");
            return 0;
        }

        GameResult result = CheckWinner(currentState);
        // 後手のビンゴラインが揃っている場合は1000点を加算
        if (result == GameResult.GoteWin) {
            evaluation += 1000;
            Debug.Log("GoteWin: +1000, Evaluation: " + evaluation);
        }
        // 先手のビンゴラインが揃っている場合は-10000点を加算
        if (result == GameResult.SenteWin) {
            evaluation -= 10000;
            Debug.Log("SenteWin: -10000, Evaluation: " + evaluation);
        }
        // op.komaが相手の上に被せている場合、+150点
        if (CheckCoveringMove(currentState, node.op)) {
            evaluation += 150;
            Debug.Log("CoveringMove: +150, Evaluation: " + evaluation);
        }

        // 潰せたプレイヤーのリーチの数をカウント
        int blockedReaches = CountBlockedReaches(parentState, currentState);
        evaluation += blockedReaches * 150;
        Debug.Log("BlockedReaches: +" + (blockedReaches * 150) + ", Evaluation: " + evaluation);

        // currentStateにおける敵AIのリーチ数×25点evaluationに加算
        var reachCounts = CountReachWithDebug(currentState);
        int goteReachCount = reachCounts.goteReachCount;
        evaluation += goteReachCount * 25;
        Debug.Log("GoteReachCount: +" + (goteReachCount * 25) + ", Evaluation: " + evaluation);

        // 置いたマス目ごとに点数を付ける
        int positionScore = GetPositionScore(node.op.targetPos);
        evaluation += positionScore;
        Debug.Log("PositionScore: +" + positionScore + ", Evaluation: " + evaluation);

        // 最終評価値を返す
        Debug.Log("Final Evaluation: " + evaluation);
        node.eval = evaluation; // ここを追加
        return evaluation;
    }


    public (int senteReachCount, int goteReachCount) CountReachWithDebug(State currentState) {
        int senteReachCount = 0;
        int goteReachCount = 0;

        // リーチ判定に使用するポジションのリストを定義
        List<List<int>> positionsList = new List<List<int>> {
                new List<int> { 0, 1, 2 },
                new List<int> { 3, 4, 5 },
                new List<int> { 6, 7, 8 },
                new List<int> { 0, 3, 6 },
                new List<int> { 1, 4, 7 },
                new List<int> { 2, 5, 8 },
                new List<int> { 0, 4, 8 },
                new List<int> { 2, 4, 6 }
            };

        foreach (var positions in positionsList) {
            if (IsReach(1, positions, currentState)) {
                senteReachCount++;
                //Debug.Log($"Sente reach found at positions: {string.Join(", ", positions)}");
            }
            if (IsReach(-1, positions, currentState)) {
                goteReachCount++;
                //Debug.Log($"Gote reach found at positions: {string.Join(", ", positions)}");
            }
        }

        return (senteReachCount, goteReachCount);
    }



}
