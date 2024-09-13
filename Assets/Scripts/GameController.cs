using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    bool isGameOver;
    State state;
    //Operator op;
    /*private GameObject selectedKoma;
    private Vector3 originalPosition;*/
    
    [SerializeField]
    private GameObject[] goteKomas;
    [SerializeField]
    private Text resultText;

    private MouseInputHandler mouseInputHandler;

    // ゲームの結果を表す列挙型
    public enum GameResult {
        None,
        SenteWin,
        GoteWin
    }


    void Start() {
        isGameOver = false;
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        state.UpdateAvailablePositionsList();

        GameObject mouseInputHandlerObject = GameObject.Find("MouseInputHandler");
        if (mouseInputHandlerObject == null) {
            Debug.LogError("MouseInputHandlerオブジェクトが見つかりませんでした。");
            return;
        }
        mouseInputHandler = mouseInputHandlerObject.GetComponent<MouseInputHandler>();
        mouseInputHandler.Initialize(this);

        if (goteKomas == null || goteKomas.Length == 0) {
            Debug.LogError("goteKomas array is not initialized or empty.");
        }
        else {
            Debug.Log($"goteKomas array initialized with {goteKomas.Length} elements.");
        }
    }

    void Update() {
        if (isGameOver) return;

        if (!state.isSenteTurn()) {
            HandleAITurn();
            return;
        }

        if (Input.GetMouseButtonDown(0)) {
            mouseInputHandler.HandlePieceSelection();
        }

        if (Input.GetMouseButton(0)) {
            mouseInputHandler.FollowCursor();
        }

        if (Input.GetMouseButtonUp(0)) {
            mouseInputHandler.HandlePieceDrop();
        }
    }

    void HandleAITurn() {
        state.UpdateAvailablePositionsList();
        Node newNode = getNext(state, 3);

        if (newNode == null || newNode.op == null) {
            Debug.LogError("HandleAITurn: bestMove or bestMove.op is null");
            return;
        }
        MoveAIPiece(newNode);

        Debug.Log("評価値: " + newNode.Eval());
        ApplyMove(newNode.state);
        UpdateMochigoma(state, newNode.op);

        //勝利判定
        CheckForWin();
    }

    

    void MoveAIPiece(Node newNode) {
        int komaSize = newNode.op.KomaSize();
        int sourceNumber = newNode.op.SourcePos();
        int positionNumber = newNode.op.TargetPos();

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
            state.UpdateAvailablePositionsList();

            //banmenのsourceNumber行の最後尾の駒を削除
            banmen[sourceNumber].RemoveAt(banmen[sourceNumber].Count - 1);
        }

        // 駒をマスの上に配置する処理（駒の底面がマスの上面に来るように調整）
        float komaHeight = koma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = positionObject.transform.position;
        newPosition.y += komaHeight / 2;
        koma.transform.position = newPosition;

        komaComponent.pos = positionNumber; // 駒が配置された新しい位置に更新

        // stateの盤面情報を更新
        banmen[positionNumber].Add(komaSize);
        newNode.state.banmen.SetBanmen(banmen);
        newNode.state.UpdateAvailablePositionsList();
        newNode.state.UpdateLastElementsArray();
    }

    GameObject FindKoma(int size, int sourcePos) {
        // goteKomas 配列内のすべての GameObject をチェック
        foreach (GameObject komaObject in goteKomas) {
            Koma koma = komaObject.GetComponent<Koma>();
            //koma.posとsourcePos、koma.sizeとsizeを全てログに出力
            if (koma != null && koma.pos == sourcePos && koma.size == size && koma.player == -1) {
                return komaObject;
            }
        }

        // 条件に合うKomaが見つからなかった場合
        Debug.LogError("No matching Koma found.");
        return null;
    }

    

    //駒を置いた後の状態を生成する状態遷移関数
    public State Put(State state, Operator op) {
        // オペレータが有効かどうかをチェック
        if (!IsValidMove(state, op)) {
            throw new InvalidOperationException("Invalid move");
        }
        State newState = new State(state);
        // オペレータに基づいて駒を置く処理を行う      
        List<List<int>> banmen = newState.banmen.GetBanmen();

        // 盤面上の移動の際、駒を移動元から削除する処理を追加
        int sourcePos = op.SourcePos();
        if (sourcePos >= 0 && sourcePos < banmen.Count) {
            if (banmen[sourcePos].Count > 0) {
                banmen[sourcePos].RemoveAt(banmen[sourcePos].Count - 1);
            }
            else {
                Debug.LogWarning("No pieces to remove at source position: " + sourcePos);
            }
        }
        // 駒を置く処理
        banmen[op.TargetPos()].Add(op.KomaSize());

        newState.NextTurn();

        return newState;
    }

    public State GetState() {
        return state;
    }

    public void ApplyMove(State newState) {
        // 現在の状態を新しい状態に更新
        this.state = newState;

        state.UpdateLastElementsArray();
        PrintCurrentBanmen(newState);
        Debug.Log("Turn: " + state.Turn());
    }

    //持ち駒の更新を行う関数
    public void UpdateMochigoma(State state, Operator op) {//if文要リファクタ
        int komaSize = op.KomaSize();
        if (komaSize > 0) {
            state.sente.RemoveKoma(komaSize);
        }
        else {
            state.gote.RemoveKoma(komaSize);
        }
        // デバッグログを追加して、持ち駒の状態を確認
        string senteMochigoma = "Updated Sente Mochigoma: " + string.Join(", ", state.sente.GetMochigoma());
        string goteMochigoma = "Updated Gote Mochigoma: " + string.Join(", ", state.gote.GetMochigoma());
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

    // 現在の盤面の状態を3×3の二次元配列に変換し、ログに出力する関数
    void PrintCurrentBanmen(State state) {//合格。疎結合になっている
        int[,] currentBanmen = new int[3, 3];
        List<List<int>> banmen = state.banmen.GetBanmen();

        for (int i = 0; i < banmen.Count; i++) {
            int row = i / 3;
            int col = i % 3;
            List<int> stack = banmen[i];
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

        List<List<int>> board = state.banmen.GetBanmen();

        // 持ち駒を空きマスまたは覆えるマスに置く操作を生成
        foreach (var koma in mochigoma) {
            for (int pos = 0; pos < board.Count; pos++) {
                List<int> cell = board[pos];
                int targetPiece = cell[cell.Count - 1]; // 最後の要素が現在の駒のサイズ

                if (targetPiece == 0 || CanCoverPiece(koma, targetPiece)) {
                    operators.Add(new Operator(pos, koma));
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
        int[] lastElementsArray = new int[banmen.Count];//ここもlastElementsArrayを使ってるので注意
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

        List<List<int>> banmen = state.banmen.GetBanmen();
        int targetPos = op.TargetPos();
        if (targetPos < 0 || targetPos >= banmen.Count) {
            Debug.Log("targetPos: " + targetPos);
            Debug.LogError("IndexOutOfRangeException: Index was outside the bounds of the array.");
            return false; // 範囲外の場合は、処理を中断してstateをそのまま返す
        }

        List<int> targetStack = banmen[targetPos];
        int targetPiece = targetStack[targetStack.Count - 1];
        if (targetPiece == 0 || CanCoverPiece(op.KomaSize(), targetPiece)) {
            return true;
        }
        return false;
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


    //勝利判定のために、自分の駒を1、それ以外を0とした二次元配列を返す関数
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

        if (currentState == null) {
            Debug.LogError("currentState is null");
            return false;
        }
        if (op == null) {
            Debug.LogError("op is null");
            return false;
        }

        //currentStateの盤面の状態を一時変数に格納
        List<List<int>> banmen = currentState.banmen.GetBanmen();
        if (banmen == null) {
            Debug.LogError("banmen is null");
            return false;
        }

        int targetPos = op.TargetPos();
        if (targetPos < 0 || targetPos >= banmen.Count) {
            Debug.LogError("targetPos is out of range");
            return false;
        }

        List<int> targetPosKomas = banmen[op.TargetPos()];
        if (targetPosKomas == null) {
            Debug.LogError("targetPosKomas is null");
            return false;
        }
        //op.targetPosのリストの最後の要素を取得
        int lastElement = targetPosKomas.Count > 0 ? targetPosKomas[targetPosKomas.Count - 1] : 0;
        //op.komaとlastElementの絶対値を比較して、op.komaがlastElementに被せることができるかどうかを判定
        return Math.Abs(op.KomaSize()) > Math.Abs(lastElement);
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
                return 100;
            case 4:
                return 200;
            case 1:
            case 3:
            case 5:
            case 7:
                return 50;
            default:
                return 0;
        }
    }


    Node getNext(State state, int depth) {

        Node root = new Node(state, null, null);
        // 偶数ターンはAIプレイヤー
        bool isMaximizingPlayer = !state.isSenteTurn();
        int bestValue = Minimax(root, depth, isMaximizingPlayer);//引数rootに変更を加えてしまっている

        Node bestMove = null;

        // 子ノードをすべて調べて最適な手を見つける
        foreach (Node child in root.Children()) {
            if (child.Eval() == bestValue) {
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
            int evaluation = Evaluate(node);
            node.SetEval(evaluation);
            return node.Eval();
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
                node.AddChild(childNode);

                // 勝利条件を満たす手が見つかった場合、その手を即座に返す
                if (CheckWinner(childState) == GameResult.GoteWin) {
                    int evaluation = Evaluate(childNode);
                    node.SetEval(evaluation);
                    return node.Eval();
                }
                // 再帰的にMinimaxを呼び出し、評価値を計算
                int childEvaluation = Minimax(childNode, depth - 1, false);
                maxEval = Math.Max(maxEval, childEvaluation);
                
            }
            node.SetEval(maxEval);
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
                node.AddChild(childNode);

                // 勝利条件を満たす手が見つかった場合、その手を即座に返す
                if (CheckWinner(childState) == GameResult.SenteWin) {
                    int evaluation = Evaluate(childNode);
                    node.SetEval(evaluation);
                    return node.Eval();
                }
                // 再帰的にMinimaxを呼び出し、評価値を計算
                int childEvaluation = Minimax(childNode, depth - 1, true);
                minEval = Math.Min(minEval, childEvaluation);
                
            }
            node.SetEval(minEval);
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
        state.UpdateAvailablePositionsList();
        List<int> availablePositions = state.AvailablePositionsList();

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
    int Evaluate(Node node) {
        State currentState = node.state;
        State parentState = node.parentState;
        int evaluation = 0;

        GameResult result = CheckWinner(currentState);
        if (result == GameResult.GoteWin) {
            evaluation += 1000;
        }
        if (result == GameResult.SenteWin) {
            evaluation -= 10000;
        }
        if (CheckCoveringMove(currentState, node.op)) {
            evaluation += 150;
        }
        int blockedReaches = CountBlockedReaches(parentState, currentState);
        evaluation += blockedReaches * 150;

        int goteReachCount = CountReach(currentState).goteReachCount;
        evaluation += goteReachCount * 25;

        int positionScore = GetPositionScore(node.op.TargetPos());
        evaluation += positionScore;

        node.SetEval(evaluation);
        return evaluation;
    }

    // 勝利判定の処理をメソッドとして分離
    public void CheckForWin() {
        GameResult result = CheckWinner(state);
        if (result != GameResult.None) {
            Debug.Log($"勝利判定: {result}");
            resultText.text = result.ToString();
            PrintCurrentBanmen(state);
            isGameOver = true;
        }
    }
}
