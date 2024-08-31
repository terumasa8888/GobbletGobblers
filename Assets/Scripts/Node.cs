using System.Collections.Generic;

public class Node {
    public State state;//���̔Ֆʂ̏��
    private List<Node> children;//�q�m�[�h
    public State parentState; // �e�m�[�h��state��ێ�����t�B�[���h
    public Operator op;//���̔ՖʂɎ���܂ł̃I�y���[�^
    private int eval;//���̔Ֆʂ̕]���l

    public Node(State state, State parentState = null, Operator op = null) {
        this.state = state;
        this.parentState = parentState;
        this.op = op;
        this.children = new List<Node>();
    }
    public Node(State state, int eval, List<Node> children, State parentState = null, Operator op = null) {
        this.state = state;
        this.parentState = parentState;
        this.op = op;
        this.children = new List<Node>();
        this.eval = eval;
    }

    public int Eval() {
        return this.eval;
    }

    public void SetEval(int eval) {
        this.eval = eval;
    }

    public List<Node> Children() {
        return this.children;
    }

    public void AddChild(Node child) {
        this.children.Add(child);
    }
}