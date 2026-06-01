using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Tilemaps;
using System.Collections;

public class TutorialController : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector director;
    public GameOfLifeManager myGameManager;
    private Tilemap tilemap;

    [Header("UI Text Objects (driven by Timeline)")]
    public GameObject tutorialText1;
    public GameObject tutorialText3;
    public GameObject tutorialText4;

    [Header("Timeline Pause Points")]
    public float tutorialText1Time;
    public float firstTransitionTime;
    public float secondTransitionTime;
    
    
    // -----------------------------------------------------------------------
    // State machine
    // -----------------------------------------------------------------------

    private enum TutorialState
    {
        Idle,
        Tutorial1_WaitingForClick,
        Tutorial1_Blinking,
        Tutorial2_Playing,          
        Tutorial3_Setup,
        Tutorial4_Setup,
        Tutorial5_Setup,
        Done
    }

    private TutorialState state = TutorialState.Idle;

    void Start()
    {
        if (myGameManager == null)
            myGameManager = GameObject.Find("GameManager").GetComponent<GameOfLifeManager>();

        tilemap = myGameManager.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
            Debug.LogError("TutorialController: Could not find Tilemap under GameManager!");
    }

    void Update()
    {
        switch (state)
        {
            case TutorialState.Idle:
                if ((float)director.time >= tutorialText1Time)
                    TransitionTo(TutorialState.Tutorial1_WaitingForClick);
                break;

            case TutorialState.Tutorial1_WaitingForClick:
                director.Pause();
                if (Input.GetMouseButtonDown(0))
                {
                    director.time = firstTransitionTime;
                    director.Play();
                    TransitionTo(TutorialState.Tutorial1_Blinking);
                }
                break;

            case TutorialState.Tutorial2_Playing:
                if (tutorialText3 != null && tutorialText3.activeSelf)
                    TransitionTo(TutorialState.Tutorial3_Setup);
                break;

            // All coroutine-driven states: nothing to poll each frame
            case TutorialState.Tutorial1_Blinking:
            case TutorialState.Tutorial3_Setup:
                if (tutorialText4 != null && tutorialText4.activeSelf)
                    TransitionTo(TutorialState.Tutorial4_Setup);
                break;
            case TutorialState.Tutorial4_Setup:
            case TutorialState.Tutorial5_Setup:    
            case TutorialState.Done:
                break;
        }
    }

    private void TransitionTo(TutorialState newState)
    {
        state = newState;

        switch (newState)
        {
            case TutorialState.Tutorial1_Blinking:
                StartCoroutine(BlinkThenKill());
                break;

            case TutorialState.Tutorial2_Playing:
                director.Play();
                break;

            case TutorialState.Tutorial3_Setup:
                StartCoroutine(Tutorial3Sequence());
                break;

            case TutorialState.Tutorial4_Setup:
                StartCoroutine(Tutorial4Sequence());
                break;
        }
    }

    // -----------------------------------------------------------------------
    // Tutorial sequences
    // -----------------------------------------------------------------------

    private IEnumerator BlinkThenKill()
    {
        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(BlinkCells(GetAllLivingPositions(), 4f));
        SetLivingCellsVisible(false);
        KillLivingCells();
        TransitionTo(TutorialState.Tutorial2_Playing);
    }

    private IEnumerator Tutorial3Sequence()
    {
        yield return new WaitForSeconds(0.5f);

        int cx = myGameManager.gridSize / 2;
        int cy = myGameManager.gridSize / 2;

        PlaceCells(cx, cy, new int[][] {
            new int[] { 0,  1 },
            new int[] { 0, -1 },
            new int[] {-1,  0 },
            new int[] { 1,  0 },
            new int[] { 0,  0 },   // center — different color
        });

        yield return StartCoroutine(FadeTilemap(0f, 1f, 1f));
        yield return new WaitForSeconds(1f);
        //kill single cell
        var centerList = new System.Collections.Generic.List<Vector3Int> { new Vector3Int(cx, cy, 0) };
        yield return StartCoroutine(BlinkCells(centerList, 4f));
        int p = myGameManager.Pos(cx, cy);
        myGameManager.cells[p] = 0;
        myGameManager.age[p] = 0;
        myGameManager.colorIndex[p] = 0;

        director.time = secondTransitionTime;
        yield return StartCoroutine(FadeTilemap(1f, 0f, 2f));

        tilemap.ClearAllTiles();
        KillLivingCells();
        TransitionTo(TutorialState.Tutorial4_Setup);
    }

    private IEnumerator Tutorial4Sequence()
    {
        yield return new WaitForSeconds(0.5f);

        int cx = myGameManager.gridSize / 2 +1;
        int cy = myGameManager.gridSize / 2 +1;

        // 3 parent cells surrounding the target dead cell at (cx, cy)
        PlaceCells(cx, cy, new int[][] {
            new int[] { -1,  0 },  // left
            new int[] { -1, -1 },  // bottom-left
            new int[] {  0, -1 },  // below
            // (cx, cy) itself stays dead — it has exactly 3 neighbors
        });

        yield return StartCoroutine(FadeTilemap(0f, 1f, 1f));
        yield return new WaitForSeconds(1.5f);

        // Blink the dead center cell to show it's about to be born
        var targetPos = new Vector3Int(cx, cy, 0);
        var targetList = new System.Collections.Generic.List<Vector3Int> { targetPos };
        yield return StartCoroutine(BlinkCells(targetList, 3f));

        // Now actually birth it
        int p = myGameManager.Pos(cx, cy);
        myGameManager.cells[p] = 1;
        myGameManager.age[p] = 1;
        myGameManager.colorIndex[p] = 1;
        myGameManager.UpdateCellTile(cx, cy);

        yield return new WaitForSeconds(2f);
        TransitionTo(TutorialState.Done);
    }

    // -----------------------------------------------------------------------
    // Reusable building blocks
    // -----------------------------------------------------------------------

    // Fades the tilemap alpha from startAlpha to endAlpha over duration
    private IEnumerator FadeTilemap(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            tilemap.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        tilemap.color = new Color(1f, 1f, 1f, endAlpha);
    }


    // Blinks the cells on and off
    private IEnumerator BlinkCells(System.Collections.Generic.List<Vector3Int> positions, float duration)
    {
        bool visible = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float blinkRate = Mathf.Lerp(0.3f, 0.05f, elapsed / duration);
            foreach (var pos in positions)
                SetCellVisible(pos.x, pos.y, visible);
            visible = !visible;
            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate;
        }
    }

    // Places a cross/pattern of cells relative to a center
    private void PlaceCells(int centerX, int centerY, int[][] offsets)
    {
        foreach (var offset in offsets)
        {
            int x = centerX + offset[0];
            int y = centerY + offset[1];
            int p = myGameManager.Pos(x, y);
            myGameManager.cells[p] = 1;
            myGameManager.age[p] = 1;
            myGameManager.colorIndex[p] = (offset[0] == 0 && offset[1] == 0) ? 0 : 1;
            myGameManager.UpdateCellTile(x, y);
        }
    }

    private void SetCellVisible(int i, int j, bool visible)
    {
        if (visible) myGameManager.UpdateCellTile(i, j);
        else tilemap.SetTile(new Vector3Int(i, j, 0), null);
    }

    private void SetLivingCellsVisible(bool visible)
    {
        for (int i = 0; i < myGameManager.gridSize; i++)
            for (int j = 0; j < myGameManager.gridSize; j++)
                if (myGameManager.cells[myGameManager.Pos(i, j)] == 1)
                    SetCellVisible(i, j, visible);
    }

    private System.Collections.Generic.List<Vector3Int> GetAllLivingPositions()
    {
        var list = new System.Collections.Generic.List<Vector3Int>();
        for (int i = 0; i < myGameManager.gridSize; i++)
            for (int j = 0; j < myGameManager.gridSize; j++)
                if (myGameManager.cells[myGameManager.Pos(i, j)] == 1)
                    list.Add(new Vector3Int(i, j, 0));
        return list;
    }

    private void KillLivingCells()
    {
        for (int i = 0; i < myGameManager.gridSize; i++)
            for (int j = 0; j < myGameManager.gridSize; j++)
            {
                int p = myGameManager.Pos(i, j);
                myGameManager.cells[p] = 0;
                myGameManager.age[p] = 0;
                myGameManager.colorIndex[p] = 0;
            }
        myGameManager.UpdateTilemap();
    }
}