/// CODER	      :		
/// MODIFIED DATE : 
/// IMPLEMENTATION: 
using CoffeeCat.Utils;
using UnityEngine;

namespace CoffeeCat.FrameWork {
	public class RogueLiteManager : GenericSingleton<RogueLiteManager> {
		// Properties
		public Player SpawnedPlayer { get; private set; } = null;
		public Vector3 SpawnedPlayerPosition => SpawnedPlayer.Tr.position;

		// Fields
		private const string playerKey = "Player";

		protected override void Initialize() {
			base.Initialize();
		}

		public void SpawnPlayer(Vector2 spawnPosition) {
			if (!ObjectPoolManager.Instance.IsExistInPoolDictionary(playerKey)) {
				var origin = ResourceManager.Instance.AddressablesSyncLoad<GameObject>(playerKey, true);
				ObjectPoolManager.Instance.AddToPool(PoolInformation.New(origin, true, 1));
			}

			SpawnedPlayer = ObjectPoolManager.Instance.Spawn<Player>(playerKey, spawnPosition);
		}

		public void DespawnPlayer() {
			if (!SpawnedPlayer) {
				return;
			}
			
			ObjectPoolManager.Instance.Despawn(SpawnedPlayer.gameObject);
		}

		public bool IsPlayerExistAndAlive() => SpawnedPlayer && SpawnedPlayer.State != PlayerState.Dead;

		public bool IsPlayerNotExistOrDeath() => !SpawnedPlayer || SpawnedPlayer.State == PlayerState.Dead;
	}
}
