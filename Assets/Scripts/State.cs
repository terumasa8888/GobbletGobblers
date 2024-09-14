using System;
using System.Collections.Generic;
using UnityEditor.iOS.Xcode;

//�Q�[���̏�Ԃ�\���N���X
public class State {
    public Banmen banmen;
    public Mochigoma sente;
    public Mochigoma gote;
    private int turn;
    private int[] lastElementsArray;
    private List<int> availablePositionsList;
    private bool isGameOver;


    // �������p�̃R���X�g���N�^
    public State(Banmen banmen, Mochigoma sente, Mochigoma gote) {
        this.banmen = banmen;
        this.sente = sente;
        this.gote = gote;
        this.turn = 1;
        this.lastElementsArray = new int[9];
        this.availablePositionsList = new List<int>();
        this.isGameOver = false;
    }

    // �R�s�[�R���X�g���N�^
    public State(State other) {
        this.banmen = new Banmen(other.banmen);
        this.sente = new Mochigoma(other.sente);
        this.gote = new Mochigoma(other.gote);
        this.turn = other.turn;
        this.lastElementsArray = other.lastElementsArray;
        this.availablePositionsList = other.availablePositionsList;
        this.isGameOver = other.isGameOver;
    }

    public void NextTurn() {
        turn++;
    }

    public int Turn() {
        return turn;
    }

    public bool isSenteTurn() {
        return turn % 2 == 1;
    }

    public void UpdateLastElementsArray() {
        List<List<int>> banmen = this.banmen.GetBanmen();
        for (int i = 0; i < banmen.Count; i++) {
            if (banmen[i].Count > 0) {
                lastElementsArray[i] = banmen[i][banmen[i].Count - 1];
            }
            else {
                lastElementsArray[i] = 0;
            }
        }
    }

    public int[] LastElementsArray() {
        return lastElementsArray;
    }

    public void UpdateAvailablePositionsList() {
        int maxKomaSize = 0;
        List<int> currentMochigoma = this.turn % 2 == 1 ? this.sente.GetMochigoma() : this.gote.GetMochigoma();
        //���݂̎�����̒��Ő�Βl���ł��傫�����̂̃T�C�Y��maxKomaSize�Ɋi�[
        foreach (int komaSize in currentMochigoma) {
            if (Math.Abs(komaSize) > maxKomaSize) {
                maxKomaSize = Math.Abs(komaSize);
            }
        }

        availablePositionsList = new List<int>();
        int[] lastElementsArray = this.LastElementsArray();
        for (int i = 0; i < lastElementsArray.Length; i++) {
            if (Math.Abs(lastElementsArray[i]) < maxKomaSize) {
                availablePositionsList.Add(i);
            }
        }
    }

    public List<int> AvailablePositionsList() {
        return availablePositionsList;
    }

    public void SetGameOver() {
        isGameOver = true;
    }

    public bool IsGameOver() {
        return isGameOver;
    }

    // ���҂����邩�ǂ������`�F�b�N���A���҂�����ꍇ�̓Q�[�����I������
    public GameResult CheckGameOver() {
        GameResult result = CheckWinner();
        if (result != GameResult.None) {
            SetGameOver();
        }
        return result;
    }

    //����������s���֐�
    public GameResult CheckWinner() {
        var (senteArray, goteArray) = CreateBinaryArrays();

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

    //��������̂��߂ɁA�����̋��1�A����ȊO��0�Ƃ����񎟌��z���Ԃ��֐�
    private (int[,], int[,]) CreateBinaryArrays() {
        int[,] senteArray = new int[3, 3];
        int[,] goteArray = new int[3, 3];
        List<List<int>> banmen = this.banmen.GetBanmen();

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
}
