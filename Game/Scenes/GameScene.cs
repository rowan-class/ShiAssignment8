using Microsoft.Xna.Framework.Graphics;
using GameBase.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameBase;

public class GameScene : Scene
{
   
    int mapSize = 5;
    int mapWidth = 16;
    GameObject[,] map;
    bool playerActionTaken = false;
    private bool gameOver = false;
    private bool restartRequested = false;

    GameObject Player;
    public enum GameState
    {
        PlayerTurn,
        EnemyTurn
    }
    StateMachine stateMachine = new StateMachine();

    public GameScene(ContentManager content) : base(content)
    {
        stateMachine.SetNumOfStates(2);
        stateMachine.SetStateFunctions((int)GameState.PlayerTurn, EnterPlayerTurn, UpdatePlayerTurn, ExitPlayerTurn);
        stateMachine.SetStateFunctions((int)GameState.EnemyTurn, EnterEnemyTurn, UpdateEnemyTurn, ExitEnemyTurn);

        exitSprite = Services.Sprites.CreateSprite("placeholder1");
        exitSprite.Color = Color.LightGreen;
        itemSprite = Services.Sprites.CreateSprite("placeholder");
        itemSprite.Color = Color.Gold;
        itemUI = new GameSceneUI(this);
        sceneUI = itemUI;
        ResetLevel();
    }

    enum Relic { None, Bomb, Potion, Rubber }
    GameSceneUI itemUI;
    Sprite exitSprite, itemSprite;
    Point exitTile, itemTile;
    Relic active, passive, floor;
    int level = 1, enemiesLeft, cooldown, potionTurns;
    bool cleared, nextLevel;

    void OnPlayerMoveRequested(Vector2 direction)
    {
        if (!isActive || gameOver || nextLevel || Player == null || !Player.IsActive)
        {
            return;
        }

        if (stateMachine.CurrentState != (int)GameState.PlayerTurn)
        {
            return;
        }

        if (playerActionTaken)
        {
            return;
        }

        var tile = WorldToTilePosition(Player.transform.position);

        Vector2 destination = new Vector2(tile.x, tile.y) + direction;

        bool actionTaken = Move(Player, destination);

        if (actionTaken && cleared)
        {
            var pos = WorldToTilePosition(Player.transform.position);
            Point currentTile = new Point(pos.x, pos.y);
            if (currentTile == itemTile && floor != Relic.None)
            {
                if (floor == Relic.Rubber) { passive = floor; }
                else
                {
                    active = floor;
                    cooldown = 0;
                }
                floor = Relic.None;
            }
            if (currentTile == exitTile)
            {
                nextLevel = true;
                return;
            }
        }
        if (actionTaken)
        {
            playerActionTaken = true;
            stateMachine.SetState((int)GameState.EnemyTurn);
        }
    }
    public override void EnterScene()
    {

        if (isActive) {return;}

        base.EnterScene();
        RefreshItemUI();
        EventSystem.Death += OnDeath;
        Services.Input.ActionPressed += OnActionPressed;

    }
    public override void Update(GameTime gameTime)
    {
        if (restartRequested || nextLevel)
        {
            if (nextLevel) { level++; }
            else
            {
                level = 1;
                active = passive = Relic.None;
                cooldown = potionTurns = 0;
            }
            ResetLevel();
            RefreshItemUI();
            return;
        }

        if (!gameOver)
        {
            stateMachine.UpdateState(gameTime);
        }
        base.Update(gameTime);
        RefreshItemUI();
    }
    public override void ExitScene()
    {
        if (!isActive)
        {
            return;
        }

        EventSystem.Death -= OnDeath;
        Services.Input.ActionPressed -= OnActionPressed;
        base.ExitScene();
    }


