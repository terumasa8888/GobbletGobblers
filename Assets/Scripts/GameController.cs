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

    // �Q�[���̌��ʂ�\���񋓌^
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
            Debug.LogError("MouseInputHandler�I�u�W�F�N�g��������܂���ł����B");
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

        Debug.Log("�]���l: " + newNode.Eval());
        ApplyMove(newNode.state);
        UpdateMochigoma(state, newNode.op);

        //��������
        CheckForWin();
    }

    

    void MoveAIPiece(Node newNode) {
        int komaSize = newNode.op.KomaSize();
        int sourceNumber = newNode.op.SourcePos();
        int positionNumber = newNode.op.TargetPos();

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
            state.UpdateAvailablePositionsList();

            //banmen��sourceNumber�s�̍Ō���̋���폜
            banmen[sourceNumber].RemoveAt(banmen[sourceNumber].Count - 1);
        }

        // ����}�X�̏�ɔz�u���鏈���i��̒�ʂ��}�X�̏�ʂɗ���悤�ɒ����j
        float komaHeight = koma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = positionObject.transform.position;
        newPosition.y += komaHeight / 2;
        koma.transform.position = newPosition;

        komaComponent.pos = positionNumber; // ��z�u���ꂽ�V�����ʒu�ɍX�V

        // state�̔Ֆʏ����X�V
        banmen[positionNumber].Add(komaSize);
        newNode.state.banmen.SetBanmen(banmen);
        newNode.state.UpdateAvailablePositionsList();
        newNode.state.UpdateLastElementsArray();
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

    

    //���u������̏�Ԃ𐶐������ԑJ�ڊ֐�
    public State Put(State state, Operator op) {
        // �I�y���[�^���L�����ǂ������`�F�b�N
        if (!IsValidMove(state, op)) {
            throw new InvalidOperationException("Invalid move");
        }
        State newState = new State(state);
        // �I�y���[�^�Ɋ�Â��ċ��u���������s��      
        List<List<int>> banmen = newState.banmen.GetBanmen();

        // �Ֆʏ�̈ړ��̍ہA����ړ�������폜���鏈����ǉ�
        int sourcePos = op.SourcePos();
        if (sourcePos >= 0 && sourcePos < banmen.Count) {
            if (banmen[sourcePos].Count > 0) {
                banmen[sourcePos].RemoveAt(banmen[sourcePos].Count - 1);
            }
            else {
                Debug.LogWarning("No pieces to remove at source position: " + sourcePos);
            }
        }
        // ���u������
        banmen[op.TargetPos()].Add(op.KomaSize());

        newState.NextTurn();

        return newState;
    }

    public State GetState() {
        return state;
    }

    public void ApplyMove(State newState) {
        // ���݂̏�Ԃ�V������ԂɍX�V
        this.state = newState;

        state.UpdateLastElementsArray();
        PrintCurrentBanmen(newState);
        Debug.Log("Turn: " + state.Turn());
    }

    //������̍X�V���s���֐�
    public void UpdateMochigoma(State state, Operator op) {//if���v���t�@�N�^
        int komaSize = op.KomaSize();
        if (komaSize > 0) {
            state.sente.RemoveKoma(komaSize);
        }
        else {
            state.gote.RemoveKoma(komaSize);
        }
        // �f�o�b�O���O��ǉ����āA������̏�Ԃ��m�F
        string senteMochigoma = "Updated Sente Mochigoma: " + string.Join(", ", state.sente.GetMochigoma());
        string goteMochigoma = "Updated Gote Mochigoma: " + string.Join(", ", state.gote.GetMochigoma());
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

    // ���݂̔Ֆʂ̏�Ԃ�3�~3�̓񎟌��z��ɕϊ����A���O�ɏo�͂���֐�
    void PrintCurrentBanmen(State state) {//���i�B�a�����ɂȂ��Ă���
        int[,] currentBanmen = new int[3, 3];
        List<List<int>> banmen = state.banmen.GetBanmen();

        for (int i = 0; i < banmen.Count; i++) {
            int row = i / 3;
            int col = i % 3;
            List<int> stack = banmen[i];
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
        int[] lastElementsArray = new int[banmen.Count];//������lastElementsArray���g���Ă�̂Œ���
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

        List<List<int>> banmen = state.banmen.GetBanmen();
        int targetPos = op.TargetPos();
        if (targetPos < 0 || targetPos >= banmen.Count) {
            Debug.Log("targetPos: " + targetPos);
            Debug.LogError("IndexOutOfRangeException: Index was outside the bounds of the array.");
            return false; // �͈͊O�̏ꍇ�́A�����𒆒f����state�����̂܂ܕԂ�
        }

        List<int> targetStack = banmen[targetPos];
        int targetPiece = targetStack[targetStack.Count - 1];
        if (targetPiece == 0 || CanCoverPiece(op.KomaSize(), targetPiece)) {
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


    //��������̂��߂ɁA�����̋��1�A����ȊO��0�Ƃ����񎟌��z���Ԃ��֐�
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
        bool isMaximizingPlayer = !state.isSenteTurn();
        int bestValue = Minimax(root, depth, isMaximizingPlayer);//����root�ɕύX�������Ă��܂��Ă���

        Node bestMove = null;

        // �q�m�[�h�����ׂĒ��ׂčœK�Ȏ��������
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

        // �œK�Ȏ肪���������ꍇ�A���̃m�[�h��Ԃ�
        return bestMove != null ? bestMove : root;
    }

    int Minimax(Node node, int depth, bool isMaximizingPlayer) {
        // �T���̐[����0�܂��̓Q�[�����I�����Ă���ꍇ�A�]���l��Ԃ�
        if (depth == 0 || IsGameOver(node.state)) {
            int evaluation = Evaluate(node);
            node.SetEval(evaluation);
            return node.Eval();
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
                node.AddChild(childNode);

                // ���������𖞂����肪���������ꍇ�A���̎�𑦍��ɕԂ�
                if (CheckWinner(childState) == GameResult.GoteWin) {
                    int evaluation = Evaluate(childNode);
                    node.SetEval(evaluation);
                    return node.Eval();
                }
                // �ċA�I��Minimax���Ăяo���A�]���l���v�Z
                int childEvaluation = Minimax(childNode, depth - 1, false);
                maxEval = Math.Max(maxEval, childEvaluation);
                
            }
            node.SetEval(maxEval);
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
                node.AddChild(childNode);

                // ���������𖞂����肪���������ꍇ�A���̎�𑦍��ɕԂ�
                if (CheckWinner(childState) == GameResult.SenteWin) {
                    int evaluation = Evaluate(childNode);
                    node.SetEval(evaluation);
                    return node.Eval();
                }
                // �ċA�I��Minimax���Ăяo���A�]���l���v�Z
                int childEvaluation = Minimax(childNode, depth - 1, true);
                minEval = Math.Min(minEval, childEvaluation);
                
            }
            node.SetEval(minEval);
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
        state.UpdateAvailablePositionsList();
        List<int> availablePositions = state.AvailablePositionsList();

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

        int positionScore = GetPositionScore(node.op.TargetPos());
        evaluation += positionScore;

        node.SetEval(evaluation);
        return evaluation;
    }

    // ��������̏��������\�b�h�Ƃ��ĕ���
    public void CheckForWin() {
        GameResult result = CheckWinner(state);
        if (result != GameResult.None) {
            Debug.Log($"��������: {result}");
            resultText.text = result.ToString();
            PrintCurrentBanmen(state);
            isGameOver = true;
        }
    }
}
