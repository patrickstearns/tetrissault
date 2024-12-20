using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using static Unity.VisualScripting.Member;

public class GameController : MonoBehaviour, GameControls.IGameActions {

    private static GameController _instance;
    public static GameController Instance { get { return _instance; } }

    private GameControls controls;

    public int BonusRows = 10;
    public PieceOrigin PieceOrigin;
    public TileLayer TileLayer;
    public GameObject Targeter;
    public BonusCounter BonusCounter;
    public TextMeshPro LinesLabel;
    public HPMeter HPMeter;
    public GameTimer GameTimer;

    public Piece CurrentPiece;
    private Piece NextPiece;
    private Vector3 LastFiredPieceFrom = new Vector3(0.5f, 0f, 1.5f);

    public List<Piece> PiecesInFlight = new List<Piece>();

    private bool leftHeld = false, rightHeld = false;
    private float lastMoveTime = 0f;
    private float moveDelay = 0.1f;
    private int clearedRows = 0, totalClearedRows = 0;
    private int targetHp = 0;
    private bool specialEnabled = false;

    public GameOverMenu GameOverMenu;
    public GameWonMenu GameWonMenu;

    public CinemachineVirtualCamera ConsoleCam, TargetCam, PlayCam;

    private AudioManager audioManager;

    void Awake() { 
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else { _instance = this; }
    }

    void OnEnable() {
        if (controls == null){
            controls = new GameControls();
            controls.Game.SetCallbacks(this);
        }
    }
    void OnDisable() { if (controls != null) controls.Game.Disable(); }

    void Start() {
        BeginGame();

        StartCoroutine(startAfterOneTick());
    }

    private IEnumerator startAfterOneTick() {
        yield return new WaitForEndOfFrame();

        audioManager = FindObjectOfType<AudioManager>();
        audioManager.SetGameMusicPlaying(true);        
    }

    public void BeginGame() {
        //clear any existing blocks, score etc.
        GameWonMenu.gameObject.SetActive(false);
        GameOverMenu.gameObject.SetActive(false);
        TileLayer.Reset();
        BonusCounter.Value = 0;
        LinesLabel.text = "0\nLines";
        clearedRows = 0;
        totalClearedRows = 0;

        PlayCam.Priority = 10;
        ConsoleCam.Priority = 0;

        //start up new stuff
        targetHp = TileLayer.HP;
        HPMeter.SetValue(TileLayer.HP);
        StartCoroutine(CreateNoise(TileLayer.Fill-1));
        NextPiece = generatePiece();
        ReleasePiece();
        GameTimer.SetRunning(true);
        TileLayer.Running = true;

        controls.Game.Enable();
    }

