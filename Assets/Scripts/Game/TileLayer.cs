using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileLayer : MonoBehaviour {

    public GameObject Alley;
    public GameObject Target;
    public GameObject Targeter;

    //indices are y, x
    public BlockModel[][] blockModels;
    private List<BlockModel> blockList = new List<BlockModel>();

    public List<Block> blocks;

    public List<Block> AllBlocks = new List<Block>();

    //how far the layer moves when it moves, should be round numbers
    public Vector3 MoveIncrement;

    public int Spread, Depth, Fill, HP;

    //from 0-1, how likely is it a noise block is in a given cell?
    public float Density;

    public Color[] BlockColors;

    public bool Running = false;

    public int BonusRowsToClearThisFrame = 0;

    public void Reset() {
        for (int i = 0; i < blockModels.Length; i++) {
            for (int j = 0; j < blockModels[i].Length; j++) {
                if (blockModels[i][j] != null) {
                    Destroy(blockModels[i][j]);
                    blockModels[i][j] = null;
                }
            }
        }

        foreach (Block block in blocks) Destroy(block.gameObject);
        blocks.Clear();
        AllBlocks.Clear();
    }

    private List<Block> externalToAdd = new List<Block>();
    public void AddBlockExternal(Block block) { 
        int[] indices = toIndices(block.transform.position);

        BlockModel model = ScriptableObject.CreateInstance<BlockModel>();
        model.Row = indices[0];
        model.Column = indices[1];
        model.Color = block.GetComponent<Renderer>().material.color;
        model.PiecePart = false;
        block.model = model;

        externalToAdd.Add(block);
    }

    private void Awake() {
        blockModels = new BlockModel[Depth+6][]; //extra 6 is for piece origin area
        for(int i = 0; i < blockModels.Length; i++) blockModels[i] = new BlockModel[Spread];

        blocks = new List<Block>();

        Alley.transform.localScale = new Vector3(Spread, 1f, Depth);
        Alley.transform.localPosition = new Vector3(0f, -1f, Depth/2f + 6 - 1);
    }

    private void checkAllBlocks() {
        AllBlocks.RemoveAll(item => item == null);
/*
        foreach (Block block in AllBlocks)
            if (!blocks.Contains(block) && !block.Unplaced)
                Debug.Log("reg blocks is missing block "+block.transform.position);

        foreach (Block a in AllBlocks)
            foreach (Block b in AllBlocks)
                if (a != b && a.transform.position == b.transform.position && !a.Unplaced && !b.Unplaced)
                    Debug.Log("two placed blocks are in the same position!  "+a.transform.position+" ["+a.model.Column+","+a.model.Row+"] and ["+b.model.Column+","+b.model.Row+"]");
*/
    }

    public void Update() {
        //add any that were created externally
        foreach (Block block in externalToAdd) blocks.Add(block);
        externalToAdd.Clear();

        //do any bonus row clearing (do before removing)
        if (BonusRowsToClearThisFrame > 0) { 
            for (int i = 0; i < BonusRowsToClearThisFrame; i++)
                GameController.Instance.ClearRandomBonusRow();
            BonusRowsToClearThisFrame = 0;
        }

        //move or remove any blocks that need to be
        List<Block> blocksToRemove = new List<Block> ();
        foreach (Block block in blocks) {
            Vector3 newPos = toPosition(block.model.Column, block.model.Row);
            if (block.model == null) blocksToRemove.Add(block);
            else if (newPos != block.transform.localPosition && !block.Moving) StartCoroutine(moveBlockTo(block, newPos));
        }
        foreach (Block block in blocksToRemove) StartCoroutine(removeBlock(block));

        //if any models are found that don't have a block, create one for them
        List<Block> blocksToAdd = new List<Block>();
        foreach (BlockModel blockModel in blocksAsList()) {
            bool found = false;
            foreach (Block block in blocks) if (block.model == blockModel) found = true;
            if (!found){
                GameObject newObject = Instantiate(PrefabsManager.Instance.BlockPrefab, transform);
                Block newBlock = newObject.GetComponent<Block>();
                newBlock.transform.position = toPosition(blockModel.Column, blockModel.Row);
                newBlock.model = blockModel;
                newBlock.GetComponent<Renderer>().material.color = blockModel.Color;
                blocksToAdd.Add(newBlock);
            }
        }                        
        foreach (Block block in blocksToAdd) blocks.Add(block);

        //deal with complete rows
        GameController.Instance.CheckForFullRows();

        //deal with empty rows, from row 1 onward (don't come back for row 0)
        for (int rowToClear = 1; rowToClear < blockModels.Length; rowToClear++) {
            bool empty = true;
            for (int col = 0; col < blockModels[rowToClear].Length; col++) 
                if (blockModels[rowToClear][col] != null) 
                    empty = false;

            if (empty) {
                for (int row = rowToClear+1; row < blockModels.Length; row++) {
                    for (int col = 0; col < blockModels[row].Length; col++) {
                        if (blockModels[row][col] != null) {

if (blockModels[row-1][col] != null){
    Debug.Log("block models "+col+","+(row-1)+" was supposed to be empty but is "+blockModels[row-1][col]);
    Debug.Break();
}
                            blockModels[row-1][col] = blockModels[row][col];
                            blockModels[row-1][col].Row = row-1;
                            blockModels[row][col] = null;
                        }
                    }
                }
            }
        }

        //check for victory - any placed blocks in row 0 means target was struck, then destroy those
        bool targetStruck = false;
        for (int col = 0; col < blockModels[0].Count(); col++) {
            if (blockModels[0][col] != null) {
                targetStruck = true;

                blockModels[0][col] = null;

                Block block = null;
                foreach (Block b in blocks) {
                    int[] indices = toIndices(b.transform.position);
                    if (indices[0] == 0 && indices[1] == col)
                        block = b;
                }
                if (block != null) StartCoroutine(removeBlock(block));
            }
        }
        if (targetStruck) GameController.Instance.TargetStruck();

        //check for loss - any placed blocks in the last 6 rows of blockModels means we lose
        bool blockInLossZone = false;
        for (int r = Depth; r < blockModels.Length; r++) 
            for (int c = 0; c < blockModels[r].Length; c++) 
                if (blockModels[r][c] != null) 
                    blockInLossZone = true;
        if (blockInLossZone && Running) GameController.Instance.GameLost();

checkAllBlocks();

        //update targeter
        Piece piece = GameController.Instance.CurrentPiece;
        Targeter.SetActive(Running && piece != null);
        if (Running && piece != null) {
            float minBlockX = float.MaxValue, maxBlockX = float.MinValue;
            foreach (Block block in piece.Blocks) {
                if (block == null) continue; //happens when game won
                if (block.transform.position.x < minBlockX) minBlockX = block.transform.position.x - 0.5f;
                if (block.transform.position.x > maxBlockX) maxBlockX = block.transform.position.x + 0.5f;
            }

            Targeter.transform.localScale = new Vector3(maxBlockX-minBlockX, Depth, 1f);
            Targeter.transform.localPosition = new Vector3((minBlockX+maxBlockX)/2f, -0.49f, Depth /2f + 5);
        }
    }

    //rounds to hundredths
    private float round(float r) { return Mathf.Round(r*100f)/100f; }

    //floor to nearest int or half
    private float halfFloor(float f) { return ((int)f) + ((f - (int)f < 0) ? 0f : 0.5f); }

    public int[] toIndices(Vector3 position) {
        int blockColumn = (int)(Spread/2f - 0.5f + halfFloor(position.x)); // 4.5+x
        int blockRow = (int)(Depth + 4.5f - halfFloor(position.z));        //34.5-z
        return new int[]{ blockRow, blockColumn };
    }

    public Vector3 toPosition(int x, int y) {
        float xPos = x - Spread/2f + 0.5f;
        float yPos = Depth - y + 4.5f;
        return new Vector3(xPos, 0, yPos);
    }

    private IEnumerator moveBlockTo(Block block, Vector3 newPos) {
        block.Moving = true;
        float startTime = Time.time;
        float duration = 0.1f;
        Vector3 originalPosition = block.transform.position;
        while (Time.time-startTime < duration) {
            float ratio = (Time.time-startTime)/duration;
            block.transform.position = Vector3.Lerp(originalPosition, newPos, ratio);
            yield return new WaitForEndOfFrame();
        }
        block.transform.position = newPos;
        block.Moving = false;
    }

    public IEnumerator removeBlock(Block block) {
        if (!block.Removing) {
            block.Removing = true;
            blocks.Remove(block);

            float startTime = Time.time;
            float duration = 0.25f;
            while (Time.time-startTime < duration) {
                float ratio = (Time.time-startTime)/duration;
                Color c = block.GetComponent<Renderer>().material.color;
                block.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 1-ratio);
                yield return new WaitForEndOfFrame();
            }

            Destroy(block.gameObject);
        }
    }

    private List<BlockModel> blocksAsList() {
        blockList.Clear();
        for (int row = blockModels.Length-1; row >= 0; row--) 
            for (int col = 0; col < blockModels[row].Length; col++) 
                if (blockModels[row][col] != null) 
                    blockList.Add(blockModels[row][col]);

        return blockList;
    }
}
