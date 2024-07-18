using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static NewBehaviourScript;

public class NewBehaviourScript : MonoBehaviour
{
    // �Q�[�����I���������ǂ�����\��bool�^�̕ϐ�
    bool isGameOver = false;
    State state;
    Operator op;
    int[] lastElementsArray; // �T�C�Y9�̔z����`
    InputField inputSourcePosField; //sourcePos�̓��͂��i�[���邽�߂̕ϐ�
    InputField inputTargetPosField; //targetPos�̓��͂��i�[���邽�߂̕ϐ�
    InputField inputKomaField; //koma�̓��͂��i�[���邽�߂̕ϐ�
    List<int> availablePositionsList; // ���u����ꏊ�̃��X�g

    //Unity�ŋ���i�[���邽�߂̕ϐ�


    public enum GameResult {
        None,      // �N�������Ă��Ȃ�
        SenteWin,  // ���̏���
        GoteWin  // ���̏���
    }

    public class Banmen {
        // �����̔z��̐ݒ�
        List<List<int>> banmen = new List<List<int>>();
        
        //�R���X�g���N�^
        public Banmen() {
            for (int i = 0; i < 9; i++) {
                banmen.Add(new List<int> { 0 });
            }
        }

        public List<List<int>> GetBanmen() {
            return banmen;
        }
    }

    public class Mochigoma {
        //���OmochigomaList�ɂ���ׂ��H�H
        private List<int> mochigoma;//�莝���̋�
        //�R���X�g���N�^
        public Mochigoma(int koma1, int koma2, int koma3, int koma4, int koma5, int koma6) {
            this.mochigoma = new List<int> { koma1, koma2, koma3, koma4, koma5, koma6 };
        }
        //�Q�b�^�[
        public List<int> GetMochigoma() {
            return mochigoma;
        }
        // ���X�g�ɋ��ǉ����郁�\�b�h�i�K�v�ɉ����Ēǉ��j
        public void AddKoma(int koma) {
            mochigoma.Add(koma);
        }

        // ���X�g�������폜���郁�\�b�h�i�K�v�ɉ����Ēǉ��j
        public bool RemoveKoma(int koma) {
            return mochigoma.Remove(koma);
        }
    }

    public class State {
        public Banmen banmen;
        public Mochigoma sente;
        public Mochigoma gote;
        public int turn;//�o�߃^�[����

        public State(Banmen banmen, Mochigoma sente, Mochigoma gote) {
            this.banmen = new Banmen();
            this.sente = new Mochigoma(3,3,2,2,1,1);
            this.gote = new Mochigoma(-3,-3,-2,-2,-1,-1);
            this.turn = 1;
        }

        //turn���v���X1���郁�\�b�h
        public void NextTurn() {
            turn++;
        }
    }

    public class Operator {
        List<int> availablePositonsList;  //���u����Ƃ���
        public int sourcePos;//�ǂ�����
        public int targetPos;// �ǂ���
        public int koma;//�ǂ̃T�C�Y��

        //�������u����Ƃ��낪����ꍇ�̃R���X�g���N�^
        public Operator(List<int> availablePositonsList, int targetPos, int koma) {
            this.availablePositonsList = availablePositonsList;
            this.targetPos = targetPos;
            this.koma = koma;
        }