    private IEnumerator CreateNoise(int rows) {
        yield return new WaitForEndOfFrame();
        CreateNoiseRow(1);
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < rows; i++){
            AdvanceTiles();
            CreateNoiseRow(1);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void ReleasePiece() {
        StartCoroutine(releasePieceInternal(NextPiece));
        NextPiece = generatePiece();
    }

    private IEnumerator releasePieceInternal(Piece piece) {
        float duration = 0.1f;
        float startTime = Time.time;
        Vector3 initPos = piece.transform.position;
        while (Time.time-startTime < duration) {
            float ratio = (Time.time-startTime)/duration;
            piece.transform.position = Vector3.Lerp(initPos, LastFiredPieceFrom, ratio);     
            yield return new WaitForEndOfFrame();
        }
        piece.transform.position = LastFiredPieceFrom;
        CorrectPositionAfterRotation(piece);

        CurrentPiece = piece;
    }

    private Piece generatePiece() {
        GameObject newObject = Instantiate(PrefabsManager.Instance.PiecePrefabs[Random.Range(0, PrefabsManager.Instance.PiecePrefabs.Count)]);
        Piece newPiece = newObject.GetComponent<Piece>();
        newPiece.transform.position = PieceOrigin.NextPieceEmitter.transform.position + new Vector3(0, 1f, 0);

        //idk why but instantiating the prefabs seems to unset this; figure out what's up with having to do this silliness
        for (int i = 0; i < 4; i++) newPiece.Blocks[i] = newPiece.transform.GetChild(i).GetComponent<Block>();

        return newPiece;
    }

    public bool CanMoveLoadedPiece(Piece piece, Vector3 movement) {
        float minX = -PieceOrigin.Frame.transform.localScale.x/2f, maxX = PieceOrigin.Frame.transform.localScale.x/2f;
        foreach (Block block in piece.Blocks) {
            if (block.transform.position.x + movement.x <= minX || block.transform.position.x + movement.x >= maxX) return false;
        }
        return true;
    }

    public void CorrectPositionAfterRotation(Piece piece) {
        float minX = -PieceOrigin.Frame.transform.localScale.x/2f, maxX = PieceOrigin.Frame.transform.localScale.x/2f;
        float minZ = 1, maxZ = 5;

        Vector3 correction;
        do {
            correction = Vector3.zero;
            foreach (Block block in piece.Blocks) {
                if (block.transform.position.x <= minX) correction = new Vector3(1, 0, 0);
                else if (block.transform.position.x >= maxX) correction = new Vector3(-1, 0, 0);
                else if (block.transform.position.z <= minZ) correction = new Vector3(0, 0, 1);
                else if (block.transform.position.z >= maxZ) correction = new Vector3(0, 0, -1);
            }
            piece.transform.position += correction;
        }
        while (correction != Vector3.zero);
    }

    void FixedUpdate() {
        if (leftHeld && Time.time-lastMoveTime >= moveDelay) move(new Vector3(-1f, 0f, 0f));
        if (rightHeld && Time.time-lastMoveTime >= moveDelay) move(new Vector3(1f, 0f, 0f));
    }

    public void AdvanceTiles() {
        foreach (Piece piece in PiecesInFlight) piece.transform.position += new Vector3(0f, 0f, -1f);

        //move (unowned) cubes up one row
        for (int row = TileLayer.blockModels.Length-1; row >= 0; row--) {
            for (int col = 0; col < TileLayer.blockModels[row].Length; col++) {
                if (TileLayer.blockModels[row][col] != null) {
                    TileLayer.blockModels[row+1][col] = TileLayer.blockModels[row][col];
                    TileLayer.blockModels[row][col] = null;
                    if (TileLayer.blockModels[row+1][col] != null) TileLayer.blockModels[row+1][col].Row = row+1;
                }
            }
        }
    }

    public void TimerDinged() { StartCoroutine(timerDingedInternal()); }
    private IEnumerator timerDingedInternal() {
        //ding noise, pause timer
        AudioManager.Instance.PlaySFX(AudioManager.Instance.ding);
        GameTimer.SetRunning(false);

        //shake a little
        AudioSource rumbler = AudioManager.Instance.PlaySFX(AudioManager.Instance.rumble);
        rumbler.loop = true;

        float duration = 1f;
        float startTime = Time.time;
        while (Time.time-startTime < duration) {
            TileLayer.Target.transform.LookAt(PieceOrigin.transform.position + Random.insideUnitSphere);
            TileLayer.Target.transform.rotation *= Quaternion.Euler(180, 0, 180);
            yield return new WaitForEndOfFrame();
        }
        TileLayer.Target.transform.rotation = Quaternion.Euler(0, 0, 0);
        rumbler.loop = false;
        rumbler.Stop();
        yield return new WaitForSeconds(0.1f);

        AdvanceTiles();
        CreateNoiseRow(1);

        GameTimer.SetRunning(true);
    }

    public void OnMove(InputAction.CallbackContext context) {
        if (CurrentPiece == null) return;

        if (context.performed || context.canceled) {
            leftHeld = false;
            rightHeld = false;
        }

        if (context.performed) {            
            Vector2 move = context.ReadValue<Vector2>();
            if (move.y < 0) rotate(new Vector3(0f, 90f, 0f));
            else if (move.y > 0) rotate(new Vector3(0f, -90f, 0f));
            else if (move.x < 0) leftHeld = true;
            else if (move.x > 0) rightHeld = true;
        }
    }

    private void rotate(Vector3 euler) {
        CurrentPiece.transform.rotation *= Quaternion.Euler(euler);
        CorrectPositionAfterRotation(CurrentPiece);

        AudioManager.Instance.PlaySFX(AudioManager.Instance.rotate);
    }

    private void move(Vector3 move) {
        if (CanMoveLoadedPiece(CurrentPiece, move)) {
            CurrentPiece.transform.position += move;
            lastMoveTime = Time.time;

            AudioManager.Instance.PlaySFX(AudioManager.Instance.move);
        }
    }

    private float lastFireTime = 0;
    private static float FireDelay = 0.2f;

    public void OnConfirm(InputAction.CallbackContext context) {
        if (!context.performed) return;

        if (CurrentPiece != null && Time.time-lastFireTime >= FireDelay){
            lastFireTime = Time.time;
            LastFiredPieceFrom = CurrentPiece.transform.position;
            StartCoroutine(firePiece(CurrentPiece, TileLayer.MoveIncrement));
            StartCoroutine(delayAndReleasePiece());
        }
    }

    private IEnumerator triggerBonus(int count) {
        for (int i = 0; i < count; i++) {   
            TileLayer.BonusRowsToClearThisFrame++;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator firePiece(Piece piece, Vector3 movement) {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.select);

        PiecesInFlight.Add(piece);
        while (CanMoveFiredPiece(piece, movement)) {
            piece.transform.position += movement * Time.deltaTime * 100f;
            yield return new WaitForEndOfFrame();
        }

        PlacePiece(piece);
        PiecesInFlight.Remove(piece);
        Destroy(piece.gameObject);
    }

    private IEnumerator delayAndReleasePiece() {
        yield return new WaitForSeconds(0.1f);
        ReleasePiece();
    }

    public void OnSpecial(InputAction.CallbackContext context) {
        if (context.performed && specialEnabled) {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.special);

            clearedRows = 0;
            specialEnabled = false;
            BonusCounter.Value = 0;
            StartCoroutine(triggerBonus(2));
        }
    }

    public void TargetStruck() {
        targetHp--;
        HPMeter.SetValue(targetHp);
        if (targetHp > 0) {
            TileLayer.Fill++;
            GameTimer.TimeToIncrement *= 5f/6f;
            StartCoroutine(targetStruckInternal());
        }
        else{
            StopAllCoroutines();
            StartCoroutine(gameWonInternal());
        }
    }

    private IEnumerator targetStruckInternal() {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.struck);
        
        //pause timer
        GameTimer.SetRunning(false);

        //shake a lot
        AudioSource rumbler = AudioManager.Instance.PlaySFX(AudioManager.Instance.rumble);
        rumbler.loop = true;

        float duration = 1f;
        float startTime = Time.time;
        while (Time.time-startTime < duration) {
            TileLayer.Target.transform.LookAt(PieceOrigin.transform.position + Random.insideUnitSphere * 3f);
            TileLayer.Target.transform.rotation *= Quaternion.Euler(180, 0, 180);
            yield return new WaitForEndOfFrame();
        }

        TileLayer.Target.transform.rotation = Quaternion.Euler(0, 0, 0);
        rumbler.loop = false;
        rumbler.Stop();
        yield return new WaitForSeconds(0.1f);

        AdvanceTiles();
        StartCoroutine(CreateNoise(TileLayer.Fill-1));

        GameTimer.SetRunning(true);
    }

