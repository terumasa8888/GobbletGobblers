using System.Collections;
using System.Collections.Generic;
public class Banmen {
    // 初期の配列の設定
    List<List<int>> banmen = new List<List<int>>();

    //コンストラクタ
    public Banmen() {
        for (int i = 0; i < 9; i++) {
            banmen.Add(new List<int> { 0 });
        }
    }

    // コピーコンストラクタ
    public Banmen(Banmen other) {
        foreach (var row in other.banmen) {
            this.banmen.Add(new List<int>(row));
        }
    }

    public List<List<int>> GetBanmen() {
        return banmen;
    }

    public void SetBanmen(List<List<int>> newBanmen) {
        banmen = newBanmen;
    }
}