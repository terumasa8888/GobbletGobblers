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
            //���̎��̔Ֆʂ����O�ɏo��
            PrintCurrentBanmen(state);
        }
    }

    void HandleAITurn() {
        // AI�̃^�[������
        //Debug.Log("AI�����u�����Ƃ��Ă��܂�");
        GetAvailablePositonsList(state);
        //State newState = getNext(state, 2);
        Node newNode = getNext(state, 3);

        // newNode��null�łȂ����Ƃ��m�F
        if (newNode == null) {
            Debug.LogError("newNode is null in HandleAITurn");
            return;
        }

        // newNode.op��null�łȂ����Ƃ��m�F
        if (newNode.op == null) {
            Debug.LogError("newNode.op is null in HandleAITurn");
            return;
        }

        EvaluateStateWithDebug(newNode);
        //����ꂽ�m�[�h�̕]���l�����O�ɏo��
        Debug.Log("�]���l: " + newNode.eval);
        ApplyMove(newNode.state);
        UpdateMochigoma(state, newNode.op);

        //��I�u�W�F�N�g���ړ������鏈����ǉ�
        MoveAIPiece(newNode.op);//�����łȂ���op.koma��-3�ɂȂ�

        //Debug.Log("AI�����u���܂���");
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
        //Debug.Log($"positionNumber: {positionNumber}");
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
        GameObject koma = FindKoma(komaSize, sourceNumber);//komaSize���Ȃ�������-3
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
        //Debug.Log("Updated lastElementsArray[" + positionNumber + "]: " + lastElementsArray[positionNumber]);
    }

    GameObject FindKoma(int size, int sourcePos) {
        // goteKomas �z����̂��ׂĂ� GameObject ���`�F�b�N
        foreach (GameObject komaObject in goteKomas) {
            Koma koma = komaObject.GetComponent<Koma>();
            //koma.pos��sourcePos�Akoma.size��size��S�ă��O�ɏo��
            //Debug.Log($"koma.pos: {koma.pos}, sourcePos: {sourcePos}, koma.size: {koma.size}, size: {size}");
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
        /*if (state.turn % 2 == 1) {
            //Debug.Log($"{state.turn}�^�[���ځF���̕��͎����͂��Ă�������");
            Debug.Log("���̎�����:" + string.Join(",", state.sente.GetMochigoma()));
        }
        else {
            //Debug.Log($"{state.turn}�^�[���ځF���̕��͎����͂��Ă�������");
            Debug.Log("���̎�����:" + string.Join(",", state.gote.GetMochigoma()));
        }*/
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
        if (op.koma > 0) {
            state.sente.RemoveKoma(op.koma);
        }
        else {
            state.gote.RemoveKoma(op.koma);
        }
        // ���ƌ��̎��������̃��O���b�Z�[�W�Ƃ��ĕ\��
        string senteMochigoma = "Current Sente Mochigoma: " + string.Join(", ", state.sente.GetMochigoma());
        string goteMochigoma = "Current Gote Mochigoma: " + string.Join(", ", state.gote.GetMochigoma());
        //Debug.Log(senteMochigoma + "\n" + goteMochigoma);
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
    List<Operator> GetMochigomaOperators(State state, bool isMaximizingPlayer) {
        List<Operator> operators = new List<Operator>();

        // �v���C���[�̎�����̃��X�g���擾
        List<int> mochigoma = isMaximizingPlayer ? state.gote.GetMochigoma() : state.sente.GetMochigoma();

        // �Ֆʂ̏�Ԃ��擾
        List<List<int>> board = state.banmen.GetBanmen();

        // ��������󂫃}�X�܂��͕�����}�X�ɒu������𐶐�
        foreach (var koma in mochigoma) {
            for (int pos = 0; pos < board.Count; pos++) {
                List<int> cell = board[pos];
                int targetPiece = cell[cell.Count - 1]; // �Ō�̗v�f�����݂̋�̃T�C�Y

                if (targetPiece == 0 || CanCoverPiece(koma, targetPiece)) {
                    operators.Add(new Operator(null, pos, koma));
                }
            }
        }
        return operators;
    }

    bool CanCoverPiece(int piece, int targetPiece) {
        // ��̃T�C�Y���r���āA�����邩�ǂ����𔻒�
        return Math.Abs(piece) > Math.Abs(targetPiece);
    }

    List<Operator> GetBoardOperators(State state, bool isMaximizingPlayer) {
        List<Operator> operators = new List<Operator>();

        // �v���C���[�̋�̃��X�g���擾
        List<int> playerPieces = isMaximizingPlayer ? state.gote.GetMochigoma() : state.sente.GetMochigoma();

        // �Ֆʂ̏�Ԃ��擾
        List<List<int>> board = state.banmen.GetBanmen();

        // �Ֆʏ�̋�𓮂�������𐶐�
        for (int pos = 0; pos < board.Count; pos++) {
            List<int> cell = board[pos];
            int piece = cell[cell.Count - 1]; // �Ō�̗v�f�����݂̋�̃T�C�Y

            if (playerPieces.Contains(piece)) {
                var possibleMoves = GetPossibleMovesForPiece(piece, pos, state);
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
        int targetPiece = targetStack[targetStack.Count - 1];
        if (targetPiece == 0 || CanCoverPiece(op.koma, targetPiece)) {
            return true;
        }
        return false;

        /*// �ړ��悪�󂫃}�X�܂��͕������ł���ꍇ
        if (targetStack.Count == 0 || CanCoverPiece(op.koma, targetStack[targetStack.Count - 1])) {
            //��������R�����g�A�E�g
            //Debug.Log("Valid move: targetStack = " + (targetStack.Count == 0 ? "empty" : targetStack[targetStack.Count - 1].ToString()) + ", op.koma = " + op.koma);
            return true;
        }
        else {
            //��������R�����g�A�E�g
            //Debug.Log("Invalid move: targetStack = " + targetStack[targetStack.Count - 1] + ", op.koma = " + op.koma);
            return false;
        }*/
    }

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
    /*
    //AI�����[�`�̐����v�Z����֐�
    public (int senteReachCount, int goteReachCount) CountReach(State currentState) {
        int senteReachCount = 0;
        int goteReachCount = 0;

        // ���̏�Ԃ�senteArray��goteArray���擾
        var (senteArray, goteArray) = CreateBinaryArrays(currentState);

        // senteArray�̃��[�`�̐����v�Z
        // �c�A���̃��[�`���`�F�b�N
        for (int i = 0; i < 3; i++) {
            int[,] horizontalPositions = { { i, 0 }, { i, 1 }, { i, 2 } };
            int[,] verticalPositions = { { 0, i }, { 1, i }, { 2, i } };

            if (IsReach(senteArray[i, 0], senteArray[i, 1], senteArray[i, 2], currentState, horizontalPositions) ||
                IsReach(senteArray[0, i], senteArray[1, i], senteArray[2, i], currentState, verticalPositions)) {
                senteReachCount++;
            }

            if (IsReach(goteArray[i, 0], goteArray[i, 1], goteArray[i, 2], currentState, horizontalPositions) ||
                IsReach(goteArray[0, i], goteArray[1, i], goteArray[2, i], currentState, verticalPositions)) {
                goteReachCount++;
            }
        }

        // �΂߂̃��[�`���`�F�b�N
        int[,] diagonalPositions1 = { { 0, 0 }, { 1, 1 }, { 2, 2 } };
        int[,] diagonalPositions2 = { { 0, 2 }, { 1, 1 }, { 2, 0 } };

        if (IsReach(senteArray[0, 0], senteArray[1, 1], senteArray[2, 2], currentState, diagonalPositions1) ||
            IsReach(senteArray[0, 2], senteArray[1, 1], senteArray[2, 0], currentState, diagonalPositions2)) {
            senteReachCount++;
        }

        if (IsReach(goteArray[0, 0], goteArray[1, 1], goteArray[2, 2], currentState, diagonalPositions1) ||
            IsReach(goteArray[0, 2], goteArray[1, 1], goteArray[2, 0], currentState, diagonalPositions2)) {
            goteReachCount++;
        }

        // ���ƌ��̃��[�`�̐���Ԃ�
        return (senteReachCount, goteReachCount);
    }

    */



    /*//3�̂���2�����̋�i1�j�ŁA�c��1���󂫁i0�j�܂��͐��̋�i3�����j�ł���ꍇ�����[�`�Ƃ݂Ȃ��܂�
    public bool IsReach(int a, int b, int c, State state, int[,] positions) {

        int playerPieceCount = 0;
        int otherCount = 0;

        // 3�̈ʒu�̒l���擾
        int[] values = { a, b, c };

        for (int i = 0; i < 3; i++) {
            int pos = values[i];

            if (pos == 1) {
                playerPieceCount++;
            }
            else if (pos != 1 && Math.Abs(pos) < 3) {
                otherCount++;
            }
        }

        // ���[�`�̏����𖞂������ǂ����𔻒�
        return playerPieceCount == 2 && otherCount == 1;
    }*/

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

        // currentState��null�łȂ����Ƃ��m�F
        if (currentState == null) {
            Debug.LogError("currentState is null");
            return false;
        }

        // op��null�łȂ����Ƃ��m�F
        if (op == null) {
            Debug.LogError("op is null");
            return false;
        }
        //currentState�̔Ֆʂ̏�Ԃ��ꎞ�ϐ��Ɋi�[
        List<List<int>> banmen = currentState.banmen.GetBanmen();
        // banmen��null�łȂ����Ƃ��m�F
        if (banmen == null) {
            Debug.LogError("banmen is null");
            return false;
        }

        // targetPos���L���ȃC���f�b�N�X�ł��邱�Ƃ��m�F
        if (op.targetPos < 0 || op.targetPos >= banmen.Count) {
            Debug.LogError("targetPos is out of range");
            return false;
        }

        //op.targetPos�̃��X�g���擾
        List<int> targetPosKomas = banmen[op.targetPos];
        // targetPosKomas��null�łȂ����Ƃ��m�F
        if (targetPosKomas == null) {
            Debug.LogError("targetPosKomas is null");
            return false;
        }
        //op.targetPos�̃��X�g�̍Ō�̗v�f���擾
        int lastElement = targetPosKomas.Count > 0 ? targetPosKomas[targetPosKomas.Count - 1] : 0;
        //op.koma��lastElement�̐�Βl���r���āAop.koma��lastElement�ɔ킹�邱�Ƃ��ł��邩�ǂ����𔻒�
        return Math.Abs(op.koma) > Math.Abs(lastElement);
    }

    //AI�����̎肪�v���C���[�̃��[�`��ׂ��������v�Z����֐�
    //����������Ɩ��ʂ�����
    public int CountBlockedReaches(State parentState, State currentState) {
        // parentState�̐��̃��[�`�����J�E���g
        var (parentSenteReachCount, _) = CountReach(parentState);

        // currentState�̐��̃��[�`�����J�E���g
        var (currentSenteReachCount, _) = CountReach(currentState);

        // �ׂ������̃��[�`�̐����v�Z
        //
        int blockedSenteReaches = parentSenteReachCount - currentSenteReachCount;

        // �ׂ������̃��[�`����Ԃ�
        return blockedSenteReaches;
    }

    int GetPositionScore(int position) {
        switch (position) {
            case 0:
            case 2:
            case 6:
            case 8:
                return 100; // �l��
            case 4:
                return 200; // ����
            case 1:
            case 3:
            case 5:
            case 7:
                return 50;  // ���̑�
            default:
                return 0;   // �����Ȉʒu
        }
    }


    Node getNext(State state, int depth) {
        // ���[�g�m�[�h�����݂̏�Ԃŏ�����
        Node root = new Node(state, null, null);
        // �����^�[����AI�v���C���[
        bool isMaximizingPlayer = state.turn % 2 == 0;
        // �~�j�}�b�N�X�A���S���Y�����g�p���čœK�Ȏ��T��
        int bestValue = Minimax(root, depth, isMaximizingPlayer);
        Debug.Log("getNext: Minimax completed with bestValue = " + bestValue);

        // op��null�łȂ����Ƃ��m�F
        /*if (root.op == null) {
            Debug.LogError("op is null in getNext after Minimax");
            return null;
        }*/

        Node bestMove = null;
        int bestEval = int.MinValue; // �œK�ȕ]���l��������

        // �q�m�[�h�����ׂĒ��ׂčœK�Ȏ��������
        foreach (Node child in root.children) {
            if (child.eval > bestEval) {
                bestEval = child.eval;
                bestMove = child;
            }
        }

        if (bestMove == null) {
            Debug.LogError("getNext: bestMove is null");
        }
        else if (bestMove.op == null) {
            Debug.LogError("getNext: bestMove.op is null");
        }

        // �œK�Ȏ肪���������ꍇ�A���̃m�[�h��Ԃ�
        return bestMove != null ? bestMove : root;
    }

    int Minimax(Node node, int depth, bool isMaximizingPlayer) {
        // �T���̐[����0�܂��̓Q�[�����I�����Ă���ꍇ�A�]���l��Ԃ�
        if (depth == 0 || IsGameOver(node.state)) {
            node.eval = EvaluateState(node); // ���̃m�[�h�̕]���l��]���֐�����v�Z
            //Debug.Log("Minimax (depth 0 or game over): " + node.eval); // �f�o�b�O���O��ǉ�
            return node.eval;
        }

        // �GAI�����_�ő剻�v���C���[�̏ꍇ
        if (isMaximizingPlayer) {
            int maxEval = int.MinValue;
            var possibleMoves = GetPossibleMoves(node.state, isMaximizingPlayer);
            //Debug.Log("Possible moves (maximizing): " + possibleMoves.Count); // �f�o�b�O���O��ǉ�

            // ���ׂẲ\�Ȏ��̏�Ԃ𐶐�
            foreach ((State childState, Operator op) in possibleMoves) {
                if (op == null) {
                    Debug.LogError("op is null in Minimax for maximizing player");
                    continue;
                }
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
            //Debug.Log("Minimax (maximizing): " + node.eval); // �f�o�b�O���O��ǉ�
            return maxEval;
        }
        // ���_�ŏ����v���C���[�̏ꍇ
        else {
            int minEval = int.MaxValue;
            var possibleMoves = GetPossibleMoves(node.state, isMaximizingPlayer);
            //Debug.Log("Possible moves (minimizing): " + possibleMoves.Count); // �f�o�b�O���O��ǉ�

            foreach ((State childState, Operator op) in possibleMoves) {
                if (op == null) {
                    Debug.LogError("op is null in Minimax for minimizing player");
                    continue;
                }
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
            //Debug.Log("Minimax (minimizing): " + node.eval); // �f�o�b�O���O��ǉ�
            return minEval;
        }
    }

    // �Q�[�����I�����Ă��邩�ǂ����𔻒肷��֐�
    bool IsGameOver(State state) {
        return CheckWinner(state) != GameResult.None;
    }

    // ���݂̏�Ԃ���\�Ȃ��ׂĂ̎�𐶐�����֐�
    List<(State, Operator)> GetPossibleMoves(State state, bool isMaximizingPlayer) {
        List<(State, Operator)> possibleMoves = new List<(State, Operator)>();

        // �������u����ꏊ�̃��X�g���擾
        List<int> availablePositions = GetAvailablePositonsList(state);

        if (availablePositions.Count > 0) {
            // �������u����ꍇ
            List<Operator> mochigomaOperators = GetMochigomaOperators(state, isMaximizingPlayer);
            foreach (var op in mochigomaOperators) {
                if (IsValidMove(state, op)) {
                    State newState = Put(state, op);
                    possibleMoves.Add((newState, op));
                }
            }
        }
        else {
            // �Ֆʂ��瓮�����ꍇ
            List<Operator> boardOperators = GetBoardOperators(state, isMaximizingPlayer);
            foreach (var op in boardOperators) {
                if (IsValidMove(state, op)) {
                    State newState = Put(state, op);
                    possibleMoves.Add((newState, op));
                }
            }
        }

        //Debug.Log("GetPossibleMoves: possibleMoves count = " + possibleMoves.Count);
        return possibleMoves;
    }

    // �]���֐�
    int EvaluateState(Node node) {
        State currentState = node.state; // ���݂̏�Ԃ��擾
        State parentState = node.parentState; // �e�m�[�h�̏�Ԃ��擾
        int evaluation = 0;

        GameResult result = CheckWinner(currentState);
        // ���̃r���S���C���������Ă���ꍇ��1000�_�����Z
        if (result == GameResult.GoteWin) {
            evaluation += 1000;
        }
        // ���̃r���S���C���������Ă���ꍇ��-10000�_�����Z
        if (result == GameResult.SenteWin) {
            evaluation -= 10000;
        }
        // op.koma������̏�ɔ킹�Ă���ꍇ�A+150�_
        if (CheckCoveringMove(currentState, node.op)) {
            evaluation += 150;
        }

        // �ׂ����v���C���[�̃��[�`�̐����J�E���g
        int blockedReaches = CountBlockedReaches(parentState, currentState);
        evaluation += blockedReaches * 150;

        // currentState�ɂ�����GAI�̃��[�`���~25�_evaluation�ɉ��Z
        int goteReachCount = CountReach(currentState).goteReachCount;
        evaluation += goteReachCount * 25;

        // �u�����}�X�ڂ��Ƃɓ_����t����
        int positionScore = GetPositionScore(node.op.targetPos);
        evaluation += positionScore;

        // �ŏI�]���l��Ԃ�
        //Debug.Log("Final Evaluation: " + evaluation);
        node.eval = evaluation; // ������ǉ�
        return evaluation;
    }

    int EvaluateStateWithDebug(Node node) {
        State currentState = node.state; // ���݂̏�Ԃ��擾
        State parentState = node.parentState; // �e�m�[�h�̏�Ԃ��擾
        int evaluation = 0;

        // op��null�łȂ����Ƃ��m�F
        if (node.op == null) {
            Debug.LogError("op is null in EvaluateStateWithDebug");
            return 0;
        }

        GameResult result = CheckWinner(currentState);
        // ���̃r���S���C���������Ă���ꍇ��1000�_�����Z
        if (result == GameResult.GoteWin) {
            evaluation += 1000;
            Debug.Log("GoteWin: +1000, Evaluation: " + evaluation);
        }
        // ���̃r���S���C���������Ă���ꍇ��-10000�_�����Z
        if (result == GameResult.SenteWin) {
            evaluation -= 10000;
            Debug.Log("SenteWin: -10000, Evaluation: " + evaluation);
        }
        // op.koma������̏�ɔ킹�Ă���ꍇ�A+150�_
        if (CheckCoveringMove(currentState, node.op)) {
            evaluation += 150;
            Debug.Log("CoveringMove: +150, Evaluation: " + evaluation);
        }

        // �ׂ����v���C���[�̃��[�`�̐����J�E���g
        int blockedReaches = CountBlockedReaches(parentState, currentState);
        evaluation += blockedReaches * 150;
        Debug.Log("BlockedReaches: +" + (blockedReaches * 150) + ", Evaluation: " + evaluation);

        // currentState�ɂ�����GAI�̃��[�`���~25�_evaluation�ɉ��Z
        var reachCounts = CountReachWithDebug(currentState);
        int goteReachCount = reachCounts.goteReachCount;
        evaluation += goteReachCount * 25;
        Debug.Log("GoteReachCount: +" + (goteReachCount * 25) + ", Evaluation: " + evaluation);

        // �u�����}�X�ڂ��Ƃɓ_����t����
        int positionScore = GetPositionScore(node.op.targetPos);
        evaluation += positionScore;
        Debug.Log("PositionScore: +" + positionScore + ", Evaluation: " + evaluation);

        // �ŏI�]���l��Ԃ�
        Debug.Log("Final Evaluation: " + evaluation);
        node.eval = evaluation; // ������ǉ�
        return evaluation;
    }

    /*public (int senteReachCount, int goteReachCount) CountReachWithDebug(State currentState) {
        int senteReachCount = 0;
        int goteReachCount = 0;

        // ���̏�Ԃ�senteArray��goteArray���擾
        var (senteArray, goteArray) = CreateBinaryArrays(currentState);

        // senteArray�̃��[�`�̐����v�Z
        // �c�A���̃��[�`���`�F�b�N
        for (int i = 0; i < 3; i++) {
            int[,] horizontalPositions = { { i, 0 }, { i, 1 }, { i, 2 } };
            int[,] verticalPositions = { { 0, i }, { 1, i }, { 2, i } };

            if (IsReach(senteArray[i, 0], senteArray[i, 1], senteArray[i, 2], currentState, horizontalPositions)) {
                senteReachCount++;
                Debug.Log($"Sente reach found at row {i}");
            }
            if (IsReach(senteArray[0, i], senteArray[1, i], senteArray[2, i], currentState, verticalPositions)) {
                senteReachCount++;
                Debug.Log($"Sente reach found at column {i}");
            }

            if (IsReach(goteArray[i, 0], goteArray[i, 1], goteArray[i, 2], currentState, horizontalPositions)) {
                goteReachCount++;
                Debug.Log($"Gote reach found at row {i}");
            }
            if (IsReach(goteArray[0, i], goteArray[1, i], goteArray[2, i], currentState, verticalPositions)) {
                goteReachCount++;
                Debug.Log($"Gote reach found at column {i}");
            }
        }

        // �΂߂̃��[�`���`�F�b�N
        int[,] diagonalPositions1 = { { 0, 0 }, { 1, 1 }, { 2, 2 } };
        int[,] diagonalPositions2 = { { 0, 2 }, { 1, 1 }, { 2, 0 } };

        if (IsReach(senteArray[0, 0], senteArray[1, 1], senteArray[2, 2], currentState, diagonalPositions1)) {
            senteReachCount++;
            Debug.Log("Sente reach found at diagonal 1");
        }
        if (IsReach(senteArray[0, 2], senteArray[1, 1], senteArray[2, 0], currentState, diagonalPositions2)) {
            senteReachCount++;
            Debug.Log("Sente reach found at diagonal 2");
        }

        if (IsReach(goteArray[0, 0], goteArray[1, 1], goteArray[2, 2], currentState, diagonalPositions1)) {
            goteReachCount++;
            Debug.Log("Gote reach found at diagonal 1");
        }
        if (IsReach(goteArray[0, 2], goteArray[1, 1], goteArray[2, 0], currentState, diagonalPositions2)) {
            goteReachCount++;
            Debug.Log("Gote reach found at diagonal 2");
        }

        // ���ƌ��̃��[�`�̐���Ԃ�
        return (senteReachCount, goteReachCount);
    }*/

    public (int senteReachCount, int goteReachCount) CountReachWithDebug(State currentState) {
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
                Debug.Log($"Sente reach found at positions: {string.Join(", ", positions)}");
            }
            if (IsReach(-1, positions, currentState)) {
                goteReachCount++;
                Debug.Log($"Gote reach found at positions: {string.Join(", ", positions)}");
            }
        }

        return (senteReachCount, goteReachCount);
    }



}
