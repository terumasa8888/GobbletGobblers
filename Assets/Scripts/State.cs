using System.Collections.Generic;

//�Q�[���̏�Ԃ�\���N���X
public class State {
    public Banmen banmen;
    public Mochigoma sente;
    public Mochigoma gote;
    public int turn;

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

    public void NextTurn() {
        turn++;
    }
}
