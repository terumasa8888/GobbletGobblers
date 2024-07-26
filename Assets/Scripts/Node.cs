
using System.Collections.Generic;

public class Node {
    public State state;//その盤面の状態
    public List<Node> children;//子ノード
    public State parentState; // 親ノードのstateを保持するフィールド
    public Operator op;//その盤面に至るまでのオペレータ
    public int eval;//その盤面の評価値


    public Node parent;//親ノード
    
    int availablePositonsListCount;//駒を置ける場所の数
    int masume;//どのマスに評価値をつけるかを格納する変数

    public Node(State state, State parentState = null) {
        this.state = state;
        this.parentState = parentState; // 親ノードのstateを設定
        this.children = new List<Node>();
    }

    public Node(State state, State parentState = null, Operator op = null) {
        this.state = state;
        this.parentState = parentState; // 親ノードのstateを設定
        this.op = op; // オペレータを設定
        this.children = new List<Node>();
    }
}