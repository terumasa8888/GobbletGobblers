using System.Collections.Generic;

//�I�y���[�^��\���N���X
public class Operator {
    List<int> availablePositonsList;  //���u����Ƃ���
    public int sourcePos;//�ǂ�����
    public int targetPos;// �ǂ���
    public int koma;//�ǂ̃T�C�Y��

    //�������u����Ƃ��낪����ꍇ�̃R���X�g���N�^
    public Operator(List<int> availablePositonsList, int targetPos, int koma) {
        this.availablePositonsList = availablePositonsList;
        this.targetPos = targetPos;
        this.koma = koma;
    }

    //�������u����Ƃ��낪�Ȃ��ꍇ�̃R���X�g���N�^
    public Operator(List<int> availablePositonsList, int sourcePos, int targetPos, int koma) {
        this.availablePositonsList = availablePositonsList;
        this.sourcePos = sourcePos;
        this.targetPos = targetPos;
        this.koma = koma;
    }
}