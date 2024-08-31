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
    Operator op;
    int[] lastElementsArray; //�Ֆʂ̊e�}�X�̍Ō�̗v�f���i�[����z��
    private GameObject selectedKoma;
    private Vector3 originalPosition;
    [SerializeField]
    private LayerMask komaLayer; // Koma�p�̃��C���[�}�X�N
    [SerializeField]
    private LayerMask positionLayer;  // Position�p�̃��C���[�}�X�N
    [SerializeField]
    private GameObject[] goteKomas;
    [SerializeField]
    private Text resultText;

    // �Q�[���̌��ʂ�\���񋓌^
    public enum GameResult {
        None,
        SenteWin,
        GoteWin
    }

    void Start() {
        isGameOver = false;
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        lastElementsArray = new int[9];
        GetAvailablePositonsList(state);

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
            if (Input.GetMouseButtonDown(0)) {
                HandlePieceSelection();
            }
            if (Input.GetMouseButton(0) && selectedKoma != null) {
                FollowCursor();
            }
            if (Input.GetMouseButtonUp(0) && selectedKoma != null) {
                HandlePieceDrop();
            }
        } else {
            HandleAITurn();
        }
    }

    void HandleAITurn() {
        GetAvailablePositonsList(state);
        Node newNode = getNext(state, 3);

        if (newNode != null && newNode.op != null) {
            MoveAIPiece(newNode);
        }
        else {
            Debug.LogError("HandleAITurn: bestMove or bestMove.op is null");
        }

        Debug.Log("�]���l: " + newNode.eval);
        ApplyMove(newNode.state);
        UpdateMochigoma(state, newNode.op);
        PrintCurrentBanmen(state);

        //��������
        GameResult postMoveResult = CheckWinner(state);
        if (postMoveResult != GameResult.None) {
            Debug.Log($"��������: {postMoveResult}");
            resultText.text = postMoveResult.ToString();
            PrintCurrentBanmen(state);
            isGameOver = true;
        }
    }

    // �h���b�O�J�n���ɋ��I������֐�
    void HandlePieceSelection() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 5.0f);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, komaLayer)) {
            selectedKoma = hit.collider.gameObject;

            originalPosition = selectedKoma.transform.position;
            Koma selectedKomaComponent = selectedKoma.GetComponent<Koma>();

            int currentPlayer = state.turn % 2 == 1 ? 1 : -1;

            Mochigoma currentPlayerMochigoma = state.turn % 2 == 1 ? state.sente : state.gote;
            int komaSize = 0;
            int komaPos = -1;

            if (selectedKomaComponent.player != currentPlayer) {
                Debug.Log("���̋�͌��݂̃v���C���[�̂��̂ł͂���܂���");
                selectedKoma = null;
            }
  
            komaSize = selectedKoma.GetComponent<Koma>().size;
            komaPos = selectedKoma.GetComponent<Koma>().pos;

            bool canPlaceFromMochigoma = GetAvailablePositonsList(state).Count > 0;

            // �I��������Ֆʂɂ���A�������u����ꏊ������ꍇ
            if (komaPos != -1 && canPlaceFromMochigoma) {
                Debug.Log("�������u����ꏊ�����邽�߁A�Ֆʂ̋�͑I���ł��܂���");
                selectedKoma = null;
                return;
            }
            // �I�������������ŁA�������u����ꏊ���Ȃ��ꍇ
            else if (komaPos == -1 && !canPlaceFromMochigoma) {
                Debug.Log("�������u����ꏊ���Ȃ����߁A������͑I���ł��܂���");
                selectedKoma = null;
                return;
            }
            // �I�������������ŁA�������u����ꏊ������ꍇ
            else if(komaPos == -1 && canPlaceFromMochigoma) {
                return;
            }

            // �I��������Ֆʂɂ���A�������u����ꏊ���Ȃ��ꍇ
            Debug.Log("�I���������������ɒǉ����܂�");
            currentPlayerMochigoma.AddKoma(komaSize);
            List<List<int>> banmen = state.banmen.GetBanmen();
            banmen[komaPos].RemoveAt(banmen[komaPos].Count - 1);

            //��������
            GameResult postMoveResult = CheckWinner(state);
            if (postMoveResult != GameResult.None) {
                Debug.Log($"��������: {postMoveResult}");
                resultText.text = postMoveResult.ToString();
                PrintCurrentBanmen(state);
                isGameOver = true;
            }
        }
    }

    // �}�E�X�J�[�\���̈ʒu�ɋ��Ǐ]������֐�
    void FollowCursor() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Vector3 mousePosition = Input.mousePosition;
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

    // �}�E�X�𗣂������ɋ��u���֐�
    void HandlePieceDrop() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.blue, 5.0f);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, positionLayer)) {
            GetAvailablePositonsList(state);
            PlaceSelectedKomaOnPosition(hit);
            UpdateMochigoma(state, op);
            Debug.Log("�^�[����: " + state.turn);
        }
        selectedKoma = null;
    }

    //���u����̓I�ȏ������s���֐�
    void PlaceSelectedKomaOnPosition(RaycastHit hit) {

        int komaSize = selectedKoma.GetComponent<Koma>().size;
        int komaPos = selectedKoma.GetComponent<Koma>().pos;

        Position positionComponent = hit.collider.gameObject.GetComponent<Position>();
        if (positionComponent == null) {
            Debug.LogWarning("Position component not found on the hit object.");
            return;
        }
        int positionNumber = positionComponent.Number;

        Mochigoma currentPlayerMochigoma = state.turn % 2 == 1 ? state.sente : state.gote;

        //�ړ��O�ƌオ�����ʒu�Ȃ�A������̈ʒu�ɖ߂��A�ړ������͍s��Ȃ�
        if (positionNumber == komaPos && komaPos != -1) {
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
                    banmen[komaPos].Add(komaSize);
                }
            }
            else {
                Debug.LogError("komaPos is out of range: " + komaPos);
            }
            return;
        }
        //�ړ���ɂ��傫�������Ȃ�A���u�����ɏ������I��
        if (Math.Abs(lastElementsArray[positionNumber]) >= Math.Abs(komaSize)) {

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

        op = new Operator(komaPos, positionNumber, komaSize);

        State newState = Put(state, op);
        ApplyMove(newState);

        //��������
        GameResult preMoveResult = CheckWinner(state);
        if (preMoveResult != GameResult.None) {
            Debug.Log($"��������: {preMoveResult}");
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

        // �AI�̂��̂ł��邱�Ƃ��m�F
        Koma komaComponent = koma.GetComponent<Koma>();
        if (komaComponent.player != -1) {
            Debug.LogError("Koma does not belong to AI.");
            return;
        }

        // �}�X�̃I�u�W�F�N�g������
        GameObject positionObject = GameObject.Find("Position (" + positionNumber + ")");
        if (positionObject == null) {
            Debug.LogError("Position object not found: Position (" + positionNumber + ")");
            return;
        }

        // sourceNumber���Ֆʂ͈͓̔��ł���ꍇ�ɂ̂݁A����폜���A���̋����
        if (sourceNumber >= 0 && sourceNumber < 9) {
            // sourceNumber�̈ʒu�������폜���邱�Ƃ����O�ɏo��
            Debug.Log("Removing Koma from sourceNumber: " + sourceNumber);
            GetAvailablePositonsList(state);//�u����Ƃ��냊�X�g�X�V�̂��߂ɂ�����
            //banmen��sourceNumber�s�̍Ō���̋���폜
            banmen[sourceNumber].RemoveAt(banmen[sourceNumber].Count - 1);
        }

        // ����}�X�̏�ɔz�u���鏈���i��̒�ʂ��}�X�̏�ʂɗ���悤�ɒ����j
        float komaHeight = koma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = positionObject.transform.position;
        newPosition.y += komaHeight / 2;
        koma.transform.position = newPosition;

        komaComponent.pos = positionNumber; // ��z�u���ꂽ�V�����ʒu�ɍX�V

        lastElementsArray[positionNumber] = komaSize;
        GetAvailablePositonsList(state);

        // state�̔Ֆʏ����X�V
        banmen[positionNumber].Add(komaSize);
        newNode.state.banmen.SetBanmen(banmen);

    }

    GameObject FindKoma(int size, int sourcePos) {
        // goteKomas �z����̂��ׂĂ� GameObject ���`�F�b�N
        foreach (GameObject komaObject in goteKomas) {
            Koma koma = komaObject.GetComponent<Koma>();
            //koma.pos��sourcePos�Akoma.size��size��S�ă��O�ɏo��
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
            if (banmen[i].Count <= 0) {
                Debug.LogError($"banmen[{i}] �͋�ł��B");
            }
            int lastElement = banmen[i][banmen[i].Count - 1];
            lastElementsArray[i] = lastElement;

            if (Math.Abs(lastElement) < maxKomaSize) {
                availablePositionsList.Add(i);
            }
        }
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

        // �Ֆʏ�̈ړ��̍ہA����ړ�������폜���鏈����ǉ�
        if (op.sourcePos >= 0 && op.sourcePos < banmen.Count) {
            if (banmen[op.sourcePos].Count > 0) {
                banmen[op.sourcePos].RemoveAt(banmen[op.sourcePos].Count - 1);
            }
            else {
                Debug.LogWarning("No pieces to remove at source position: " + op.sourcePos);
            }
        }
        // ���u������
        banmen[op.targetPos].Add(op.koma);

        //laseElementsArray���A�O���[�o���ϐ�����Ȃ������o�[�ϐ��Ŏ��K�v������?
        //�Ă����ꂢ��H
        //lastElementsArray[op.targetPos] = op.koma;

        newState.NextTurn();

        return newState;
    }

    void ApplyMove(State newState) {
        // ���݂̏�Ԃ�V������ԂɍX�V
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

        // ���݂̔Ֆʂ̏�Ԃ����O�ɏo��
        PrintCurrentBanmen(newState);
        //������lastElementsArray�����O�ɏo��
        Debug.Log("lastElementsArray: " + string.Join(", ", lastElementsArray));
    }

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
        Debug.Log(senteMochigoma + "\n" + goteMochigoma);
    }

    //����������s���֐�
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
                    operators.Add(new Operator(pos, koma));
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
        List<List<int>> banmen = state.banmen.GetBanmen();
        int player = isMaximizingPlayer ? -1 : 1;//�������t������

        // lastElementsArray���X�V
        int[] lastElementsArray = new int[banmen.Count];
        for (int i = 0; i < banmen.Count; i++) {
            if (banmen[i].Count > 0) {
                lastElementsArray[i] = banmen[i][banmen[i].Count - 1];
            }
            else {
                lastElementsArray[i] = 0; // ��̏ꍇ��0��ݒ�
            }
        }

        for (int i = 0; i < lastElementsArray.Length; i++) {
            int piece = lastElementsArray[i];
            if ((player > 0 && piece > 0) || (player < 0 && piece < 0)) {//����̂����H
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


    //��������̂��߂ɁA1�������̋�Ƃ����񎟌��z���Ԃ��֐�
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
        // �����^�[����AI�v���C���[
        bool isMaximizingPlayer = state.turn % 2 == 0;
        int bestValue = Minimax(root, depth, isMaximizingPlayer);

        Node bestMove = null;

        // �q�m�[�h�����ׂĒ��ׂčœK�Ȏ��������
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

        // �œK�Ȏ肪���������ꍇ�A���̃m�[�h��Ԃ�
        return bestMove != null ? bestMove : root;
    }

    int Minimax(Node node, int depth, bool isMaximizingPlayer) {
        // �T���̐[����0�܂��̓Q�[�����I�����Ă���ꍇ�A�]���l��Ԃ�
        if (depth == 0 || IsGameOver(node.state)) {
            node.eval = Evaluate(node); // ���̃m�[�h�̕]���l��]���֐�����v�Z
            return node.eval;
        }

        // �GAI�����_�ő剻�v���C���[�̏ꍇ
        if (isMaximizingPlayer) {
            int maxEval = int.MinValue;
            var possibleMoves = GetPossibleMoves(node.state, isMaximizingPlayer);

            // ���ׂẲ\�Ȏ��̏�Ԃ𐶐�
            foreach ((State childState, Operator op) in possibleMoves) {
                if (op == null) {
                    Debug.LogError("op is null in Minimax for maximizing player");
                    continue;
                }
                Node childNode = new Node(childState, node.state, op);
                node.children.Add(childNode);

                // ���������𖞂����肪���������ꍇ�A���̎�𑦍��ɕԂ�
                if (CheckWinner(childState) == GameResult.GoteWin) {
                    node.eval = Evaluate(childNode);
                    return node.eval;
                }
                // �ċA�I��Minimax���Ăяo���A�]���l���v�Z
                int eval = Minimax(childNode, depth - 1, false);
                maxEval = Math.Max(maxEval, eval);
                
            }
            node.eval = maxEval;
            return maxEval;
        }
        // ���_�ŏ����v���C���[�̏ꍇ
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

                // ���������𖞂����肪���������ꍇ�A���̎�𑦍��ɕԂ�
                if (CheckWinner(childState) == GameResult.SenteWin) {
                    node.eval = Evaluate(childNode);
                    return node.eval;
                }
                // �ċA�I��Minimax���Ăяo���A�]���l���v�Z
                int eval = Minimax(childNode, depth - 1, true);
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
    List<(State, Operator)> GetPossibleMoves(State state, bool isMaximizingPlayer) {//////////
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
        return possibleMoves;
    }

    // �]���֐�
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

        int positionScore = GetPositionScore(node.op.targetPos);
        evaluation += positionScore;

        node.eval = evaluation;
        return evaluation;
    }
}