        //�������u����Ƃ��낪�Ȃ��ꍇ�̃R���X�g���N�^
        public Operator(List<int> availablePositonsList, int sourcePos, int targetPos, int koma) {
            this.availablePositonsList = availablePositonsList;
            this.sourcePos = sourcePos;
            this.targetPos = targetPos;
            this.koma = koma;
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




    void Start() {

        //State�N���X�̃C���X�^���X�𐶐�
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        //lastElement���i�[���邽�߂̃T�C�Y9�̔z����`
        lastElementsArray = new int[9];
        
        inputSourcePosField = GameObject.Find("InputSourcePosField").GetComponent<InputField>();
        inputTargetPosField = GameObject.Find("InputTargetPosField").GetComponent<InputField>();
        inputKomaField = GameObject.Find("InputKomaField").GetComponent<InputField>();

        availablePositionsList = GetAvailablePositonsList(state);

    }

    void Update() {

    }
    
    //���u���֐�
    State Put(State state, Operator op) {

        //��Βl���r���āAop.koma��op.targetPos�ɒu���邩�ǂ����𔻒�
        if(lastElementsArray[op.targetPos] < Math.Abs(op.koma)) {
            // �Ֆʂ̏�Ԃ��擾
            List<List<int>> banmen = state.banmen.GetBanmen();
            // �ڕW�ʒu�̋�X�g���擾
            List<int> targetPosKomas = banmen[op.targetPos];
            // �ڕW�ʒu�ɋ��u��
            targetPosKomas.Add(op.koma);
            //lastElementsArray�ɉ�����
            lastElementsArray[op.targetPos] = op.koma;//�X�V

            //������̍X�V
            if (state.turn % 2 == 1) {//���̏ꍇ
                for (int i = 0; i < state.sente.GetMochigoma().Count; i++) {
                    if (state.sente.GetMochigoma()[i] == op.koma) {
                        //
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
            //�^�[����i�߂�
            state.NextTurn();
        }
        else {
            Debug.Log("���̋�͒u���܂���");
        }
        return state;
    }

    //���̓t�H�[������e�L�X�g�����擾����֐�
    public int GetInputField(InputField inputField) {
        // InputField����e�L�X�g�����擾���A���͂̑O��̋󔒂��폜
        string inputText = inputField.text.Trim(); 

        // ���͂���̏ꍇ�̏���
        if (string.IsNullOrEmpty(inputText)) {
            Debug.LogError("���͂���ł��B�l����͂��Ă��������B");
            return 0; // �܂��͓K�؂ȃG���[�l
        }

        // inputText��int�^�Ɉ��S�ɕϊ����鎎��
        if (int.TryParse(inputText, out int inputNumber)) {
            inputField.text = "";
            return inputNumber;
        }
        else {
            // �ϊ��Ɏ��s�����ꍇ�̏����i�G���[���b�Z�[�W�̕\���Ȃǁj
            Debug.LogError("���͂��ꂽ�l�������Ƃ��ĉ��߂ł��܂���: " + inputText);
            return 0; // �܂��͓K�؂ȃG���[�l
        }
    }


    //�{�^���������ꂽ���ɌĂяo�����֐�
    public void OnClickButton() {
        // ���u����ꏊ�̃��X�g���v�Z

        //int inputSourcePos;

        // ���̓t�H�[������targetPos��koma���擾
        int inputTargetPos = GetInputField(inputTargetPosField);
        int inputKoma = GetInputField(inputKomaField);

        // ���݂̃^�[���Ɋ�Â��āA���܂��͌��̎�������擾
        List<int> currentMochigoma = state.turn % 2 == 1 ? state.sente.GetMochigoma() : state.gote.GetMochigoma();

        // currentMochigoma�̒��ŁA�Ֆʂɒu����ꏊ�����邩�ǂ�����bool�^�Ŕ��肷�鏈��
        //������X�g�̍ŏ��̐�Βl���A�Ֆʂ̍ŏ��̐�Βl�ȉ��̏ꍇ�͔Ֆʂ��瓮���������Ȃ�
        // �Ֆʂ��瓮�����ꍇ�A��������グ���i�K�ŏ�����������ށB���̂Ƃ�������Ȃ��A�������́A�A

        if (currentMochigoma.Count == 0 ) {
            int inputSourcePos = GetInputField(inputSourcePosField);
            op = new Operator(availablePositionsList, inputSourcePos, inputTargetPos, inputKoma);
            
            // �Ֆʂ��瓮�����\��̋��������X�g�ɉ�����
            if (state.turn % 2 == 1) { // ���̏ꍇ
                state.sente.AddKoma(inputKoma);
            }
            else { // ���̏ꍇ
                state.gote.AddKoma(inputKoma);
            }

            // ��������グ��O�̏�������
            GameResult preMoveResult = CheckWinner(state);
            if (preMoveResult != GameResult.None) {
                Debug.Log($"��������̌���: {preMoveResult}");
                return; // ���s�����܂����ꍇ�͂����ŏ������I��
            }

            // ��ԕ\��state�ƃI�y���[�^op���g���ď�ԑJ�ڊ֐�Put�����s
            Put(state, op);
        }
        else {
            // �������u����ꍇ
            op = new Operator(availablePositionsList, inputTargetPos, inputKoma);
            Put(state, op);
        }

        // ���u������̏�������
        GameResult postMoveResult = CheckWinner(state);
        if (postMoveResult != GameResult.None) {
            Debug.Log($"��������̌���: {postMoveResult}");
            return; // ���s�����܂����ꍇ�͂����ŏ������I��
        }

        /*
        //�������u����Ƃ��낪����ꍇ
        if (currentMochigoma.Count != 0) {
            op = new Operator(availablePositionsList, inputTargetPos, inputKoma);
        }
        //�������u����Ƃ��낪�Ȃ��ꍇ(�܂�Ֆʂ��瓮�����ꍇ)
        else {
            //InputSourcePos��active�ɂ���

            int inputSourcePos = GetInputField(inputSourcePosField);
            op = new Operator(availablePositionsList, inputSourcePos, inputTargetPos, inputKoma);

            // �Ֆʂ��瓮�����\��̋��������X�g�ɉ�����
            if (state.turn % 2 == 1) { // ���̏ꍇ
                state.sente.AddKoma(inputKoma);
            }
            else { // ���̏ꍇ
                state.gote.AddKoma(inputKoma);
            }
            //InputSourcePos��inactive�ɂ���

            // ���݂̔Ֆʂ̏�Ԃ����O�ɏo��
            PrintCurrentBanmen(state);

            // ���u������̏�������
            GameResult postMoveResult = CheckWinner(state);
            if (postMoveResult != GameResult.None) {
                Debug.Log($"��������̌���: {postMoveResult}");
                return; // ���s�����܂����ꍇ�͂����ŏ������I��
            }
        }*/

        // ���u����ꏊ�̃��X�g���Čv�Z���ĕ\��
        availablePositionsList = GetAvailablePositonsList(state);
        //Debug.Log("�Čv�Z���ꂽ�u����ꏊ: " + string.Join(", ", availablePositionsList));
    }

    //����������s���֐�
    public GameResult CheckWinner(State state) {
        int[,] senteArray = new int[3, 3];
        int[,] goteArray = new int[3, 3];
        // �Ֆʂ̏�Ԃ��ꎞ�ϐ��Ɋi�[
        List<List<int>> banmen = state.banmen.GetBanmen();

        // �Ֆʂ̏�Ԃ���͂���senteArray��goteArray���X�V
        for (int i = 0; i < banmen.Count; i++) {
            //banmen[i][banmen[i].Count - 1](�e�}�X�̍Ō�̗v�f)��0�łȂ��Ȃ�lastElement�ɂ��̂܂ܑ���A0�Ȃ�0����
            int lastElement = banmen[i][banmen[i].Count - 1] != 0 ? banmen[i][banmen[i].Count - 1] : 0;

            if (lastElement > 0) {
                senteArray[i / 3, i % 3] = 1;
            }
            else if (lastElement < 0) {
                goteArray[i / 3, i % 3] = 1;
            }
        }

        // ���������̃`�F�b�N
        if (HasWinningLine(senteArray)) {
            string banmenOutput = "";
            for (int row = 0; row < 3; row++) {
                for (int col = 0; col < 3; col++) {
                    banmenOutput += senteArray[row, col].ToString() + " ";
                }
                banmenOutput += "\n";
            }
            Debug.Log($"�����Ֆ�:\n{banmenOutput}");
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
            return GameResult.GoteWin;
        }

        return GameResult.None;
    }

    //
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

    // currentMochigoma�̒��ŁA�Ֆʂɒu����ꏊ�����邩�ǂ�����bool�^�Ŕ��肷�鏈��
    bool CanPlaceFromMochigoma(List<int> currentMochigoma, List<int> availablePositionsList) {
        // �Ֆʂɒu����ꏊ���Ȃ��A�܂��͎�����Ȃ��ꍇ�Afalse��Ԃ�
        if (availablePositionsList.Count == 0 || currentMochigoma.Count == 0) {
            return false;
        }

        // ������̒��ŁA�Ֆʂɒu�������邩�ǂ������`�F�b�N
        foreach (int koma in currentMochigoma) {
            if (koma != 0) { // ������X�g�ɋ���݂���i0�ȊO�j
                             // �Ֆʂɒu����ꏊ������ꍇ�Atrue��Ԃ�
                return true;
            }
        }

        // ������̒��ɔՖʂɒu�����Ȃ��ꍇ�Afalse��Ԃ�
        return false;
    }


    //�Ֆʂ��X�V����֐�

}
