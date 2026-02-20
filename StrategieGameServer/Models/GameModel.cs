using System;
using System.Collections.Generic;
using System.Linq;

namespace StrategieGameServer.Models
{
    public class GameModel
    {
        public const int GRID_SIZE = 30;
        public Tile[,] Tiles { get; set; } = new Tile[GRID_SIZE, GRID_SIZE];
        public List<Unit> Units { get; set; } = new();
        public List<Item> Items { get; set; } = new();
        public List<Building> Buildings { get; set; } = new();
        public int CurrentTurnPlayerId { get; set; } = 0;

        public GameModel()
        {
            var random = new Random();
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    double rand = random.NextDouble();
                    string type;
                    if (rand < 0.1) type = "MOUNTAIN";
                    else if (rand < 0.2) type = "WATER";
                    else if (rand < 0.4) type = "FOREST";
                    else type = "PLAINS";
                    Tiles[y, x] = new Tile(x, y, type);
                }
            }
        }

        public bool MoveUnit(int unitId, int targetX, int targetY)
        {
            var unit = Units.FirstOrDefault(u => u.Id == unitId);
            if (unit == null) return false;
            if (unit.PlayerId != CurrentTurnPlayerId) return false;

            int dx = Math.Abs(targetX - unit.GridX);
            int dy = Math.Abs(targetY - unit.GridY);
            if (dx > 1 || dy > 1) return false;
            if (dx == 0 && dy == 0) return false;

            var targetTile = Tiles[targetY, targetX];
            if (targetTile.Type == "WATER") return false;
            bool occupied = Units.Any(u => u != unit && u.GridX == targetX && u.GridY == targetY);
            if (occupied) return false;

            unit.MoveTo(targetX, targetY);

            if (targetTile.HasTrap)
            {
                unit.Hp -= 20;
                targetTile.HasTrap = false;
            }

            var item = Items.FirstOrDefault(i => i.GridX == targetX && i.GridY == targetY);
            if (item != null)
            {
                if (item.Type == "HEALTH_PACK") unit.Hp = Math.Min(unit.MaxHp, unit.Hp + 30);
                Items.Remove(item);
            }
            return true;
        }

        public bool AttackUnit(int attackerId, int targetX, int targetY)
        {
            var attacker = Units.FirstOrDefault(u => u.Id == attackerId);
            if (attacker == null) return false;
            if (attacker.PlayerId != CurrentTurnPlayerId) return false;

            var target = Units.FirstOrDefault(u => u.GridX == targetX && u.GridY == targetY);
            if (target == null || target == attacker) return false;

            int dx = Math.Abs(targetX - attacker.GridX);
            int dy = Math.Abs(targetY - attacker.GridY);
            if (dx > 1 || dy > 1) return false;

            target.Hp -= 25;
            if (target.Hp <= 0) Units.Remove(target);
            return true;
        }

        public bool PlaceTrap(int unitId, int targetX, int targetY)
        {
            var unit = Units.FirstOrDefault(u => u.Id == unitId);
            if (unit == null) return false;
            if (unit.PlayerId != CurrentTurnPlayerId) return false;

            int dx = Math.Abs(targetX - unit.GridX);
            int dy = Math.Abs(targetY - unit.GridY);
            if (dx > 1 || dy > 1) return false;
            if (dx == 0 && dy == 0) return false;

            var tile = Tiles[targetY, targetX];
            if (tile.HasTrap) return false;

            tile.HasTrap = true;
            return true;
        }

        public void SpawnRandomItem()
        {
            var random = new Random();
            int x, y;
            Tile tile;
            do
            {
                x = random.Next(GRID_SIZE);
                y = random.Next(GRID_SIZE);
                tile = Tiles[y, x];
            } while (tile.Type == "WATER" || Units.Any(u => u.GridX == x && u.GridY == y) || Items.Any(i => i.GridX == x && i.GridY == y));

            double r = random.NextDouble();
            string type;
            if (r < 0.2) type = "HEALTH_PACK";
            else if (r < 0.3) type = "BOMB1";
            else if (r < 0.4) type = "BOMB2";
            else if (r < 0.5) type = "BOMB3";
            else if (r < 0.6) type = "BLOP";
            else if (r < 0.7) type = "WHITE_BLOP";
            else if (r < 0.8) type = "EXPLOSION1";
            else if (r < 0.9) type = "EXPLOSION2";
            else type = "EXPLOSION3";

            Items.Add(new Item(x, y, type));
        }
    }
}