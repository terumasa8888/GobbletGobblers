using UnityEngine;
public class Position : MonoBehaviour {
    [SerializeField]
    private int number;

    public int Number {
        get { return number; }
    }
}