using UnityEngine;
using Sirenix.OdinInspector;
using CoffeeCat.FrameWork;
using CoffeeCat.RogueLite;
using CoffeeCat.Utils;
using RandomDungeonWithBluePrint;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Grid = UnityEngine.Grid;

namespace CoffeeCat
{
    public enum RoomType
    {
        PlayerSpawnRoom,
        MonsterSpawnRoom,
        ShopRoom,
        BossRoom,
        RewardRoom,
        EmptyRoom,
        ExitRoom,
    }

    public class StageManager : PrelocatedSingleton<StageManager> {
        [Title("StageManager")]
        [SerializeField] private RandomMapGenerator generator;
        //[SerializeField] private Grid grid = null;
        
        // Fields
        private Room playerCurrentRoom = null;
        
        // Properties
        public bool IsPlayerInsideRoom => playerCurrentRoom != null;
        public int CurrentRoomMonsterKilledCount { get; private set; } = 0;
        public int CurrentFloorMonsterKillCount { get; private set; } = 0;
        public int TotalMonsterKilledCount { get; private set; } = 0;

        [Title("Events")]
        [SerializeField] private UnityEvent<RoomType> OnRoomEntering = null;
        [SerializeField] private UnityEvent<RoomType> OnRoomFirstEntering = null;
        [SerializeField] private UnityEvent OnMonsterKilled = null;
        [SerializeField] private UnityEvent OnPlayerKilled = null;

        private void Update()
        {
            if (!RogueLiteManager.Instance.SpawnedPlayer) {
                return;
            }
            
            // ī�޶� ������Ʈ�� ī�޶󿡼� !
            CameraMovementUpdate();
        }

        private void CameraMovementUpdate() {
            Vector2 playerPos = RogueLiteManager.Instance.SpawnedPlayer.transform.position;
            Camera.main.transform.position = new Vector3(playerPos.x, playerPos.y, Camera.main.transform.position.z);
        }

        public void CreateGate(Field field) {
            foreach (var gate in field.Gates) {
                var spawnedGate = ObjectPoolManager.Instance.Spawn<GateObject>("dungeon_door", Vector3.zero);
                spawnedGate.Initialize(gate.Direction, gate.Position, gate.Room);
                //var cellPosition = grid.CellToWorld(new Vector3Int(gate.Position.x, gate.Position.y, 0));
                //var position = new Vector3(cellPosition.x + grid.cellSize.x * 0.5f, cellPosition.y + grid.cellSize.y * 0.5f, 0);
            }
        }

        public void SpawnPlayer(Field field) {
            // Not Founded Entry Room
            if (!field.TryFindRoomFromType(RoomType.PlayerSpawnRoom, out Room result)) {
                CatLog.WLog("Not Exist PlayerSpawn Room !");
                return;
            }
            
            // Spawn RogueLite Player 
            RogueLiteManager.Instance.SpawnPlayer(result.Rect.center);
            playerCurrentRoom = result;
            playerCurrentRoom.RoomData.EnteredPlayer();
        }

        /// <summary>
        /// Despawn Players
        /// </summary>
        public void DespawnPlayer() => RogueLiteManager.Instance.DespawnPlayer();

        /// <summary>
        /// Despawn All Gates
        /// </summary>
        public void DespawnGates() => ObjectPoolManager.Instance.DespawnAll("dungeon_door");

        public void SetPlayersRoom(Room enteredRoom) {
            // ���� �÷��̾��� ���� �����ϴ� ������ ������������ ���� üũ
            if (playerCurrentRoom != null) {
                CatLog.WLog("Overriding Players CurrentRoom !");
            }
            
            playerCurrentRoom = enteredRoom;
        }

        public void ClearPlayersRoom(Room leavesRoom) {
            // ���� �÷��̾��� ��� �������� ���� ��ġ�ϴ��� Ȯ��
            if (!ReferenceEquals(playerCurrentRoom, leavesRoom)) {
                CatLog.ELog("Player Leaves Room Check Error !");
                return;
            }

            playerCurrentRoom = null;
        }

        #region Events

        public void InvokeEventMonsterKilledEvent(MonsterStatus key) => OnMonsterKilled?.Invoke();

        public void InvokeEventPlayerKilledEvent(Player key) => OnPlayerKilled?.Invoke();

        public void InvokeRoomEnteringEvent(RoomType roomType) => OnRoomEntering?.Invoke(roomType);

        public void InvokeRoomEnteringFirstEvent(RoomType roomType) => OnRoomFirstEntering?.Invoke(roomType);

        #endregion
    }
}
