using System;
using UnityEngine;
using System.Collections.Generic;

public class GameControllerTests : MonoBehaviour {
    private GameController gameController;
    private State testState;
    private Operator testOperator;

    public void Setup() {
        gameController = new GameController();
        testState = new State(new Banmen(), new Mochigoma(3, 3, 2, 2, 1, 1), new Mochigoma(-3, -3, -2, -2, -1, -1));
        testOperator = new Operator(new List<int>(), 4, 3);
    }

    public void TestCountReach_TwoGoteOneEmpty() {
        Setup();
        testState.banmen.GetBanmen().AddRange(new List<List<int>> {
            new List<int> { -1, -1, 0 },
            new List<int> { 0, 0, 0 },
            new List<int> { 0, 0, 0 }
        });

        int reachCount = gameController.CountReach(testState);
        Debug.Log(reachCount == 1 ? "TestCountReach_TwoGoteOneEmpty Passed" : "TestCountReach_TwoGoteOneEmpty Failed");
    }

    public void TestCountReach_OneGoteTwoEmpty() {
        Setup();
        testState.banmen.GetBanmen().AddRange(new List<List<int>> {
            new List<int> { -1, 0, 0 },
            new List<int> { 0, 0, 0 },
            new List<int> { 0, 0, 0 }
        });

        int reachCount = gameController.CountReach(testState);
        Debug.Log(reachCount == 0 ? "TestCountReach_OneGoteTwoEmpty Passed" : "TestCountReach_OneGoteTwoEmpty Failed");
    }

    public void TestCountReach_ThreeGote() {
        Setup();
        testState.banmen.GetBanmen().AddRange(new List<List<int>> {
            new List<int> { -1, -1, -1 },
            new List<int> { 0, 0, 0 },
            new List<int> { 0, 0, 0 }
        });

        int reachCount = gameController.CountReach(testState);
        Debug.Log(reachCount == 0 ? "TestCountReach_ThreeGote Passed" : "TestCountReach_ThreeGote Failed");
    }

    public void TestCountReach_TwoGoteOneSente() {
        Setup();
        testState.banmen.GetBanmen().AddRange(new List<List<int>> {
            new List<int> { -1, -1, 1 },
            new List<int> { 0, 0, 0 },
            new List<int> { 0, 0, 0 }
        });

        int reachCount = gameController.CountReach(testState);
        Debug.Log(reachCount == 0 ? "TestCountReach_TwoGoteOneSente Passed" : "TestCountReach_TwoGoteOneSente Failed");
    }

    public void TestIsReach_TwoGoteOneEmpty() {
        Setup();
        testState.banmen.GetBanmen().AddRange(new List<List<int>> {
            new List<int> { -1, -1, 0 },
            new List<int> { 0, 0, 0 },
            new List<int> { 0, 0, 0 }
        });

        bool result = gameController.IsReach(-1, -1, 0, testState, new int[,] { { 0, 0 }, { 0, 1 }, { 0, 2 } });
        Debug.Log(result ? "TestIsReach_TwoGoteOneEmpty Passed" : "TestIsReach_TwoGoteOneEmpty Failed");
    }

    public void TestIsReach_OneGoteTwoEmpty() {
        Setup();
        testState.banmen.GetBanmen().AddRange(new List<List<int>> {
            new List<int> { -1, 0, 0 },
            new List<int> { 0, 0, 0 },
            new List<int> { 0, 0, 0 }
        });

        bool result = gameController.IsReach(-1, 0, 0, testState, new int[,] { { 0, 0 }, { 0, 1 }, { 0, 2 } });
        Debug.Log(!result ? "TestIsReach_OneGoteTwoEmpty Passed" : "TestIsReach_OneGoteTwoEmpty Failed");
    }

    public void TestIsReach_ThreeGote() {
        Setup();
        testState.banmen.GetBanmen().AddRange(new List<List<int>> {
            new List<int> { -1, -1, -1 },
            new List<int> { 0, 0, 0 },
            new List<int> { 0, 0, 0 }
        });

        bool result = gameController.IsReach(-1, -1, -1, testState, new int[,] { { 0, 0 }, { 0, 1 }, { 0, 2 } });
        Debug.Log(!result ? "TestIsReach_ThreeGote Passed" : "TestIsReach_ThreeGote Failed");
    }

    public void TestIsReach_TwoGoteOneSente() {
        Setup();
        testState.banmen.GetBanmen().AddRange(new List<List<int>> {
            new List<int> { -1, -1, 1 },
            new List<int> { 0, 0, 0 },
            new List<int> { 0, 0, 0 }
        });

        bool result = gameController.IsReach(-1, -1, 1, testState, new int[,] { { 0, 0 }, { 0, 1 }, { 0, 2 } });
        Debug.Log(!result ? "TestIsReach_TwoGoteOneSente Passed" : "TestIsReach_TwoGoteOneSente Failed");
    }

    void Start() {
        TestCountReach_TwoGoteOneEmpty();
        TestCountReach_OneGoteTwoEmpty();
        TestCountReach_ThreeGote();
        TestCountReach_TwoGoteOneSente();
        TestIsReach_TwoGoteOneEmpty();
        TestIsReach_OneGoteTwoEmpty();
        TestIsReach_ThreeGote();
        TestIsReach_TwoGoteOneSente();
    }
}
