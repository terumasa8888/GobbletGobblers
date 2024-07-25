
public class Node {
    State state;//その盤面の状態
    Operator op;//その盤面に至るまでのオペレータ
    Node parent;//親ノード
    int eval;//その盤面の評価値
    int availablePositonsListCount;//駒を置ける場所の数
    int masume;//どのマスに評価値をつけるかを格納する変数
}