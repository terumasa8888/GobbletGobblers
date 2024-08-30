public class Operator {
    public int sourcePos;
    public int targetPos;
    public int koma;

    // 持ち駒から置けるところがある場合のコンストラクタ
    public Operator(int targetPos, int koma) {
        this.sourcePos = -1;
        this.targetPos = targetPos;
        this.koma = koma;
    }

    // 盤面から動かす場合のコンストラクタ
    public Operator(int sourcePos, int targetPos, int koma) {
        this.sourcePos = sourcePos;
        this.targetPos = targetPos;
        this.koma = koma;
    }
}