    public void EnterPlayerTurn()
    {
        playerActionTaken = false;
        Player.GetComponent<PlayerInputComponent>().SetActive(true);
    }
    public void UpdatePlayerTurn(GameTime gameTime)
    {

    }
    public void ExitPlayerTurn()
    {
        Player.GetComponent<PlayerInputComponent>().SetActive(false);
    }
    public void EnterEnemyTurn()
    {
        List<GameObject> enemies = new List<GameObject>();
        foreach (GameObject obj in map)
        {
            if (obj != null && obj.layer == Layer.Enemy)
            {
                enemies.Add(obj);
            }
        }
        foreach (GameObject enemy in enemies)
        {
            if (!enemy.IsActive) {continue; }
            Vector2 direction = FindBestMove(enemy);

            if (direction == Vector2.Zero) {continue; }

            var tile = WorldToTilePosition(enemy.transform.position);
            Vector2 destination = new Vector2(tile.x, tile.y) + direction;

            Move(enemy, destination);
            if (gameOver)
            {
                return;
            }
        }

        if (potionTurns > 0) { potionTurns--; }
        if (cooldown > 0) { cooldown--; }
        stateMachine.SetState((int)GameState.PlayerTurn);
    }
    public void UpdateEnemyTurn(GameTime gameTime)
    {
    }
    public void ExitEnemyTurn()
    {
    }
    public Vector2 FindBestMove(GameObject obj)
    {
        Point objp = new Point(WorldToTilePosition(obj.transform.position).x, WorldToTilePosition(obj.transform.position).y);
        Point pobj = new Point(WorldToTilePosition(Player.transform.position).x, WorldToTilePosition(Player.transform.position).y);
        List<PathNode> path = PathFinding.FindPath(objp, pobj, IsWalkable, mapSize);

        if (path.Count > 1)
        {
            Point current = path[0].loc;
            Point next = path[1].loc;

            Vector2 direction = new Vector2(next.X - current.X,next.Y - current.Y);
            return direction;
        }
        return Vector2.Zero;
    }

    public bool Move(GameObject obj, Vector2 position)
    {
        if (position.X >= 0 && position.X < mapSize && position.Y >= 0 && position.Y < mapSize)
        {
            if (map[(int)position.X, (int)position.Y] == null || map[(int)position.X, (int)position.Y].layer == Layer.Interactable)
            {
                var pos = WorldToTilePosition(obj.transform.position);
                map[pos.x, pos.y] = null;
                map[(int)position.X, (int)position.Y] = obj;

                obj.UpdateLocation(TileToWorldPosition((int)position.X, (int)position.Y));
                return true;
            }
            else if (map[(int)position.X, (int)position.Y] != null && map[(int)position.X, (int)position.Y].layer != Layer.Interactable)
            {
                if (map[(int)position.X, (int)position.Y].layer == Layer.Enemy && stateMachine.CurrentState == (int)GameState.PlayerTurn){
                    map[(int)position.X, (int)position.Y].GetComponent<IDamagable>().takeDamage(1);
                    return true;
                }
                else if (map[(int)position.X, (int)position.Y].layer == Layer.Player && stateMachine.CurrentState == (int)GameState.EnemyTurn)
                {
                    if (passive == Relic.Rubber)
                    {
                        var tile = WorldToTilePosition(obj.transform.position);
                        int x = 2 * tile.x - (int)position.X;
                        int y = 2 * tile.y - (int)position.Y;
                        if (x >= 0 && x < mapSize && y >= 0 && y < mapSize && map[x, y] == null)
                        {
                            Move(obj, new Vector2(x, y));
                        }
                        else
                        {
                            obj.GetComponent<IDamagable>().takeDamage(1);
                        }
                    }
                    else if (potionTurns == 0) { 
                        Player.GetComponent<IDamagable>().takeDamage(1); 
                    }
                    return true;
                }
            }
        }
        return false;
    }


    public (int x, int y) WorldToTilePosition(Vector2 worldPosition)
    {
        int tileX = (int)MathF.Floor(worldPosition.X / mapWidth);
        int tileY = (int)MathF.Floor(worldPosition.Y / mapWidth);
        return (tileX, tileY);
    }
    public Vector2 TileToWorldPosition(int tileX, int tileY)
    {

        float worldX = tileX * mapWidth;
        float worldY = tileY * mapWidth;
        return new Vector2(worldX, worldY);
    }

    public bool IsWalkable(Point loc)
    {
        if (loc.X >= 0 && loc.X < mapSize && loc.Y >= 0 && loc.Y < mapSize)
        {
            return (map[loc.X, loc.Y] == null || map[loc.X, loc.Y].layer == Layer.Interactable || map[loc.X, loc.Y].layer == Layer.Player);

        }
        return false;
    }

