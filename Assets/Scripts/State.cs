using System.Collections.Generic;

//ゲームの状態を表すクラス
public class State {
    public Banmen banmen;
    public Mochigoma sente;
    public Mochigoma gote;
    public int turn;//経過ターン数

    // 初期化用のコンストラクタ
    public State(Banmen banmen, Mochigoma sente, Mochigoma gote) {
        this.banmen = banmen;
        this.sente = sente;
        this.gote = gote;
        this.turn = 1;
    }

    // コピーコンストラクタ
    public State(State other) {
        this.banmen = new Banmen(other.banmen);
        this.sente = new Mochigoma(other.sente);
        this.gote = new Mochigoma(other.gote);
        this.turn = other.turn;
    }

    //turnをプラス1するメソッド
    public void NextTurn() {
        turn++;
    }
}

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
}

//持ち駒の状態を表すクラス
public class Mochigoma {
    //名前mochigomaListにするべき？？
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
    //返り値Bool？
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

