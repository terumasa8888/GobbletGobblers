using System.Collections.Generic;

//�Q�[���̏�Ԃ�\���N���X
public class State {
    public Banmen banmen;
    public Mochigoma sente;
    public Mochigoma gote;
    public int turn;//�o�߃^�[����

    public State(Banmen banmen, Mochigoma sente, Mochigoma gote) {
        this.banmen = new Banmen();
        this.sente = new Mochigoma(3, 3, 2, 2, 1, 1);
        this.gote = new Mochigoma(-3, -3, -2, -2, -1, -1);
        this.turn = 1;
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
    // ���X�g���擾���郁�\�b�h
    public List<int> GetMochigoma() {
        return mochigoma;
    }
    // ���X�g�ɋ��ǉ����郁�\�b�h
    public void AddKoma(int koma) {
        mochigoma.Add(koma);
    }

    // ���X�g�������폜���郁�\�b�h
    public bool RemoveKoma(int koma) {
        return mochigoma.Remove(koma);
    }
}

