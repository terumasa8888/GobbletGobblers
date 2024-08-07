
using System.Collections.Generic;

public class Node {
    public State state;//���̔Ֆʂ̏��
    public List<Node> children;//�q�m�[�h
    public State parentState; // �e�m�[�h��state��ێ�����t�B�[���h
    public Operator op;//���̔ՖʂɎ���܂ł̃I�y���[�^
    public int eval;//���̔Ֆʂ̕]���l

    public Node(State state, State parentState = null) {
        this.state = state;
        this.parentState = parentState;
        this.children = new List<Node>();
    }

    public Node(State state, State parentState = null, Operator op = null) {
        this.state = state;
        this.parentState = parentState;
        this.op = op;
        this.children = new List<Node>();
    }
}