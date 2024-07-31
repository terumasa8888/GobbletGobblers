using System.Collections.Generic;

//�Q�[���̏�Ԃ�\���N���X
public class State {
    public Banmen banmen;
    public Mochigoma sente;
    public Mochigoma gote;
    public int turn;//�o�߃^�[����

    // �������p�̃R���X�g���N�^
    public State(Banmen banmen, Mochigoma sente, Mochigoma gote) {
        this.banmen = banmen;
        this.sente = sente;
        this.gote = gote;
        this.turn = 1;
    }

    // �R�s�[�R���X�g���N�^
    public State(State other) {
        this.banmen = new Banmen(other.banmen);
        this.sente = new Mochigoma(other.sente);
        this.gote = new Mochigoma(other.gote);
        this.turn = other.turn;
    }

    //turn���v���X1���郁�\�b�h
    public void NextTurn() {
        turn++;
    }
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

    // �R�s�[�R���X�g���N�^
    public Banmen(Banmen other) {
        foreach (var row in other.banmen) {
            this.banmen.Add(new List<int>(row));
        }
    }

    public List<List<int>> GetBanmen() {
        return banmen;
    }
}

//������̏�Ԃ�\���N���X
public class Mochigoma {
    //���OmochigomaList�ɂ���ׂ��H�H
    private List<int> mochigoma;//�莝���̋�

    //�R���X�g���N�^
    public Mochigoma(int koma1, int koma2, int koma3, int koma4, int koma5, int koma6) {
        this.mochigoma = new List<int> { koma1, koma2, koma3, koma4, koma5, koma6 };
    }
    // �R�s�[�R���X�g���N�^
    public Mochigoma(Mochigoma other) {
        this.mochigoma = new List<int>(other.mochigoma);
    }

    // ���X�g���擾���郁�\�b�h
    public List<int> GetMochigoma() {
        return mochigoma;
    }
    // ���X�g�ɋ��ǉ����郁�\�b�h
    public void AddKoma(int koma) {
        mochigoma.Add(koma);
    }

    // ���X�g�������폜���郁�\�b�h
    //�Ԃ�lBool�H
    public bool RemoveKoma(int koma) {
        if (mochigoma.Contains(koma)) {
            mochigoma.Remove(koma);
            return true;
        }
        else {
            return false;
        }
    }
}

