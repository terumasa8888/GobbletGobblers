using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Evaluator {
    private static readonly int WIN_SCORE = 1000;
    private static readonly int LOSE_SCORE = -10000;
    private static readonly int COVERING_MOVE_SCORE = 150;
    private static readonly int BLOCKED_REACH_SCORE = 150;
    private static readonly int REACH_SCORE = 25;

    //�����̈ʒu�ɑ΂���X�R�A
    private static readonly int CENTER_SCORE = 200;
    //�l���̈ʒu�ɑ΂���X�R�A
    private static readonly int CORNER_SCORE = 100;
    //�l���ƒ����ȊO�̈ʒu�ɑ΂���X�R�A
    private static readonly int OTHER_SCORE = 50;

    public int Evaluate(Node node) {
        State currentState = node.state;
        State parentState = node.parentState;
        int evaluation = 0;

        GameResult result = currentState.CheckWinner();
        if (result == GameResult.GoteWin) {
            evaluation += WIN_SCORE;
        }
        if (result == GameResult.SenteWin) {
            evaluation += LOSE_SCORE;
        }
        if (CheckCoveringMove(currentState, node.op)) {
            evaluation += COVERING_MOVE_SCORE;
        }
        int blockedReaches = CountBlockedReaches(parentState, currentState);
        evaluation += blockedReaches * BLOCKED_REACH_SCORE;

        int goteReachCount = CountReach(currentState).goteReachCount;
        evaluation += goteReachCount * REACH_SCORE;

        int positionScore = GetPositionScore(node.op.TargetPos());
        evaluation += positionScore;

        node.SetEval(evaluation);
        return evaluation;
    }

    //���v���C���[�̃��[�`�̐����J�E���g����֐�
    public (int senteReachCount, int goteReachCount) CountReach(State currentState) {
        int senteReachCount = 0;
        int goteReachCount = 0;

        // ���[�`����Ɏg�p����|�W�V�����̃��X�g���`
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

    //�����񂪃��[�`���ǂ����𔻒肷��֐�
    public bool IsReach(int player, List<int> positions, State state) {
        List<List<int>> banmen = state.banmen.GetBanmen();
        int[] lastElementsArray = new int[banmen.Count];

        // �e���X�g�̍Ō�̗v�f���擾���Ĕz��Ɋi�[
        for (int i = 0; i < banmen.Count; i++) {
            if (banmen[i].Count > 0) {
                lastElementsArray[i] = banmen[i][banmen[i].Count - 1];
            }
            else {
                lastElementsArray[i] = 0; // ���X�g����̏ꍇ��0���i�[
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

        // 2�̋�����Ă��āA1�̋󂫃}�X�����邩�A�G�̋����ꍇ�Ƀ��[�`�Ƃ݂Ȃ�
        return count == 2 && (emptyCount == 1 || enemyCount == 1);
    }

    //AI�����̎肪�v���C���[�̋�̏ォ��킹��肩�ǂ����𔻒肷��֐�
    public bool CheckCoveringMove(State currentState, Operator op) {

        if (currentState == null) {
            Debug.LogError("currentState is null");
            return false;
        }
        if (op == null) {
            Debug.LogError("op is null");
            return false;
        }

        //currentState�̔Ֆʂ̏�Ԃ��ꎞ�ϐ��Ɋi�[
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
        //op.targetPos�̃��X�g�̍Ō�̗v�f���擾
        int lastElement = targetPosKomas.Count > 0 ? targetPosKomas[targetPosKomas.Count - 1] : 0;
        //op.koma��lastElement�̐�Βl���r���āAop.koma��lastElement�ɔ킹�邱�Ƃ��ł��邩�ǂ����𔻒�
        return Math.Abs(op.KomaSize()) > Math.Abs(lastElement);
    }

    //AI�����̎肪�v���C���[�̃��[�`��ׂ��������v�Z����֐�
    public int CountBlockedReaches(State parentState, State currentState) {
        // parentState�̐��̃��[�`�����J�E���g
        var (parentSenteReachCount, _) = CountReach(parentState);

        // currentState�̐��̃��[�`�����J�E���g
        var (currentSenteReachCount, _) = CountReach(currentState);

        // �ׂ������̃��[�`�̐����v�Z
        int blockedSenteReaches = parentSenteReachCount - currentSenteReachCount;

        return blockedSenteReaches;
    }

    int GetPositionScore(int position) {
        switch (position) {
            case 0:
            case 2:
            case 6:
            case 8:
                return CORNER_SCORE;
            case 4:
                return CENTER_SCORE;
            case 1:
            case 3:
            case 5:
            case 7:
                return OTHER_SCORE;
            default:
                return 0;
        }
    }
}