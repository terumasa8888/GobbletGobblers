using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameController;

public class Evaluator
{
    private GameController gameController;
    public Evaluator(GameController gameController) {
        this.gameController = gameController;
    }

    public int Evaluate(Node node) {
        State currentState = node.state;
        State parentState = node.parentState;
        int evaluation = 0;

        GameResult result = gameController.CheckWinner(currentState);
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
}
