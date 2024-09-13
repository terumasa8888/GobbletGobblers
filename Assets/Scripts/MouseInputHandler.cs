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

    // ドラッグ開始時に駒を選択する関数
    public void HandlePieceSelection() {
        state = gameController.GetState();

        if (state == null) {
            Debug.LogError("Stateオブジェクトがnullです。");
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

        int currentPlayer = state.isSenteTurn() ? 1 : -1;//ここbool

        Mochigoma currentPlayerMochigoma = state.isSenteTurn() ? state.sente : state.gote;
        int komaSize = selectedKomaComponent.size;
        int komaPos = selectedKomaComponent.pos;

        if (selectedKomaComponent.player != currentPlayer) {
            Debug.Log("この駒は現在のプレイヤーのものではありません");
            selectedKoma = null;
            return;
        }

        state.UpdateAvailablePositionsList();
        bool canPlaceFromMochigoma = state.AvailablePositionsList().Count > 0;

        // 選択した駒が盤面にあり、持ち駒から置ける場所がある場合
        if (komaPos != -1 && canPlaceFromMochigoma) {
            Debug.Log("持ち駒から置ける場所があるため、盤面の駒は選択できません");
            selectedKoma = null;
            return;
        }
        // 選択した駒が持ち駒で、持ち駒から置ける場所がない場合
        else if (komaPos == -1 && !canPlaceFromMochigoma) {
            Debug.Log("持ち駒から置ける場所がないため、持ち駒は選択できません");
            selectedKoma = null;
            return;
        }
        // 選択した駒が持ち駒で、持ち駒から置ける場所がある場合
        else if(komaPos == -1 && canPlaceFromMochigoma) {
            return;
        }

        // 選択した駒が盤面にあり、持ち駒から置ける場所がない場合
        Debug.Log("選択した駒を持ち駒に追加します");
        currentPlayerMochigoma.AddKoma(komaSize);
        List<List<int>> banmen = state.banmen.GetBanmen();
        banmen[komaPos].RemoveAt(banmen[komaPos].Count - 1);

        // 新しいStateを作成してApplyMoveを呼び出す
        //State newState = new State(state.banmen, state.sente, state.gote);
        //gameController.ApplyMove(newState);

        //勝利判定
        gameController.CheckForWin();
    }

    // マウスカーソルの位置に駒を追従させる関数
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

    // マウスを離した時に駒を置く関数
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
            //Debug.Log("ターン数: " + state.Turn());
        }
        selectedKoma = null;
    }

    //駒を置く具体的な処理を行う関数
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

        //移動前と後が同じ位置なら、駒を元の位置に戻し、移動処理は行わない
        if (positionNumber == komaPos && komaPos != -1) {
            ResetKomaPosition();
            Debug.Log("現在と同じ位置に駒を置くことはできません");
            currentPlayerMochigoma.RemoveKoma(komaSize);
            
            //持ち上げた駒はいったん削除されているので、元の位置に戻す処理を行う
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
        //移動先により大きい駒があるなら、駒を置かずに処理を終了
        if (Math.Abs(state.LastElementsArray()[positionNumber]) >= Math.Abs(komaSize)) {

            //選択した駒の元の位置(komaPos)に戻す処理
            ResetKomaPosition();
            Debug.Log("選択した駒より大きい駒の上に置くことはできません");
            currentPlayerMochigoma.RemoveKoma(komaSize);

            //持ち上げた駒はいったん削除されているので、元の位置に戻す処理を行う
            List<List<int>> banmen = state.banmen.GetBanmen();
            if (komaPos >= 0 && komaPos < banmen.Count) {
                if (banmen[komaPos].Count > 0) {
                    banmen[komaPos][banmen[komaPos].Count - 1] = komaSize;
                }
                else {
                    banmen[komaPos].Add(komaSize); // リストが空の場合、新しい要素を追加
                }
            }
            return;
        }

        // 駒をマスの上に配置する処理（駒の底面がマスの上面に来るように調整）
        float komaHeight = selectedKoma.GetComponent<Collider>().bounds.size.y;
        Vector3 newPosition = hit.collider.transform.position;
        newPosition.y += komaHeight / 2;
        selectedKoma.transform.position = newPosition;

        Koma komaComponent = selectedKoma.GetComponent<Koma>();
        komaComponent.pos = positionNumber; // 駒が配置された新しい位置に更新

        selectedKoma = null;

        op = new Operator(komaPos, positionNumber, komaSize);

        State newState = gameController.Put(state, op);
        gameController.UpdateMochigoma(newState, op);
        gameController.ApplyMove(newState);

        //勝利判定
        gameController.CheckForWin();
    }

    // 駒を元の位置に戻す処理を共通化
    void ResetKomaPosition() {
        if (selectedKoma != null) {
            selectedKoma.transform.position = originalPosition;
            selectedKoma = null;
        }
    }
}