    public void ResetLevel()
    {
        if (Player != null)
        {
            Player.GetComponent<PlayerInputComponent>().MoveRequested-= OnPlayerMoveRequested;
        }

        ClearObjects();
        enemiesLeft = 3;
        cleared = nextLevel = false;
        floor = Relic.None;
        map = new GameObject[mapSize, mapSize];
        Player = Prefabs.CreatePlayer(this,Services.Sprites.CreateSprite("placeholder1"),TileToWorldPosition(0, 0));
        Player.GetComponent<PlayerInputComponent>().MoveRequested+= OnPlayerMoveRequested;
        map[0, 0] = Player;
        AddObject(Player);

        SpawnObj(enemiesLeft, (scene, position) => Prefabs.CreateEnemy(scene,Services.Sprites.CreateSprite("placeholder"),position));
        ProcessObj();

        gameOver = false;
        restartRequested = false;
        playerActionTaken = false;

        stateMachine.SetState((int)GameState.PlayerTurn);
    }

    public void SpawnObj(int amount, Func<Scene, Vector2, GameObject> createObject) {
        List<Point> spawnPoints = new List<Point>();
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                if (map[x, y] == null)
                {
                    spawnPoints.Add(new Point(x, y));
                }
            }
        }
        for (int i = 0; i < amount; i++)
        {
            if (spawnPoints.Count == 0)
            {
                break;
            }
            Point spawnPoint = spawnPoints[Random.Shared.Next(0, spawnPoints.Count)];
            GameObject enemy = createObject(this, new Vector2(spawnPoint.X * mapWidth, spawnPoint.Y * mapWidth));
            AddObject(enemy);
            map[spawnPoint.X, spawnPoint.Y] = enemy;
            spawnPoints.Remove(spawnPoint);
        }
    }

    public void OnDeath(GameObject obj)
    {
        if (!isActive || obj.ownerScene != this)
        { return;}

        if (obj == Player)
        {
            gameOver = true;
            playerActionTaken = true;
            Player.SetActive(false);
            return;
        }

        if (obj.layer == Layer.Enemy)
        {
            var tile = WorldToTilePosition(obj.transform.position);
            if (map[tile.x, tile.y] == obj) { map[tile.x, tile.y] = null; }
            RemoveObject(obj);
            enemiesLeft--;
            if (enemiesLeft == 0)
            {
                List<Point> empty = new();
                for (int x = 0; x < mapSize; x++)
                {

                    for (int y = 0; y < mapSize; y++)
                    {
                        if (map[x, y] == null)
                        { empty.Add(new Point(x, y)); }
                    }
                }

                exitTile = empty[Random.Shared.Next(empty.Count)];
                empty.Remove(exitTile);
                itemTile = empty[Random.Shared.Next(empty.Count)];
                Relic[] drops = { Relic.Bomb, Relic.Potion, Relic.Rubber };
                floor = drops[(level - 1) % drops.Length];
                cleared = true;
            }
        }
    }

    public void UseItem()
    {
        if (gameOver || nextLevel || playerActionTaken || active == Relic.None || cooldown > 0) { return; }
        if (active == Relic.Potion) { potionTurns = 2; }
        if (active == Relic.Bomb)
        {
            var pos = WorldToTilePosition(Player.transform.position);
            for (int x = 0; x < mapSize; x++)
                for (int y = 0; y < mapSize; y++)
                    if (Math.Abs(x - pos.x) + Math.Abs(y - pos.y) <= 2)
                    {
                        if (map[x, y] == Player && potionTurns > 0) { continue; }
                        map[x, y]?.GetComponent<IDamagable>().takeDamage(1);
                    }
        }
        // moving or bumping advances the cooldown
        cooldown = 3;
    }

    void RefreshItemUI()
    {
        itemUI.UpdateSlots(active.ToString(), passive.ToString(), cooldown, active != Relic.None && !gameOver);
    }

    public override void Draw(SpriteBatch batch)
    {
        base.Draw(batch);
        if (!cleared) { return; }
        exitSprite.Draw(batch, TileToWorldPosition(exitTile.X, exitTile.Y));
        if (floor != Relic.None)
        {
            itemSprite.Draw(batch, TileToWorldPosition(itemTile.X, itemTile.Y));
        }
    }
    void OnActionPressed(GameAction action)
    {
        if (isActive && gameOver && action == GameAction.Restart)
        {
            restartRequested = true;
        }
    }
}