    private IEnumerator gameWonInternal() {
        //disable controls
        controls.Game.Disable();

        PlayCam.Priority = 0;
        TargetCam.Priority = 10;

        //stop timer
        GameTimer.SetRunning(false);
        TileLayer.Running = false;

        //destroy "current" and "next" pieces and any in flight
        List<Piece> piecesToDestroy = new List<Piece>();
        piecesToDestroy.Add(CurrentPiece);
        piecesToDestroy.Add(NextPiece);
        foreach (Piece piece in piecesToDestroy) {
            if (piece == null) continue; //already destroyed?
            foreach (Block block in piece.Blocks) 
                StartCoroutine(TileLayer.removeBlock(block));
            yield return new WaitForSeconds(0.05f);
        }

        //shake a lot
        AudioSource rumbler = AudioManager.Instance.PlaySFX(AudioManager.Instance.rumble);
        rumbler.loop = true;

        float duration = 1f;
        float startTime = Time.time;
        while (Time.time-startTime < duration) {
            TileLayer.Target.transform.LookAt(PieceOrigin.transform.position + Random.insideUnitSphere * 3f);
            TileLayer.Target.transform.rotation *= Quaternion.Euler(180, 0, 180);
            yield return new WaitForEndOfFrame();
        }
        TileLayer.Target.transform.rotation = Quaternion.Euler(0, 0, 0);
        rumbler.loop = false;
        rumbler.Stop();
        yield return new WaitForSeconds(0.1f);

        //timer explodes off
        Vector3 forcePos = Random.insideUnitCircle * 4;
        forcePos.y = 0;
        forcePos += GameTimer.transform.position;
        
        GameTimer.GetComponent<Rigidbody>().useGravity = true;
        GameTimer.GetComponent<Rigidbody>().AddForceAtPosition((Vector3.up + Vector3.forward) * 4f, forcePos, ForceMode.Impulse);
        Explosion.Create(GameTimer.transform.position + Vector3.back * 3f);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.explode);

