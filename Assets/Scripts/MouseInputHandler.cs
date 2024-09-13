using System;
using System.Collections.Generic;
using UnityEngine;

public class MouseInputHandler : MonoBehaviour {
    private GameController gameController;
    private State state;
    private GameObject selectedKoma;
    [SerializeField]
    private LayerMask komaLayer;
    [SerializeField]
    private LayerMask positionLayer;
    private Vector3 originalPosition;
    Operator op;

    public void Initialize(GameController controller) {
        gameController = controller;
    }

    // �h���b�O�J�n���ɋ��I������֐�
    public void HandlePieceSelection() {
        state = gameController.GetState();

        if (state == null) {
            Debug.LogError("State�I�u�W�F�N�g��null�ł��B");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 5.0f);

        //if (Physics.Raycast(ray, out hit, Mathf.Infinity, komaLayer)) {
        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, komaLayer)) {
            return;
        }
        selectedKoma = hit.collider.gameObject;

        originalPosition = selectedKoma.transform.position;
        Koma selectedKomaComponent = selectedKoma.GetComponent<Koma>();

        int currentPlayer = state.isSenteTurn() ? 1 : -1;//����bool

        Mochigoma currentPlayerMochigoma = state.isSenteTurn() ? state.sente : state.gote;
        int komaSize = selectedKomaComponent.size;
        int komaPos = selectedKomaComponent.pos;

        if (selectedKomaComponent.player != currentPlayer) {
            Debug.Log("���̋�͌��݂̃v���C���[�̂��̂ł͂���܂���");
            selectedKoma = null;
            return;
        }

        state.UpdateAvailablePositionsList();
        bool canPlaceFromMochigoma = state.AvailablePositionsList().Count > 0;

        // �I��������Ֆʂɂ���A�������u����ꏊ������ꍇ
        if (komaPos != -1 && canPlaceFromMochigoma) {
            Debug.Log("�������u����ꏊ�����邽�߁A�Ֆʂ̋�͑I���ł��܂���");
            selectedKoma = null;
            return;
        }
        // �I�������������ŁA�������u����ꏊ���Ȃ��ꍇ
        else if (komaPos == -1 && !canPlaceFromMochigoma) {
            Debug.Log("�������u����ꏊ���Ȃ����߁A������͑I���ł��܂���");
            selectedKoma = null;
            return;
        }
        // �I�������������ŁA�������u����ꏊ������ꍇ
        else if(komaPos == -1 && canPlaceFromMochigoma) {
            return;
        }

        // �I��������Ֆʂɂ���A�������u����ꏊ���Ȃ��ꍇ
        Debug.Log("�I���������������ɒǉ����܂�");
        currentPlayerMochigoma.AddKoma(komaSize);
        List<List<int>> banmen = state.banmen.GetBanmen();
        banmen[komaPos].RemoveAt(banmen[komaPos].Count - 1);

        // �V����State���쐬����ApplyMove���Ăяo��
        //State newState = new State(state.banmen, state.sente, state.gote);
        //gameController.ApplyMove(newState);

        //��������
        gameController.CheckForWin();
    }

    // �}�E�X�J�[�\���̈ʒu�ɋ��Ǐ]������֐�
    public void FollowCursor() {
        if (selectedKoma == null) {
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = 10.0f;
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        CapsuleCollider komaCapsuleCollider = selectedKoma.GetComponent<CapsuleCollider>();
        if (komaCapsuleCollider != null) {
            float komaHeight = komaCapsuleCollider.height;
            newPosition.y += komaHeight * selectedKoma.transform.localScale.y / 2.0f;
        }
        else {
            newPosition.y += 0.5f;
        }
        selectedKoma.transform.position = newPosition;
    }

    // �}�E�X�𗣂������ɋ��u���֐�
    public void HandlePieceDrop() {
        if (selectedKoma == null) {
            return;
        }
        //state = gameController.GetState();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.blue, 5.0f);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, positionLayer)) {
            state.UpdateLastElementsArray();
            PlaceSelectedKomaOnPosition(hit);
            //gameController.UpdateMochigoma(state, op);
            //Debug.Log("�^�[����: " + state.Turn());
        }
        selectedKoma = null;
    }

    //���u����̓I�ȏ������s���֐�
    void PlaceSelectedKomaOnPosition(RaycastHit hit) {

        int komaSize = selectedKoma.GetComponent<Koma>().size;
        int komaPos = selectedKoma.GetComponent<Koma>().pos;

        Position positionComponent = hit.collider.gameObject.GetComponent<Position>();
        if (positionComponent == null) {
            Debug.LogWarning("Position component not found on the hit object.");
            return;
        }
        int positionNumber = positionComponent.Number;

        Mochigoma currentPlayerMochigoma = state.isSenteTurn() ? state.sente : state.gote;

        //�ړ��O�ƌオ�����ʒu�Ȃ�A������̈ʒu�ɖ߂��A�ړ������͍s��Ȃ�
        if (positionNumber == komaPos && komaPos != -1) {
            ResetKomaPosition();
            Debug.Log("���݂Ɠ����ʒu�ɋ��u�����Ƃ͂ł��܂���");
            currentPlayerMochigoma.RemoveKoma(komaSize);
            
            //�����グ����͂�������폜����Ă���̂ŁA���̈ʒu�ɖ߂��������s��
            List<List<int>> banmen = state.banmen.GetBanmen();
            if (komaPos >= 0 && komaPos < banmen.Count) {
                if (banmen[komaPos].Count > 0) {
                    banmen[komaPos][banmen[komaPos].Count - 1] = komaSize;
                }
                else {
                    banmen[komaPos].Add(komaSize);
                }
            }
            else {
                Debug.LogError("komaPos is out of range: " + komaPos);
            }
            return;
        }

        state.UpdateLastElementsArray();
        //�ړ���ɂ��傫�������Ȃ�A���u�����ɏ������I��
        if (Math.Abs(state.LastElementsArray()[positionNumber]) >= Math.Abs(komaSize)) {

            //�I��������̌��̈ʒu(komaPos)�ɖ߂�����
            ResetKomaPosition();
            Debug.Log("�I����������傫����̏�ɒu�����Ƃ͂ł��܂���");
            currentPlayerMochigoma.RemoveKoma(komaSize);

            //�����グ����͂�������폜����Ă���̂ŁA���̈ʒu�ɖ߂��������s��
            List<List<int>> banmen = state.banmen.GetBanmen();
            if (komaPos >= 0 && komaPos < banmen.Count) {
                if (banmen[komaPos].Count > 0) {
                    banmen[komaPos][banmen[komaPos].Count - 1] = komaSize;
                }
                else {
                    banmen[komaPos].Add(komaSize); // ���X�g����̏ꍇ�A�V�����v�f��ǉ�
                }
            }
            return;
        }

        // ����}�X�̏�ɔz�u���鏈���i��̒�ʂ��}�X�̏�ʂɗ���悤�ɒ����j
        float komaHeight = selectedKoma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = hit.collider.transform.position;
        newPosition.y += komaHeight / 2;
        selectedKoma.transform.position = newPosition;

        Koma komaComponent = selectedKoma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // ��z�u���ꂽ�V�����ʒu�ɍX�V

        selectedKoma = null;

        op = new Operator(komaPos, positionNumber, komaSize);

        State newState = gameController.Put(state, op);
        gameController.UpdateMochigoma(newState, op);
        gameController.ApplyMove(newState);

        //��������
        gameController.CheckForWin();
    }

    // ������̈ʒu�ɖ߂����������ʉ�
    void ResetKomaPosition() {
        if (selectedKoma != null) {
            selectedKoma.transform.position = originalPosition;
            selectedKoma = null;
        }
    }
}