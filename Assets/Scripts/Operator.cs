public class Operator {
    public int sourcePos;
    public int targetPos;
    public int koma;

    // �������u����Ƃ��낪����ꍇ�̃R���X�g���N�^
    public Operator(int targetPos, int koma) {
        this.sourcePos = -1;
        this.targetPos = targetPos;
        this.koma = koma;
    }

    // �Ֆʂ��瓮�����ꍇ�̃R���X�g���N�^
    public Operator(int sourcePos, int targetPos, int koma) {
        this.sourcePos = sourcePos;
        this.targetPos = targetPos;
        this.koma = koma;
    }
}
