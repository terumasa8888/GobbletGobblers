using System.Collections.Generic;

// オペレータを表すクラス
public class Operator {
    List<int> availablePositonsList;  // 駒を置けるところ
    public int sourcePos; // どこから
    public int targetPos; // どこへ
    public int koma; // どのサイズの

    // 持ち駒から置けるところがある場合のコンストラクタ
    public Operator(List<int> availablePositonsList, int targetPos, int koma) {
        this.availablePositonsList = availablePositonsList;
        this.sourcePos = -1; // 持ち駒から出す場合は -1
        this.targetPos = targetPos;
        this.koma = koma;
    }

    // 盤面から動かす場合のコンストラクタ
    public Operator(List<int> availablePositonsList, int sourcePos, int targetPos, int koma) {
        this.availablePositonsList = availablePositonsList;
        this.sourcePos = sourcePos; // 盤面のマス番号
        this.targetPos = targetPos;
        this.koma = koma;
    }

    // 持ち駒から置けるところがある場合のコンストラクタ
    public Operator(int targetPos, int koma) {
        this.sourcePos = -1; // 持ち駒から出す場合は -1
        this.targetPos = targetPos;
        this.koma = koma;
    }

    // 盤面から動かす場合のコンストラクタ
    public Operator(int sourcePos, int targetPos, int koma) {
        this.sourcePos = sourcePos; // 盤面のマス番号
        this.targetPos = targetPos;
        this.koma = koma;
    }
}
