using System.Collections.Generic;

//持ち駒の状態を表すクラス
public class Mochigoma {
    private List<int> mochigoma;//手持ちの駒

    //コンストラクタ
    public Mochigoma(int koma1, int koma2, int koma3, int koma4, int koma5, int koma6) {
        this.mochigoma = new List<int> { koma1, koma2, koma3, koma4, koma5, koma6 };
    }
    // コピーコンストラクタ
    public Mochigoma(Mochigoma other) {
        this.mochigoma = new List<int>(other.mochigoma);
    }

    // リストを取得するメソッド
    public List<int> GetMochigoma() {
        return mochigoma;
    }
    // リストに駒を追加するメソッド
    public void AddKoma(int koma) {
        mochigoma.Add(koma);
    }

    // リストから駒を削除するメソッド
    public bool RemoveKoma(int koma) {
        if (mochigoma.Contains(koma)) {
            mochigoma.Remove(koma);
            return true;
        }
        else {
            return false;
        }
    }
}