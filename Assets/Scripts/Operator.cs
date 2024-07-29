using System.Collections.Generic;

// �I�y���[�^��\���N���X
public class Operator {
    List<int> availablePositonsList;  // ���u����Ƃ���
    public int sourcePos; // �ǂ�����
    public int targetPos; // �ǂ���
    public int koma; // �ǂ̃T�C�Y��

    // �������u����Ƃ��낪����ꍇ�̃R���X�g���N�^
    public Operator(List<int> availablePositonsList, int targetPos, int koma) {
        this.availablePositonsList = availablePositonsList;
        this.sourcePos = -1; // �������o���ꍇ�� -1
        this.targetPos = targetPos;
        this.koma = koma;
    }

    // �Ֆʂ��瓮�����ꍇ�̃R���X�g���N�^
    public Operator(List<int> availablePositonsList, int sourcePos, int targetPos, int koma) {
        this.availablePositonsList = availablePositonsList;
        this.sourcePos = sourcePos; // �Ֆʂ̃}�X�ԍ�
        this.targetPos = targetPos;
        this.koma = koma;
    }

    // �������u����Ƃ��낪����ꍇ�̃R���X�g���N�^
    public Operator(int targetPos, int koma) {
        this.sourcePos = -1; // �������o���ꍇ�� -1
        this.targetPos = targetPos;
        this.koma = koma;
    }

    // �Ֆʂ��瓮�����ꍇ�̃R���X�g���N�^
    public Operator(int sourcePos, int targetPos, int koma) {
        this.sourcePos = sourcePos; // �Ֆʂ̃}�X�ԍ�
        this.targetPos = targetPos;
        this.koma = koma;
    }
}
