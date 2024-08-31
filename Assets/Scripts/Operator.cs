public class Operator {
    private int sourcePos;
    private int targetPos;
    private int komaSize;

    // 持ち駒から置けるところがある場合のコンストラクタ
    public Operator(int targetPos, int komaSize) {
        this.sourcePos = -1;
        this.targetPos = targetPos;
        this.komaSize = komaSize;
    }

    // 盤面から動かす場合のコンストラクタ
    public Operator(int sourcePos, int targetPos, int komaSize) {
        this.sourcePos = sourcePos;
        this.targetPos = targetPos;
        this.komaSize = komaSize;
    }

    public int KomaSize() {
        return komaSize;
    }

    public int SourcePos() {
        return sourcePos;
    }

    public int TargetPos() {
        return targetPos;
    }
}
