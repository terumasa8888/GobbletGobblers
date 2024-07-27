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
    int[] lastElementsArray; // サイズ9の配列を定義
    List<int> availablePositionsList; // 駒を置ける場所のリスト
    private GameObject selectedKoma;//Unityで選択された駒を格納するための変数
    public LayerMask komaLayer; // Koma用のレイヤーマスク
    public LayerMask positionLayer;  // Position用のレイヤーマスク
    private Vector3 originalPosition; // 選択された駒の移動前の位置情報を保持する変数


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
                        Debug.Log("選択した駒を持ち駒に追加します");
                        currentPlayerMochigoma.AddKoma(komaSize);
                        //移動元の位置のリストから最後尾の駒を削除
                        List<List<int>> banmen = state.banmen.GetBanmen();
                        banmen[komaPos].RemoveAt(banmen[komaPos].Count - 1);

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
            PlaceSelectedKomaOnPosition(hit);
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

        // Komaコンポーネントのposプロパティを更新
        Koma komaComponent = selectedKoma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // 駒が配置された新しい位置に更新

        selectedKoma = null;

        // Operatorクラスのインスタンスを生成
        op = new Operator(availablePositionsList, komaPos, positionNumber, komaSize);
        // stateを更新する処理
        Put(state, op);
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



    //駒を置いた後の状態を生成する状態遷移関数
    State Put(State state, Operator op) {
        // オペレータが有効かどうかをチェック
        if (!IsValidMove(op)) {
            throw new InvalidOperationException("Invalid move");
        }

        // 新しい状態を生成
        State newState = new State(state);
        // オペレータに基づいて駒を置く処理を行う      
        // 盤面の状態を取得
        List<List<int>> banmen = newState.banmen.GetBanmen();
        // 目標位置の駒リストを取得
        List<int> targetPosKomas = banmen[op.targetPos];
        // 目標位置に駒を置く
        targetPosKomas.Add(op.koma);
        //lastElementsArrayに加える
        lastElementsArray[op.targetPos] = op.koma;

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

        newState.NextTurn();

        //ターンを進める
        //state.NextTurn();//残す

        // 現在の盤面の状態をログに出力
        PrintCurrentBanmen(state);

        //AIはPutを探索に使うので、勝利判定はいったんコメントアウト
        // 駒を置いた後の勝利判定
        /*GameResult postMoveResult = CheckWinner(state);
        if (postMoveResult != GameResult.None) {
            Debug.Log($"勝利判定の結果: {postMoveResult}");
            return newState; // 勝敗が決まった場合はここで処理を終了
        }*/

        //AIはPutを探索に使うので、勝利判定はいったんコメントアウト
        // 駒を置ける場所のリストを再計算して表示
        //availablePositionsList = GetAvailablePositonsList(state);
        return newState;
    }

    void ApplyMove(State newState) {
        // 現在の状態を新しい状態に更新
        this.state = newState;

        // 現在の盤面の状態をログに出力
        PrintCurrentBanmen(state);
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


    // オペレータが有効かどうかをチェックするメソッド
    public bool IsValidMove(Operator op) {
        //pos<0またはpos>=9の場合、IndexOutOfRangeExceptionをスロー
        //グローバル変数lastElementsArrayは保守の観点から検討の必要あり
        if (op.targetPos < 0 || op.targetPos >= lastElementsArray.Length) {
            Debug.LogError("IndexOutOfRangeException: Index was outside the bounds of the array.");
            return false; // 範囲外の場合は、処理を中断してstateをそのまま返す
        }
        //絶対値を比較して、op.komaがop.targetPosに置けるかどうかを判定
        if (lastElementsArray[op.targetPos] < Math.Abs(op.koma)) {//置ける場合
            return true;
        }
        else {//置けない場合
            return false;
        }
    }

    //AIがその手がリーチをつぶす手かどうかを判定する関数
    public bool CheckBlockingMove(State currentState, Operator op) {

        // 現在のStateをディープコピー
        State simulatedState = new State(currentState);

        // 仮に指定された位置にプレイヤーの駒を置き、Putの返り値を取得
        simulatedState = Put(simulatedState, op);

        // その状態でプレイヤーが勝利するかどうかをチェック
        GameResult result = CheckWinner(simulatedState);

        // プレイヤーが勝利する場合、その手はリーチ潰しと言えるのでtrueを返す
        return result == GameResult.SenteWin;
    }

    //AIがリーチの数を計算する関数
    public int CountReach(State currentState) {
        int reachCount = 0;
        
        // 現在のStateをディープコピー
        State simulatedState = new State(currentState);

        // 仮に指定された位置にプレイヤーの駒を置き、Putの返り値を取得
        simulatedState = Put(simulatedState, op);

        // その状態のgoteArrayを取得
        var (_, goteArray) = CreateBinaryArrays(simulatedState);
        
        //goteArrayのリーチの数を計算


        //その状態で出来ている敵AIのリーチの数を計算
        return reachCount;
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

    State getNext(State state, int depth) {
        // ルートノードを現在の状態で初期化
        Node root = new Node(state, null, null);
        // 偶数ターンはAIプレイヤー
        bool isMaximizingPlayer = state.turn % 2 == 0;
        // ミニマックスアルゴリズムを使用して最適な手を探索
        Minimax(root, depth, isMaximizingPlayer);
        Node bestMove = null;
        int bestEval = int.MinValue;// 最適な評価値を初期化

        // 子ノードをすべて調べて最適な手を見つける
        foreach (Node child in root.children) {
            if (child.eval > bestEval) {
                bestEval = child.eval;
                bestMove = child;
            }
        }

        // 最適な手が見つかった場合、その状態を返す
        return bestMove != null ? bestMove.state : state;
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
        // 勝敗がついているかをチェックし結果を返す
        return CheckWinner(state) != GameResult.None;
    }

    // 現在の状態から可能なすべての手を生成する関数
    List<(State, Operator)> GetPossibleMoves(State state) {// 現在の状態から可能なすべての手を生成する
        List<(State, Operator)> possibleMoves = new List<(State, Operator)>();
        //引数State:root.state　ルートノードのstate
        //返り値State:childState 考えられる手（子ノード）のstate
        //返り値Operator:childStateに至るためのオペレータ

        //置けるところリストについて、手持ちから作成できるopを全て作成
        //現在の状態から考えられる1手先を全て生成し、possibleMovesに作成した状態とオペレータを追加することを繰り返す

        return possibleMoves;
    }

    // 状態を評価する関数
    int EvaluateState(Node node) {
        State currentState = node.state; // 現在の状態を取得
        State parentState = node.parentState; // 親ノードの状態を取得
        //要は、opを使わないから逆算して求めようとしている
        //でも、opを使えば簡単に求められるので、opを使うべき
        //そうすれば、parentStateとopで、少ないコード量で評価関数を実現できる
        //でも、持ち上げたら負ける場面は絶対に排除したい。敵のビンゴは-1000点とか



        //parentStateとcurrentStateの盤面の差分からどこに駒を置いたのかのを取得

        // 現在の状態と親ノードの状態を使用して評価するロジックを実装
        // 例: 親ノードの状態を考慮して評価値を調整する

        int evaluation = 0; // 評価値を初期化

        return evaluation; // 評価値を返す
    }

}
