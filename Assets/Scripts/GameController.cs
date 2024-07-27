using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    bool isGameOver = false;// �Q�[�����I���������ǂ�����\��bool�^�̕ϐ�
    State state;
    Operator op;
    int[] lastElementsArray; // �T�C�Y9�̔z����`
    List<int> availablePositionsList; // ���u����ꏊ�̃��X�g
    private GameObject selectedKoma;//Unity�őI�����ꂽ����i�[���邽�߂̕ϐ�
    public LayerMask komaLayer; // Koma�p�̃��C���[�}�X�N
    public LayerMask positionLayer;  // Position�p�̃��C���[�}�X�N
    private Vector3 originalPosition; // �I�����ꂽ��̈ړ��O�̈ʒu����ێ�����ϐ�


    // �Q�[���̌��ʂ�\���񋓌^
    public enum GameResult {
        None,      // �N�������Ă��Ȃ�
        SenteWin,  // ���̏���
        GoteWin  // ���̏���
    }

    void Start() {

        //State�N���X�̃C���X�^���X�𐶐�
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        //lastElement���i�[���邽�߂̃T�C�Y9�̔z����`
        lastElementsArray = new int[9];

        availablePositionsList = GetAvailablePositonsList(state);

    }

    void Update() {
        // �h���b�O�J�n
        if (Input.GetMouseButtonDown(0)) {
            HandlePieceSelection();
        }
        // �h���b�O���ɋ���}�E�X�ɒǏ]������
        if (Input.GetMouseButton(0) && selectedKoma != null) {
            FollowCursor();
        }
        // �h���b�v�i�}�E�X�𗣂������j
        if (Input.GetMouseButtonUp(0) && selectedKoma != null) {
            HandlePieceDrop();
        }
    }


    // �h���b�O�J�n���ɋ��I������֐�
    void HandlePieceSelection() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 5.0f);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, komaLayer)) {
            selectedKoma = hit.collider.gameObject;
            // �I�����ꂽ��̌��݂̈ʒu��ۑ�
            originalPosition = selectedKoma.transform.position;
            Koma selectedKomaComponent = selectedKoma.GetComponent<Koma>();
            int currentPlayer = state.turn % 2 == 1 ? 1 : -1;
            // ���݂̃^�[���Ɋ�Â��āA���܂��͌��̃v���C���[�̎�����𑀍삷�邽�߂̕ϐ����`
            Mochigoma currentPlayerMochigoma = state.turn % 2 == 1 ? state.sente : state.gote;
            int komaSize = 0;
            int komaPos = -1;

            if (selectedKomaComponent.player == currentPlayer) {
                // ��̃T�C�Y���ƈʒu�����擾
                komaSize = selectedKoma.GetComponent<Koma>().size;
                komaPos = selectedKoma.GetComponent<Koma>().pos;

                // �������u����ꏊ�����邩�ǂ����𔻒�
                bool canPlaceFromMochigoma = availablePositionsList.Count > 0;

                //���̏�����AI�̎������ɂ��g����
                // �I��������Ֆʂɂ���A�������u����ꏊ������ꍇ�A�Ֆʂ̋�͑I���ł��Ȃ�
                if (komaPos != -1 && canPlaceFromMochigoma) {
                    Debug.Log("�������u����ꏊ�����邽�߁A�Ֆʂ̋�͑I���ł��܂���");
                    selectedKoma = null;
                }
                // �I�������������ŁA�������u����ꏊ���Ȃ��ꍇ�A������͑I���ł��Ȃ�
                else if (komaPos == -1 && !canPlaceFromMochigoma) {
                    Debug.Log("�������u����ꏊ���Ȃ����߁A������͑I���ł��܂���");
                    selectedKoma = null;
                }
                else {//�I�񂾋��������ꍇ
                    //�I��������Ֆʂ̋�̎��A���̎��̎�Ԃ̃v���C���[�̎�����X�g�ɉ�����
                    if (komaPos != -1) {
                        Debug.Log("�I���������������ɒǉ����܂�");
                        currentPlayerMochigoma.AddKoma(komaSize);
                        //�ړ����̈ʒu�̃��X�g����Ō���̋���폜
                        List<List<int>> banmen = state.banmen.GetBanmen();
                        banmen[komaPos].RemoveAt(banmen[komaPos].Count - 1);

                        //��������
                        GameResult postMoveResult = CheckWinner(state);
                        if (postMoveResult != GameResult.None) {
                            Debug.Log($"��������̌���: {postMoveResult}");
                            return; // ���s�����܂����ꍇ�͂����ŏ������I��
                        }
                    }
                }
            }
            else {
                Debug.Log("���̋�͌��݂̃v���C���[�̂��̂ł͂���܂���");
                selectedKoma = null;
            }
        }
    }

    // �}�E�X�J�[�\���̈ʒu�ɋ��Ǐ]������֐�
    void FollowCursor() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // �}�E�X�J�[�\���̈ʒu�ɋ��Ǐ]�����邽�߂ɁA�J��������̌Œ苗����ۂ�
        Vector3 mousePosition = Input.mousePosition;
        // �J��������̌Œ苗�����X�N���[�����W�ł�Z�l�Ƃ��Đݒ�
        mousePosition.z = 10.0f;
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        // CapsuleCollider���擾���A��̍������擾����
        CapsuleCollider komaCapsuleCollider = selectedKoma.GetComponent<CapsuleCollider>();
        if (komaCapsuleCollider != null) {
            float komaHeight = komaCapsuleCollider.height;
            // CapsuleCollider�̒��S����ꕔ�܂ł̋������l�����Ĉʒu�𒲐�
            newPosition.y += komaHeight * selectedKoma.transform.localScale.y / 2.0f;
        }
        else {
            newPosition.y += 0.5f; // CapsuleCollider���Ȃ��ꍇ�̓f�t�H���g�̃I�t�Z�b�g���g�p
        }
        selectedKoma.transform.position = newPosition;
    }

    // �}�E�X�𗣂������ɋ��u���֐�
    void HandlePieceDrop() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.blue, 5.0f);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, positionLayer)) {
            PlaceSelectedKomaOnPosition(hit);
        }
        selectedKoma = null;
    }

    //���u���֐�
    void PlaceSelectedKomaOnPosition(RaycastHit hit) {

        int komaSize = selectedKoma.GetComponent<Koma>().size;
        int komaPos = selectedKoma.GetComponent<Koma>().pos;
        int positionNumber = hit.collider.gameObject.GetComponent<Position>().number;

        // ���݂̃^�[���Ɋ�Â��āA���܂��͌��̃v���C���[�̎�����𑀍삷�邽�߂̕ϐ����`
        Mochigoma currentPlayerMochigoma = state.turn % 2 == 1 ? state.sente : state.gote;

        //�ړ��O�ƌオ�����ʒu�Ȃ�A������̈ʒu�ɖ߂��A�ړ������͍s��Ȃ�
        if (positionNumber == komaPos && komaPos != -1) {
            // ������̈ʒu�ɖ߂�����
            ResetKomaPosition();
            Debug.Log("���݂Ɠ����ʒu�ɋ��u�����Ƃ͂ł��܂���");
            currentPlayerMochigoma.RemoveKoma(komaSize);
            
            //�����グ����͂�������폜����Ă���̂ŁA���̈ʒu�ɖ߂��������s��
            List<List<int>> banmen = state.banmen.GetBanmen();
            if (komaPos >= 0 && komaPos < banmen.Count) {
                if (banmen[komaPos].Count > 0) {
                    banmen[komaPos][banmen[komaPos].Count - 1] = komaSize;
                }
                else {
                    banmen[komaPos].Add(komaSize); // ���X�g����̏ꍇ�A�V�����v�f��ǉ�
                }
            }
            else {
                Debug.LogError("komaPos is out of range: " + komaPos);
            }
            return;
        }
        //�ړ���ɂ��傫�������Ȃ�A���u�����ɏ������I��
        if (Math.Abs(lastElementsArray[positionNumber]) >= Math.Abs(komaSize)) {
            //�I��������̌��̈ʒu(komaPos)�ɖ߂�����
            ResetKomaPosition();
            Debug.Log("�I����������傫����̏�ɒu�����Ƃ͂ł��܂���");
            currentPlayerMochigoma.RemoveKoma(komaSize);

            //�����グ����͂�������폜����Ă���̂ŁA���̈ʒu�ɖ߂��������s��
            List<List<int>> banmen = state.banmen.GetBanmen();
            if (komaPos >= 0 && komaPos < banmen.Count) {
                if (banmen[komaPos].Count > 0) {
                    banmen[komaPos][banmen[komaPos].Count - 1] = komaSize;
                }
                else {
                    banmen[komaPos].Add(komaSize); // ���X�g����̏ꍇ�A�V�����v�f��ǉ�
                }
            }
            else {
                Debug.LogError("komaPos is out of range: " + komaPos);
            }
            return;
        }

        // ����}�X�̏�ɔz�u���鏈���i��̒�ʂ��}�X�̏�ʂɗ���悤�ɒ����j
        float komaHeight = selectedKoma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = hit.collider.transform.position;
        newPosition.y += komaHeight / 2;
        selectedKoma.transform.position = newPosition;

        // Koma�R���|�[�l���g��pos�v���p�e�B���X�V
        Koma komaComponent = selectedKoma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // ��z�u���ꂽ�V�����ʒu�ɍX�V

        selectedKoma = null;

        // Operator�N���X�̃C���X�^���X�𐶐�
        op = new Operator(availablePositionsList, komaPos, positionNumber, komaSize);
        // state���X�V���鏈��
        Put(state, op);
    }

    // ������̈ʒu�ɖ߂����������ʉ�
    void ResetKomaPosition() {
        if (selectedKoma != null) {
            selectedKoma.transform.position = originalPosition;
            selectedKoma = null;
        }
    }

    //���u����ꏊ�̃��X�g���v�Z����֐�
    public List<int> GetAvailablePositonsList(State state) {
        // �Ֆʂ̏�Ԃ��ꎞ�ϐ��Ɋi�[
        List<List<int>> banmen = state.banmen.GetBanmen();
        //������̒��Ő�Βl���ł��傫����̃T�C�Y���i�[����int�^�̕ϐ�
        int maxKomaSize = 0;
        List<int> availablePositionsList = new List<int>();

        // ���݂̃^�[���Ɋ�Â��āA���܂��͌��̎�������擾
        List<int> currentMochigoma = state.turn % 2 == 1 ? state.sente.GetMochigoma() : state.gote.GetMochigoma();
        //���݂̎�����̒��Ő�Βl���ł��傫�����̂̃T�C�Y��maxKomaSize�Ɋi�[
        foreach (int komaSize in currentMochigoma) {
            if (Math.Abs(komaSize) > maxKomaSize) {
                maxKomaSize = Math.Abs(komaSize);
            }
        }
        //�e�}�X�̍Ō�̗v�f���擾���AlastElementsArray�Ɋi�[
        for (int i = 0; i < banmen.Count; i++) {
            if (banmen[i].Count > 0) {
                int lastElement = banmen[i][banmen[i].Count - 1];
                lastElementsArray[i] = lastElement;

                if (Math.Abs(lastElement) < maxKomaSize) {
                    availablePositionsList.Add(i);
                }
            }
            else {
                Debug.Log($"banmen[{i}] �͋�ł��B");//���ꂪ�\������邱�Ƃ͂Ȃ�
            }
        }

        // ���܂��͌��̃^�[���ɉ��������b�Z�[�W��\��
        if (state.turn % 2 == 1) {
            Debug.Log($"{state.turn}�^�[���ځF���̕��͎����͂��Ă�������");
            Debug.Log("���̎�����:" + string.Join(",", state.sente.GetMochigoma()));
        }
        else {
            Debug.Log($"{state.turn}�^�[���ځF���̕��͎����͂��Ă�������");
            Debug.Log("���̎�����:" + string.Join(",", state.gote.GetMochigoma()));
        }
        // ���u����ꏊ�̃��X�g��\��
        Debug.Log("�u����ꏊ: " + string.Join(", ", availablePositionsList));

        return availablePositionsList;
    }



    //���u������̏�Ԃ𐶐������ԑJ�ڊ֐�
    State Put(State state, Operator op) {
        // �I�y���[�^���L�����ǂ������`�F�b�N
        if (!IsValidMove(op)) {
            throw new InvalidOperationException("Invalid move");
        }

        // �V������Ԃ𐶐�
        State newState = new State(state);
        // �I�y���[�^�Ɋ�Â��ċ��u���������s��      
        // �Ֆʂ̏�Ԃ��擾
        List<List<int>> banmen = newState.banmen.GetBanmen();
        // �ڕW�ʒu�̋�X�g���擾
        List<int> targetPosKomas = banmen[op.targetPos];
        // �ڕW�ʒu�ɋ��u��
        targetPosKomas.Add(op.koma);
        //lastElementsArray�ɉ�����
        lastElementsArray[op.targetPos] = op.koma;

        //������̍X�V
        if (state.turn % 2 == 1) {//���̏ꍇ
            for (int i = 0; i < state.sente.GetMochigoma().Count; i++) {
                if (state.sente.GetMochigoma()[i] == op.koma) {
                    state.sente.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
        else {//���̏ꍇ
            for (int i = 0; i < state.gote.GetMochigoma().Count; i++) {
                if (state.gote.GetMochigoma()[i] == op.koma) {
                    state.gote.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }

        newState.NextTurn();

        //�^�[����i�߂�
        //state.NextTurn();//�c��

        // ���݂̔Ֆʂ̏�Ԃ����O�ɏo��
        PrintCurrentBanmen(state);

        //AI��Put��T���Ɏg���̂ŁA��������͂�������R�����g�A�E�g
        // ���u������̏�������
        /*GameResult postMoveResult = CheckWinner(state);
        if (postMoveResult != GameResult.None) {
            Debug.Log($"��������̌���: {postMoveResult}");
            return newState; // ���s�����܂����ꍇ�͂����ŏ������I��
        }*/

        //AI��Put��T���Ɏg���̂ŁA��������͂�������R�����g�A�E�g
        // ���u����ꏊ�̃��X�g���Čv�Z���ĕ\��
        //availablePositionsList = GetAvailablePositonsList(state);
        return newState;
    }

    void ApplyMove(State newState) {
        // ���݂̏�Ԃ�V������ԂɍX�V
        this.state = newState;

        // ���݂̔Ֆʂ̏�Ԃ����O�ɏo��
        PrintCurrentBanmen(state);
    }

    //����������s���֐�
    public GameResult CheckWinner(State state) {
        var (senteArray, goteArray) = CreateBinaryArrays(state);

        // ���������̃`�F�b�N
        if (HasWinningLine(senteArray)) {
            string banmenOutput = "";
            for (int row = 0; row < 3; row++) {
                for (int col = 0; col < 3; col++) {
                    banmenOutput += senteArray[row, col].ToString() + " ";
                }
                banmenOutput += "\n";
            }
            //�T���̍ۂ̃��[�`�ׂ�����ɂ��g���̂ŁA��������R�����g�A�E�g
            //Debug.Log($"���̏����Ֆ�:\n{banmenOutput}");
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
            //�T���̍ۂ̃��[�`�ׂ�����ɂ��g���̂ŁA��������R�����g�A�E�g
            //Debug.Log($"���̏����Ֆ�:\n{banmenOutput}");
            return GameResult.GoteWin;
        }

        return GameResult.None;
    }

    //�r���S���C���������Ă��邩�ǂ����𔻒肷��֐�
    private bool HasWinningLine(int[,] array) {
        // �c�A���̏����������`�F�b�N
        for (int i = 0; i < 3; i++) {
            if ((array[i, 0] == 1 && array[i, 1] == 1 && array[i, 2] == 1) ||
                (array[0, i] == 1 && array[1, i] == 1 && array[2, i] == 1)) {
                return true;
            }
        }

        // �΂߂̏����������`�F�b�N
        if ((array[0, 0] == 1 && array[1, 1] == 1 && array[2, 2] == 1) ||
            (array[0, 2] == 1 && array[1, 1] == 1 && array[2, 0] == 1)) {
            return true;
        }
        return false;
    }

    // ���݂̔Ֆʂ̏�Ԃ�3�~3�̓񎟌��z��ɕϊ����A���O�ɏo�͂���֐�
    void PrintCurrentBanmen(State state) {
        int[,] currentBanmen = new int[3, 3];
        // �Ֆʂ̏�Ԃ��ꎞ�ϐ��Ɋi�[
        List<List<int>> banmen = state.banmen.GetBanmen();

        for (int i = 0; i < banmen.Count; i++) {
            int row = i / 3;
            int col = i % 3;
            List<int> stack = banmen[i];
            // ���X�g�̍Ō�̗v�f���Ƃ��Ă���currentBanmen[row, col]�ɑ���B��̏ꍇ��0����
            currentBanmen[row, col] = stack.Count > 0 ? stack[stack.Count - 1] : 0;
        }

        // �ϊ������Ֆʂ̏�Ԃ�3�~3�̌`�Ń��O�ɏo��
        string banmenOutput = "";
        for (int row = 0; row < 3; row++) {
            for (int col = 0; col < 3; col++) {
                banmenOutput += currentBanmen[row, col].ToString() + " ";
            }
            banmenOutput += "\n";
        }
        Debug.Log($"���݂̔Ֆ�:\n{banmenOutput}");
    }


    // �I�y���[�^���L�����ǂ������`�F�b�N���郁�\�b�h
    public bool IsValidMove(Operator op) {
        //pos<0�܂���pos>=9�̏ꍇ�AIndexOutOfRangeException���X���[
        //�O���[�o���ϐ�lastElementsArray�͕ێ�̊ϓ_���猟���̕K�v����
        if (op.targetPos < 0 || op.targetPos >= lastElementsArray.Length) {
            Debug.LogError("IndexOutOfRangeException: Index was outside the bounds of the array.");
            return false; // �͈͊O�̏ꍇ�́A�����𒆒f����state�����̂܂ܕԂ�
        }
        //��Βl���r���āAop.koma��op.targetPos�ɒu���邩�ǂ����𔻒�
        if (lastElementsArray[op.targetPos] < Math.Abs(op.koma)) {//�u����ꍇ
            return true;
        }
        else {//�u���Ȃ��ꍇ
            return false;
        }
    }

    //AI�����̎肪���[�`���Ԃ��肩�ǂ����𔻒肷��֐�
    public bool CheckBlockingMove(State currentState, Operator op) {

        // ���݂�State���f�B�[�v�R�s�[
        State simulatedState = new State(currentState);

        // ���Ɏw�肳�ꂽ�ʒu�Ƀv���C���[�̋��u���APut�̕Ԃ�l���擾
        simulatedState = Put(simulatedState, op);

        // ���̏�ԂŃv���C���[���������邩�ǂ������`�F�b�N
        GameResult result = CheckWinner(simulatedState);

        // �v���C���[����������ꍇ�A���̎�̓��[�`�ׂ��ƌ�����̂�true��Ԃ�
        return result == GameResult.SenteWin;
    }

    //AI�����[�`�̐����v�Z����֐�
    public int CountReach(State currentState) {
        int reachCount = 0;
        
        // ���݂�State���f�B�[�v�R�s�[
        State simulatedState = new State(currentState);

        // ���Ɏw�肳�ꂽ�ʒu�Ƀv���C���[�̋��u���APut�̕Ԃ�l���擾
        simulatedState = Put(simulatedState, op);

        // ���̏�Ԃ�goteArray���擾
        var (_, goteArray) = CreateBinaryArrays(simulatedState);
        
        //goteArray�̃��[�`�̐����v�Z


        //���̏�Ԃŏo���Ă���GAI�̃��[�`�̐����v�Z
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
        // ���[�g�m�[�h�����݂̏�Ԃŏ�����
        Node root = new Node(state, null, null);
        // �����^�[����AI�v���C���[
        bool isMaximizingPlayer = state.turn % 2 == 0;
        // �~�j�}�b�N�X�A���S���Y�����g�p���čœK�Ȏ��T��
        Minimax(root, depth, isMaximizingPlayer);
        Node bestMove = null;
        int bestEval = int.MinValue;// �œK�ȕ]���l��������

        // �q�m�[�h�����ׂĒ��ׂčœK�Ȏ��������
        foreach (Node child in root.children) {
            if (child.eval > bestEval) {
                bestEval = child.eval;
                bestMove = child;
            }
        }

        // �œK�Ȏ肪���������ꍇ�A���̏�Ԃ�Ԃ�
        return bestMove != null ? bestMove.state : state;
    }

    int Minimax(Node node, int depth, bool isMaximizingPlayer) {
        // �T���̐[����0�܂��̓Q�[�����I�����Ă���ꍇ�A�]���l��Ԃ�
        if (depth == 0 || IsGameOver(node.state)) {
            node.eval = EvaluateState(node); // ���̃m�[�h�̕]���l��]���֐�����v�Z
            return node.eval;
        }

        // �GAI�����_�ő剻�v���C���[�̏ꍇ
        if (isMaximizingPlayer) {
            int maxEval = int.MinValue;
            // ���ׂẲ\�Ȏ��̏�Ԃ𐶐�
            foreach ((State childState, Operator op) in GetPossibleMoves(node.state)) {
                // �q�m�[�h�𐶐����A�e�m�[�h��state�ƃI�y���[�^��n��
                Node childNode = new Node(childState, node.state, op);
                // �e�m�[�h��children���X�g�Ɏq�m�[�h��ǉ�
                node.children.Add(childNode);
                // �ċA�I��Minimax���Ăяo���A�]���l���v�Z
                int eval = Minimax(childNode, depth - 1, false);
                // ���傫������maxEval�Ƃ��A�ő�]���l���X�V
                maxEval = Math.Max(maxEval, eval);
            }
            node.eval = maxEval;
            return maxEval;
        }
        // �GAI�����_�ŏ����v���C���[�̏ꍇ
        else {
            int minEval = int.MaxValue;
            // ���ׂẲ\�Ȏ��̏�Ԃ𐶐�
            foreach ((State childState, Operator op) in GetPossibleMoves(node.state)) {
                // �q�m�[�h�𐶐����A�e�m�[�h��state�ƃI�y���[�^��n��
                Node childNode = new Node(childState, node.state, op);
                // �e�m�[�h��children���X�g�Ɏq�m�[�h��ǉ�
                node.children.Add(childNode);
                // �ċA�I��Minimax���Ăяo���A�]���l���v�Z
                int eval = Minimax(childNode, depth - 1, true);
                // ��菬��������minEval�Ƃ��A�ŏ��]���l���X�V
                minEval = Math.Min(minEval, eval);
            }
            node.eval = minEval;
            return minEval;
        }
    }

    // �Q�[�����I�����Ă��邩�ǂ����𔻒肷��֐�
    bool IsGameOver(State state) {
        // ���s�����Ă��邩���`�F�b�N�����ʂ�Ԃ�
        return CheckWinner(state) != GameResult.None;
    }

    // ���݂̏�Ԃ���\�Ȃ��ׂĂ̎�𐶐�����֐�
    List<(State, Operator)> GetPossibleMoves(State state) {// ���݂̏�Ԃ���\�Ȃ��ׂĂ̎�𐶐�����
        List<(State, Operator)> possibleMoves = new List<(State, Operator)>();
        //����State:root.state�@���[�g�m�[�h��state
        //�Ԃ�lState:childState �l�������i�q�m�[�h�j��state
        //�Ԃ�lOperator:childState�Ɏ��邽�߂̃I�y���[�^

        //�u����Ƃ��냊�X�g�ɂ��āA�莝������쐬�ł���op��S�č쐬
        //���݂̏�Ԃ���l������1����S�Đ������ApossibleMoves�ɍ쐬������ԂƃI�y���[�^��ǉ����邱�Ƃ��J��Ԃ�

        return possibleMoves;
    }

    // ��Ԃ�]������֐�
    int EvaluateState(Node node) {
        State currentState = node.state; // ���݂̏�Ԃ��擾
        State parentState = node.parentState; // �e�m�[�h�̏�Ԃ��擾
        //�v�́Aop���g��Ȃ�����t�Z���ċ��߂悤�Ƃ��Ă���
        //�ł��Aop���g���ΊȒP�ɋ��߂���̂ŁAop���g���ׂ�
        //��������΁AparentState��op�ŁA���Ȃ��R�[�h�ʂŕ]���֐��������ł���
        //�ł��A�����グ���畉�����ʂ͐�΂ɔr���������B�G�̃r���S��-1000�_�Ƃ�



        //parentState��currentState�̔Ֆʂ̍�������ǂ��ɋ��u�����̂��̂��擾

        // ���݂̏�ԂƐe�m�[�h�̏�Ԃ��g�p���ĕ]�����郍�W�b�N������
        // ��: �e�m�[�h�̏�Ԃ��l�����ĕ]���l�𒲐�����

        int evaluation = 0; // �]���l��������

        return evaluation; // �]���l��Ԃ�
    }

}
