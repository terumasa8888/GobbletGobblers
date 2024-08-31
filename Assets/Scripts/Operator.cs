public class Operator {
    private int sourcePos;
    private int targetPos;
    private int komaSize;

    // �������u����Ƃ��낪����ꍇ�̃R���X�g���N�^
    public Operator(int targetPos, int komaSize) {
        this.sourcePos = -1;
        this.targetPos = targetPos;
        this.komaSize = komaSize;
    }

    // �Ֆʂ��瓮�����ꍇ�̃R���X�g���N�^
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
