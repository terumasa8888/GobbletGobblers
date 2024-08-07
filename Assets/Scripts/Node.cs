
using System.Collections.Generic;

public class Node {
    public State state;//その盤面の状態
    public List<Node> children;//子ノード
    public State parentState; // 親ノードのstateを保持するフィールド
    public Operator op;//その盤面に至るまでのオペレータ
    public int eval;//その盤面の評価値

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