        //wait a second
        yield return new WaitForSeconds(1f);

        foreach (Piece piece in piecesToDestroy) Destroy(piece);
        CurrentPiece = null;
        NextPiece = null;

        //show 'game won' UI
        GameWonMenu.Show();
    }

    public void GameLost() { 
        StopAllCoroutines();
        StartCoroutine(gameLostInternal()); 
    }
    private IEnumerator gameLostInternal() {
        //disable controls
        controls.Game.Disable();

        PlayCam.Priority = 0;
        ConsoleCam.Priority = 10;

        //stop timer
        GameTimer.SetRunning(false);
        TileLayer.Running = false;

        //destroy "current" and "next" pieces and any in flight
        List<Piece> piecesToDestroy = new List<Piece>();
        piecesToDestroy.Add(CurrentPiece);
        piecesToDestroy.Add(NextPiece);
        foreach (Piece piece in piecesToDestroy) {
            if (piece == null) continue; //already destroyed?
            foreach (Block block in piece.Blocks) 
                StartCoroutine(TileLayer.removeBlock(block));
            yield return new WaitForSeconds(0.05f);
        }

        //wait a second
        yield return new WaitForSeconds(1f);

        foreach (Piece piece in piecesToDestroy) Destroy(piece);
        CurrentPiece = null;
        NextPiece = null;

        //show 'game over' UI
        GameOverMenu.Show();
    }

    public void CreateNoiseRow(int row) {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.barf);

        int mustBeEmpty = UnityEngine.Random.Range(0, TileLayer.blockModels[row].Length-1); //guarantee at least one open spot per row
        for (int i = 0; i < TileLayer.blockModels[row].Length; i++) {
            if (UnityEngine.Random.value < TileLayer.Density && i != mustBeEmpty) {
                TileLayer.blockModels[row][i] = ScriptableObject.CreateInstance<BlockModel>();
                TileLayer.blockModels[row][i].Row = row;
                TileLayer.blockModels[row][i].Column = i;
                TileLayer.blockModels[row][i].Color = TileLayer.BlockColors[UnityEngine.Random.Range(0, TileLayer.BlockColors.Length)];
            }
        }
    }

    public bool CanMoveFiredPiece(Piece piece, Vector3 movement) {
        //piece blocks don't have block models
        foreach (Block block in piece.Blocks) {
            int[] indices = TileLayer.toIndices(block.transform.position + movement);
            int blockRow = indices[0];
            int blockColumn = indices[1];
            if (blockRow < 0) return false; 
            if (TileLayer.blockModels[blockRow][blockColumn] != null) return false;
        }
        return true;
    }

    public void PlacePiece(Piece piece) {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.attach);

        for (int i = 0; i < 2; i++) {
            int numToMove = 0;
            foreach (Block block in piece.Blocks) {
                int[] ind = TileLayer.toIndices(block.transform.position);
                if (TileLayer.blockModels[ind[0]][ind[1]]) {
                    numToMove++;
                }
            }
            piece.transform.position += new Vector3(0, 0, -numToMove);
        }

        foreach (Block block in piece.Blocks) {
            GameObject newObject = Instantiate(PrefabsManager.Instance.BlockPrefab, transform);
            Block newBlock = newObject.GetComponent<Block>();
            newBlock.transform.position = block.transform.position;
            newBlock.GetComponent<Renderer>().material.color = block.GetComponent<Renderer>().material.color;
            newBlock.Unplaced = false;

            TileLayer.AddBlockExternal(newBlock);
            TileLayer.blockModels[newBlock.model.Row][newBlock.model.Column] = newBlock.model;
        }
    }

    public void ClearRandomBonusRow() {
        if (TileLayer.blocks.Count == 0) return; //if there are no blocks, we can't do this ever

        int randomRowWithBlock = -1;
        while (randomRowWithBlock == -1) {
            List<int> rowsWithBlocks = new List<int>();
            foreach (Block block in TileLayer.blocks) if (!rowsWithBlocks.Contains(block.model.Row)) rowsWithBlocks.Add(block.model.Row);
            randomRowWithBlock = rowsWithBlocks[Random.Range(0, rowsWithBlocks.Count)];
        }

        for (int col = 0; col < TileLayer.blockModels[randomRowWithBlock].Length; col++) {
            if (TileLayer.blockModels[randomRowWithBlock][col] != null) {
                Vector3 blockPos = TileLayer.toPosition(col, randomRowWithBlock);
                Block b = null;
                foreach (Block block in TileLayer.blocks)
                    if (block.transform.position == blockPos)
                        b = block;
                TileLayer.removeBlock(b);
                Destroy(TileLayer.blockModels[randomRowWithBlock][col]);
                TileLayer.blockModels[randomRowWithBlock][col] = null;
            }
        }
    }

    public void CheckForFullRows() { StartCoroutine(findAndClearCompleteRowsInternal()); }
    private IEnumerator findAndClearCompleteRowsInternal() {
        //find all complete rows
        List<int> completeRows = new List<int>();
        for (int row = TileLayer.blockModels.Length-1; row >= 0; row--) {
            int occupiedCount = 0;
            for (int col = 0; col < TileLayer.blockModels[row].Length; col++) 
                if (TileLayer.blockModels[row][col] != null) 
                    occupiedCount++;
            if (occupiedCount == TileLayer.blockModels[row].Length)
                completeRows.Add(row);
        }    

        for (int i = 0; i < completeRows.Count; i++) {
            int rowToClear = completeRows[i];
            AudioManager.Instance.PlaySFX(AudioManager.Instance.clear);
            
            //destroy all blockModels in the target row
            for (int col = 0; col < TileLayer.blockModels[rowToClear].Length; col++) {
                Destroy(TileLayer.blockModels[rowToClear][col]);
                TileLayer.blockModels[rowToClear][col] = null;
            }
        }

        int rows = completeRows.Count;
        totalClearedRows += rows;
        LinesLabel.text = totalClearedRows+"\nLines";

        clearedRows += rows;
        if (clearedRows >= BonusRows) {
            clearedRows = BonusRows;

            if (!specialEnabled) AudioManager.Instance.PlaySFX(AudioManager.Instance.ready);

            specialEnabled = true;
            //StartCoroutine(triggerBonus(2));
        }
        BonusCounter.Value = ((float)clearedRows/(float)BonusRows);

        yield return null;
    }
}
