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
    int[] lastElementsArray; //�Ֆʂ̊e�}�X�̍Ō�̗v�f���i�[����z��
    List<int> availablePositionsList; // ���u����ꏊ�̃��X�g
    private GameObject selectedKoma;//Unity�őI�����ꂽ����i�[���邽�߂̕ϐ�
    public LayerMask komaLayer; // Koma�p�̃��C���[�}�X�N
    public LayerMask positionLayer;  // Position�p�̃��C���[�}�X�N
    private Vector3 originalPosition; // �I�����ꂽ��̈ړ��O�̈ʒu����ێ�����ϐ�
    public GameObject[] goteKomas; // ��̃v���n�u���i�[����z��

    // �Q�[���̌��ʂ�\���񋓌^
    public enum GameResult {
        None,      // �N�������Ă��Ȃ�
        SenteWin,  // ���̏���
        GoteWin  // ���̏���
    }

    void Start() {
        isGameOver = false;
        //State�N���X�̃C���X�^���X�𐶐�
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        lastElementsArray = new int[9];

        availablePositionsList = GetAvailablePositonsList(state);
    }

    void Update() {
        if (isGameOver) return;

        if (state.turn % 2 == 1) {
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
        } else {
            // ���̃^�[���i�����^�[���j
            HandleAITurn();
        }

        // ��������
        if (CheckWinner(state) != GameResult.None) {
            isGameOver = true;
            Debug.Log("��������: " + CheckWinner(state));
        }
    }

    void HandleAITurn() {
        // AI�̃^�[������
        Debug.Log("AI�����u�����Ƃ��Ă��܂�");
        GetAvailablePositonsList(state);
        //State newState = getNext(state, 2);
        Node newNode = getNext(state, 2);
        ApplyMove(newNode.state);
        UpdateMochigoma(state, newNode.op);

        //��I�u�W�F�N�g���ړ������鏈����ǉ�
        MoveAIPiece(newNode.op);

        Debug.Log("AI�����u���܂���");
        //PrintCurrentBanmen(state);
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

                // �I��������Ֆʂɂ���A�������u����ꏊ������ꍇ
                if (komaPos != -1 && canPlaceFromMochigoma) {
                    Debug.Log("�������u����ꏊ�����邽�߁A�Ֆʂ̋�͑I���ł��܂���");
                    selectedKoma = null;
                }
                // �I�������������ŁA�������u����ꏊ���Ȃ��ꍇ
                else if (komaPos == -1 && !canPlaceFromMochigoma) {
                    Debug.Log("�������u����ꏊ���Ȃ����߁A������͑I���ł��܂���");
                    selectedKoma = null;
                }
                //�I�񂾋��������ꍇ
                else {
                    //�I��������Ֆʂ̋�̎�
                    if (komaPos != -1) {
                        Debug.Log("�I���������������ɒǉ����܂�");
                        //���̎��̎�Ԃ̃v���C���[�̎�����X�g�ɉ�����
                        currentPlayerMochigoma.AddKoma(komaSize);
                        //�ړ����̈ʒu�̃��X�g����Ō���̋���폜
                        List<List<int>> banmen = state.banmen.GetBanmen();
                        banmen[komaPos].RemoveAt(banmen[komaPos].Count - 1);

                        //��������
                        GameResult postMoveResult = CheckWinner(state);
                        if (postMoveResult != GameResult.None) {
                            Debug.Log($"��������̌���: {postMoveResult}");
                            return; //�I��
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
            //���u���Ĕ��f
            GetAvailablePositonsList(state);
            PlaceSelectedKomaOnPosition(hit);
            UpdateMochigoma(state, op);
            //state.NextTurn();
            Debug.Log("�^�[����: " + state.turn);
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
            //lastElementsArray[positionNumber]�����O�ɏo��
            Debug.Log("lastElementsArray[positionNumber]: " + lastElementsArray[positionNumber]);
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

        Koma komaComponent = selectedKoma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // ��z�u���ꂽ�V�����ʒu�ɍX�V

        selectedKoma = null;

        //positionNumber�����O�ɏo��
        Debug.Log($"positionNumber: {positionNumber}");
        //���݈ʒu�A�ړ���A��̃T�C�Y��������Operator�N���X�̃C���X�^���X�𐶐�
        op = new Operator(availablePositionsList, komaPos, positionNumber, komaSize);

        State newState = Put(state, op);
        ApplyMove(newState);
    }

    void MoveAIPiece(Operator op) {
        int komaSize = op.koma;
        int sourceNumber = op.sourcePos;
        int positionNumber = op.targetPos;

        // �ʒu�͈̔̓`�F�b�N
        if (positionNumber < 0 || positionNumber >= 9) {
            Debug.LogError("positionNumber is out of range: " + positionNumber);
            return;
        }

        // ����擾
        GameObject koma = FindKoma(komaSize, sourceNumber);
        if (koma == null) {
            Debug.LogError("Koma not found.");
            return;
        }

        // �}�X�̃I�u�W�F�N�g������
        GameObject positionObject = GameObject.Find("Position (" + positionNumber + ")");
        if (positionObject == null) {
            Debug.LogError("Position object not found: Position (" + positionNumber + ")");
            return;
        }

        // ����}�X�̏�ɔz�u���鏈���i��̒�ʂ��}�X�̏�ʂɗ���悤�ɒ����j
        float komaHeight = koma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = positionObject.transform.position;
        newPosition.y += komaHeight / 2;
        koma.transform.position = newPosition;

        Koma komaComponent = koma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // ��z�u���ꂽ�V�����ʒu�ɍX�V

        // lastElementsArray �̍X�V
        lastElementsArray[positionNumber] = komaSize;
        Debug.Log("Updated lastElementsArray[" + positionNumber + "]: " + lastElementsArray[positionNumber]);
    }

    GameObject FindKoma(int size, int sourcePos) {
        // goteKomas �z����̂��ׂĂ� GameObject ���`�F�b�N
        foreach (GameObject komaObject in goteKomas) {
            Koma koma = komaObject.GetComponent<Koma>();
            if (koma != null && koma.pos == sourcePos && koma.size == size && koma.player == -1) {
                return komaObject;
            }
        }

        // �����ɍ���Koma��������Ȃ������ꍇ
        Debug.LogError("No matching Koma found.");
        return null;
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
        List<List<int>> banmen = state.banmen.GetBanmen();
        int maxKomaSize = 0;
        List<int> availablePositionsList = new List<int>();

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
                Debug.Log($"banmen[{i}] �͋�ł��B");
            }
        }

        // ���܂��͌��̃^�[���ɉ��������b�Z�[�W��\��
        if (state.turn % 2 == 1) {
            //Debug.Log($"{state.turn}�^�[���ځF���̕��͎����͂��Ă�������");
            Debug.Log("���̎�����:" + string.Join(",", state.sente.GetMochigoma()));
        }
        else {
            //Debug.Log($"{state.turn}�^�[���ځF���̕��͎����͂��Ă�������");
            Debug.Log("���̎�����:" + string.Join(",", state.gote.GetMochigoma()));
        }
        // ���u����ꏊ�̃��X�g��\��
        //Debug.Log("�u����ꏊ: " + string.Join(", ", availablePositionsList));

        return availablePositionsList;
    }


    //���u������̏�Ԃ𐶐������ԑJ�ڊ֐�
    State Put(State state, Operator op) {
        // �I�y���[�^���L�����ǂ������`�F�b�N
        if (!IsValidMove(state, op)) {
            throw new InvalidOperationException("Invalid move");
        }
        State newState = new State(state);
        // �I�y���[�^�Ɋ�Â��ċ��u���������s��      
        List<List<int>> banmen = newState.banmen.GetBanmen();
        // ���u������
        banmen[op.targetPos].Add(op.koma);

        //laseElementsArray���A�O���[�o���ϐ�����Ȃ������o�[�ϐ��Ŏ��K�v������?
        //�Ă����ꂢ��H
        lastElementsArray[op.targetPos] = op.koma;

        //�Q�[���̐i�s�����̒���AI�̎肪�m�肵�Ă���
        //Put��getNext��ApplyMove��UpdateMochigoma�̏��ōs���H
        //��������R�����g�A�E�g
        //UpdateMochigoma(state, op);

        newState.NextTurn();

        // ���݂̔Ֆʂ̏�Ԃ����O�ɏo��
        //PrintCurrentBanmen(state);

        return newState;
    }

    void ApplyMove(State newState) {
        // ���݂̏�Ԃ�V������ԂɍX�V
        this.state = newState;

        // ���݂̔Ֆʂ̏�Ԃ����O�ɏo��
        PrintCurrentBanmen(state);
    }

    //�Ȃ񂩌��̎�����-3��3���邩�珉�������u���Ƃ��̍X�V����������
    private void UpdateMochigoma(State state, Operator op) {
        if (state.turn % 2 == 1) { // ���̏ꍇ
            for (int i = 0; i < state.sente.GetMochigoma().Count; i++) {
                if (state.sente.GetMochigoma()[i] == op.koma) {
                    state.sente.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
        else { // ���̏ꍇ
            for (int i = 0; i < state.gote.GetMochigoma().Count; i++) {
                if (state.gote.GetMochigoma()[i] == op.koma) {
                    state.gote.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
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

    //
    List<Operator> GetMochigomaOperators(State state) {
        List<Operator> operators = new List<Operator>();

        // ������̃��X�g���擾
        List<int> mochigoma = state.gote.GetMochigoma();

        // �Ֆʂ̏�Ԃ��擾
        List<List<int>> board = state.banmen.GetBanmen();

        // ��������󂫃}�X�܂��͕�����}�X�ɒu������𐶐�
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
        // ��̃T�C�Y���r���āA�����邩�ǂ����𔻒�
        return Math.Abs(piece) > Math.Abs(targetPiece);
    }

    List<Operator> GetBoardOperators(State state) {
        List<Operator> operators = new List<Operator>();

        // �Ֆʂ̏�Ԃ��擾
        List<List<int>> banmen = state.banmen.GetBanmen();

        // �Ֆʏ�̋�𓮂�������𐶐�
        for (int pos = 0; pos < banmen.Count; pos++) {
            List<int> stack = banmen[pos];
            if (stack.Count > 0) { // ����݂���ꍇ
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

        // �Ֆʂ̏�Ԃ��擾
        List<List<int>> banmen = state.banmen.GetBanmen();

        // �Փ��̂��ׂĂ̈ʒu���`�F�b�N
        for (int newPos = 0; newPos < banmen.Count; newPos++) {
            // ���݂̈ʒu�̓X�L�b�v
            if (newPos == currentPos) continue;

            List<int> targetStack = banmen[newPos];

            // �ړ��悪�󂫃}�X�܂��͕������ł���ꍇ
            if (targetStack.Count == 0 || CanCoverPiece(piece, targetStack[targetStack.Count - 1])) {
                possibleMoves.Add(newPos);
            }
        }

        return possibleMoves;
    }

    // �I�y���[�^���L�����ǂ������`�F�b�N���郁�\�b�h
    public bool IsValidMove(State state, Operator op) {
        // null�`�F�b�N��ǉ�
        if (state == null) {
            Debug.LogError("State is null");
            return false;
        }
        if (op == null) {
            Debug.LogError("Operator is null");
            return false;
        }

        // �Ֆʂ̏�Ԃ��擾
        List<List<int>> banmen = state.banmen.GetBanmen();

        // pos<0�܂���pos>=9�̏ꍇ�AIndexOutOfRangeException���X���[
        if (op.targetPos < 0 || op.targetPos >= banmen.Count) {
            //targetPos�����O�ɏo��
            Debug.Log("targetPos: " + op.targetPos);
            Debug.LogError("IndexOutOfRangeException: Index was outside the bounds of the array.");
            return false; // �͈͊O�̏ꍇ�́A�����𒆒f����state�����̂܂ܕԂ�
        }

        List<int> targetStack = banmen[op.targetPos];

        // �ړ��悪�󂫃}�X�܂��͕������ł���ꍇ
        if (targetStack.Count == 0 || CanCoverPiece(op.koma, targetStack[targetStack.Count - 1])) {
            Debug.Log("Valid move: targetStack = " + (targetStack.Count == 0 ? "empty" : targetStack[targetStack.Count - 1].ToString()) + ", op.koma = " + op.koma);
            return true;
        }
        else {
            Debug.Log("Invalid move: targetStack = " + targetStack[targetStack.Count - 1] + ", op.koma = " + op.koma);
            return false;
        }
    }

    //AI�����[�`�̐����v�Z����֐�
    public int CountReach(State currentState) {
        int reachCount = 0;

        // ���̏�Ԃ�goteArray���擾
        var (_, goteArray) = CreateBinaryArrays(currentState);

        // goteArray�̃��[�`�̐����v�Z
        // �c�A���̃��[�`���`�F�b�N
        for (int i = 0; i < 3; i++) {
            int[,] horizontalPositions = { { i, 0 }, { i, 1 }, { i, 2 } };
            int[,] verticalPositions = { { 0, i }, { 1, i }, { 2, i } };

            if (IsReach(goteArray[i, 0], goteArray[i, 1], goteArray[i, 2], currentState, horizontalPositions) ||
                IsReach(goteArray[0, i], goteArray[1, i], goteArray[2, i], currentState, verticalPositions)) {
                reachCount++;
            }
        }

        // �΂߂̃��[�`���`�F�b�N
        int[,] diagonalPositions1 = { { 0, 0 }, { 1, 1 }, { 2, 2 } };
        int[,] diagonalPositions2 = { { 0, 2 }, { 1, 1 }, { 2, 0 } };

        if (IsReach(goteArray[0, 0], goteArray[1, 1], goteArray[2, 2], currentState, diagonalPositions1) ||
            IsReach(goteArray[0, 2], goteArray[1, 1], goteArray[2, 0], currentState, diagonalPositions2)) {
            reachCount++;
        }

        // ���̏�Ԃŏo���Ă���GAI�̃��[�`�̐����v�Z
        return reachCount;
    }

    //3�̂���2�����̋�i1�j�ŁA�c��1���󂫁i0�j�܂��͐��̋�i3�����j�ł���ꍇ�����[�`�Ƃ݂Ȃ��܂�
    public bool IsReach(int a, int b, int c, State state, int[,] positions) {
        // 3�̈ʒu�̂���2�����̋�ŁA�c��1���󂫂܂��͐��̋�i3�����j�ł���ꍇ�����[�`�Ƃ݂Ȃ�
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

    //AI�����̎肪�v���C���[�̋�̏ォ��킹��肩�ǂ����𔻒肷��֐�
    public bool CheckCoveringMove(State currentState, Operator op) {
        //currentState�̔Ֆʂ̏�Ԃ��ꎞ�ϐ��Ɋi�[
        List<List<int>> banmen = currentState.banmen.GetBanmen();
        //op.targetPos�̃��X�g���擾
        List<int> targetPosKomas = banmen[op.targetPos];
        //op.targetPos�̃��X�g�̍Ō�̗v�f���擾
        int lastElement = targetPosKomas.Count > 0 ? targetPosKomas[targetPosKomas.Count - 1] : 0;
        //op.koma��lastElement�̐�Βl���r���āAop.koma��lastElement�ɔ킹�邱�Ƃ��ł��邩�ǂ����𔻒�
        return Math.Abs(op.koma) > Math.Abs(lastElement);
    }

    //AI�����̎肪�v���C���[�̃��[�`��ׂ��������v�Z����֐�
    public int CountBlockedReaches(State parentState, State currentState) {
        // parentState�̃��[�`�����J�E���g
        int parentReachCount = CountReach(parentState);
        // currentState�̃��[�`�����J�E���g
        int currentReachCount = CountReach(currentState);
        // �ׂ������[�`�̐����v�Z
        int blockedReaches = parentReachCount - currentReachCount;
        return blockedReaches;
    }


    Node getNext(State state, int depth) {
        // ���[�g�m�[�h�����݂̏�Ԃŏ�����
        Node root = new Node(state, null, null);
        // �����^�[����AI�v���C���[
        bool isMaximizingPlayer = state.turn % 2 == 0;
        // �~�j�}�b�N�X�A���S���Y�����g�p���čœK�Ȏ��T��
        Minimax(root, depth, isMaximizingPlayer);
        Node bestMove = null;
        int bestEval = int.MinValue; // �œK�ȕ]���l��������

        // �q�m�[�h�����ׂĒ��ׂčœK�Ȏ��������
        foreach (Node child in root.children) {
            if (child.eval > bestEval) {
                bestEval = child.eval;
                bestMove = child;
            }
        }

        // �œK�Ȏ肪���������ꍇ�A���̃m�[�h��Ԃ�
        return bestMove != null ? bestMove : root;
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
        return CheckWinner(state) != GameResult.None;
    }

    // ���݂̏�Ԃ���\�Ȃ��ׂĂ̎�𐶐�����֐�
    List<(State, Operator)> GetPossibleMoves(State state) {
        List<(State, Operator)> possibleMoves = new List<(State, Operator)>();

        // �u����ꏊ�̃��X�g���擾
        List<int> availablePositions = GetAvailablePositonsList(state);
        // �莝���̋���擾
        List<int> mochigomas = state.gote.GetMochigoma();

        // �u����ꏊ�Ǝ莝���̋��g�ݍ��킹�ĉ\�Ȏ�𐶐�
        foreach (int pos in availablePositions) {
            foreach (int piece in mochigomas) {
                Operator op = new Operator(new List<int>(), pos, piece);//List, targetPos, koma
                if (IsValidMove(state, op)){
                    // �V������Ԃ𐶐�
                    State newState = Put(state, op);
                    possibleMoves.Add((newState, op));
                }
            }
        }
        return possibleMoves;
    }

    // �]���֐�
    int EvaluateState(Node node) {
        State currentState = node.state; // ���݂̏�Ԃ��擾
        State parentState = node.parentState; // �e�m�[�h�̏�Ԃ��擾
        int evaluation = 0;

        GameResult result = CheckWinner(currentState);
        //���̃r���S���C���������Ă���ꍇ��1000�_�����Z
        if (result == GameResult.GoteWin) {
            evaluation += 1000;
        }
        //���̃r���S���C���������Ă���ꍇ��-10000�_�����Z
        if(result == GameResult.SenteWin) {
            evaluation -= 10000;
        }
        //op.koma������̏�ɔ킹�Ă���ꍇ�A+150�_
        if (CheckCoveringMove(currentState, node.op)) {
            evaluation += 150;
        }
        // �ׂ������[�`�̐����J�E���g
        int blockedReaches = CountBlockedReaches(parentState, currentState);
        evaluation += blockedReaches * 150;
        // currentState�ɂ�����GAI�̃��[�`���~25�_evaluation�ɉ��Z
        int reaches = CountReach(currentState);
        evaluation += reaches * 25;

        return evaluation;
    }



}
