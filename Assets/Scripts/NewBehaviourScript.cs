using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    // �Q�[�����I���������ǂ�����\��bool�^�̕ϐ�
    bool isGameOver = false;
    State state;
    Operator op;
    int[] lastElementsArray; // �T�C�Y9�̔z����`
    InputField inputPosField; //pos�̓��͂��i�[���邽�߂̕ϐ�
    InputField inputKomaField; //koma�̓��͂��i�[���邽�߂̕ϐ�


    public class Banmen {
        // �����̔z��̐ݒ�
        List<List<int>> banmen = new List<List<int>>();
        
        //�R���X�g���N�^
        public Banmen() {
            /*for (int i = 0; i < 9; i++) {
                banmen.Add(new List<int> { 0 });
            }*/
            banmen.Add(new List<int> { 0, 1, 2 });
            banmen.Add(new List<int> { 0});
            banmen.Add(new List<int> { 0, 1, 2 });
            banmen.Add(new List<int> { 0, 1, -2 });
            banmen.Add(new List<int> { 0, 1 });
            banmen.Add(new List<int> { 0, 1, -3 });
            banmen.Add(new List<int> { 0, 1, 3 });
            banmen.Add(new List<int> { 0, 3 });
            banmen.Add(new List<int> { 0, -1, -2 });

        }

        public List<List<int>> GetBanmen() {
            return banmen;
        }
    }

    public class Mochigoma {
        private int[] mochigoma;//�莝���̋�

        //�R���X�g���N�^
        public Mochigoma(int koma1, int koma2, int koma3, int koma4, int koma5, int koma6) {
            this.mochigoma = new int[6];
            mochigoma[0] = koma1;
            mochigoma[1] = koma2;
            mochigoma[2] = koma3;
            mochigoma[3] = koma4;
            mochigoma[4] = koma5;
            mochigoma[5] = koma6;
        }
        public int[] GetMochigoma() {
            return mochigoma;
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
    }

    public class Operator {
        //uteru teban;  //���u����Ƃ���
        List<int> availablePositonsList;  //���u����Ƃ���
        public int pos;   // �ǂ���
        public int koma;  // �ǂ̃T�C�Y��

        //�R���X�g���N�^
        public Operator(List<int> availablePositonsList, int pos, int koma) {
            this.availablePositonsList = availablePositonsList;
            this.pos = pos;
            this.koma = koma;
        }

    }

    //���u����ꏊ�̃��X�g���v�Z����֐�
    public List<int> GetAvailablePositonsList(State state) {
        int maxKomaSize = 0;//������̒��Ő�Βl���ł��傫����̃T�C�Y���i�[����int�^�̕ϐ�
        
        List<int> availablePositonsList = new List<int>();

        //if(state.turn % 2 == 1) {
        //state.sente�ɂ����̐�Βl���ł��傫�����̂�maxKomaSize�Ɋi�[
        foreach (int komaSize in state.sente.GetMochigoma()) {
            if (Math.Abs(komaSize) > maxKomaSize) {
                    maxKomaSize = Math.Abs(komaSize);
            }
        }

        for (int i = 0; i < state.banmen.GetBanmen().Count; i++) {
            if (state.banmen.GetBanmen()[i].Count > 0) {
                int lastElement = state.banmen.GetBanmen()[i][state.banmen.GetBanmen()[i].Count - 1];
                //lastElement���i�[���邽�߂̃T�C�Y9�̔z����`����lastElement���i�[
                lastElementsArray[i] = lastElement; // �z���lastElement���i�[
                
                if (Math.Abs(lastElement) < maxKomaSize) {
                availablePositonsList.Add(i);
            }
        }
            else {
                Debug.Log($"state.banmen.GetBanmen()[{i}] �͋�ł��B");
            }
        }
        //state.turn++;

        //���u����ꏊ�̃��X�g��Ԃ�
        return availablePositonsList; 
    }




    void Start() {

        // State�N���X�̃C���X�^���X�𐶐�
        state = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        lastElementsArray = new int[9];

        
        inputPosField = GameObject.Find("InputPosField").GetComponent<InputField>();
        inputKomaField = GameObject.Find("InputKomaField").GetComponent<InputField>();
    }

    void Update() {
        //�ǂ��炩�̃v���C���[����������܂ŌJ��Ԃ�while��
        //while (!isGameOver) {
        /*while (true) {

            //���͂��󂯕t����
            Debug.Log("���u���ꏊ����͂��Ă�������:");

        }    */ 

    }
    
    //���u���֐�
    State Put(State state, Operator op) {
        //��Βl���r���āAop.koma��op.pos�ɒu���邩�ǂ����𔻒�
        if(lastElementsArray[op.pos] < Math.Abs(op.koma)) {
            state.banmen.GetBanmen()[op.pos].Add(op.koma);
            lastElementsArray[op.pos] = op.koma;//�X�V
        }

        //��������X�V
        if (state.turn % 2 == 1) {//���̏ꍇ
            for (int i = 0; i < state.sente.GetMochigoma().Length; i++) {
                if (state.sente.GetMochigoma()[i] == op.koma) {
                    //
                    state.sente.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
        else {//���̏ꍇ
            for (int i = 0; i < state.gote.GetMochigoma().Length; i++) {
                if (state.gote.GetMochigoma()[i] == op.koma) {
                    state.gote.GetMochigoma()[i] = 0;
                    break;
                }
            }
        }
        //�^�[����i�߂�
        state.turn++;
        return state;
    }

    public int GetInputField(InputField inputField) {
        //InputField����e�L�X�g�����擾����
        string inputText = inputField.text;
        //inputText��int�^�ɕϊ�����
        int inputNumber = int.Parse(inputText);
        Debug.Log(inputNumber);
        //���̓t�H�[���̃e�L�X�g����ɂ���
        inputField.text = "";

        return inputNumber;
    }

    //�{�^���������ꂽ���ɌĂяo�����֐�
    public void OnClickButton() {
        int inputPos = GetInputField(inputPosField);
        int inputKoma = GetInputField(inputKomaField);
        
        // ���u����ꏊ�̃��X�g���v�Z
        List<int> availablePositionsList = GetAvailablePositonsList(state);

        
        // Operator�N���X�̃C���X�^���X�𐶐�
        op = new Operator(availablePositionsList, inputPos, inputKoma);
        //��ԕ\���ƃI�y���[�^���g���ď�ԑJ�ڊ֐������s
        Put(state, op);
        
        for (int i = 0; i < state.banmen.GetBanmen().Count; i++) {
            Debug.Log($"state.banmen.GetBanmen()[{i}] �̍Ō�̗v�f: {lastElementsArray[i]}");
        }
        // ���u����ꏊ�̃��X�g���Čv�Z���ĕ\��
        availablePositionsList = GetAvailablePositonsList(state);
        Debug.Log(string.Join(", ", availablePositionsList));
    }

}
