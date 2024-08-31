using System.Collections.Generic;

public class Node {
    public State state;//その盤面の状態
    private List<Node> children;//子ノード
    public State parentState; // 親ノードのstateを保持するフィールド
    public Operator op;//その盤面に至るまでのオペレータ
    private int eval;//その盤面の評価値

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