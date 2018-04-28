using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Assets.Scripts
{
	[Serializable]
	internal class BlockData
	{
		// we store only block type
		public Block.BlockType[,,] Matrix;

		public BlockData() { }

		public BlockData(Block[,,] b)
		{
			Matrix = new Block.BlockType[World.ChunkSize, World.ChunkSize, World.ChunkSize];
			for (int z = 0; z < World.ChunkSize; z++)
				for (int y = 0; y < World.ChunkSize; y++)
					for (int x = 0; x < World.ChunkSize; x++)
						Matrix[x, y, z] = b[x, y, z].Type;
		}
	}

	public class Chunk
	{
		public enum ChunkStatus { NotInitialized, Created, NeedToBeRedrawn, Keep }

		public Material CubeMaterial;
		public Material FluidMaterial;
		public GameObject ChunkObject;
		public GameObject FluidObject;
		public Block[,,] Blocks;
		public ChunkMB MonoBehavior;
		public UVScroller TextureScroller;
		public bool Changed = false;
		public ChunkStatus Status; // status of the current chunk

		// caves should be more erratic so has to be a higher number
		const float CaveProbability = 0.43f;
		const float CaveSmooth = 0.09f;
		const int CaveOctaves = 3; // reduced a bit to lower workload but not to much to maintain randomness
		const int WaterLeverl = 65; // inclusive

		// shiny diamonds!
		const float DiamondProbability = 0.38f; // this is not percentage chance because we are using Perlin function
		const float DiamondSmooth = 0.06f;
		const int DiamondOctaves = 3;
		const int DiamondMaxHeight = 50;

		// red stones
		const float RedstoneProbability = 0.41f;
		const float RedstoneSmooth = 0.06f;
		const int RedstoneOctaves = 3;
		const int RedstoneMaxHeight = 30;

		// woodbase
		// BUG: these values are very counterintuitive and at some point needs to be converted to percentage values
		const float WoodbaseProbability = 0.36f;
		const float WoodbaseSmooth = 0.4f;
		const int WoodbaseOctaves = 2;

		BlockData _blockData;
		bool treesCreated = false;
		
		public Chunk(Vector3 position, Material chunkMaterial, Material transparentMaterial, int chunkKey)
		{
			ChunkObject = new GameObject(chunkKey.ToString());
			ChunkObject.transform.position = position;
			CubeMaterial = chunkMaterial;

			FluidObject = new GameObject(chunkKey + "_fluid");
			FluidObject.transform.position = position;
			FluidMaterial = transparentMaterial;

			MonoBehavior = ChunkObject.AddComponent<ChunkMB>();
			MonoBehavior.SetOwner(this);

			// BUG: This is extremely slow
			//TextureScroller = FluidObject.AddComponent<UVScroller>();

			BuildChunk();

			// BUG: It doesn't really work as intended 
			// For some reason recreated chunks lose their transparency
			InformSurroundingChunks(chunkKey);
		}
		
		public void UpdateChunk()
		{
			for (var z = 0; z < World.ChunkSize; z++)
				for (var y = 0; y < World.ChunkSize; y++)
					for (var x = 0; x < World.ChunkSize; x++)
						if (Blocks[x, y, z].Type == Block.BlockType.Sand)
							MonoBehavior.StartCoroutine(MonoBehavior.Drop(
								Blocks[x, y, z], 
								Block.BlockType.Sand));
		}

		void BuildChunk()
		{
			bool dataFromFile = Load();

			Blocks = new Block[World.ChunkSize, World.ChunkSize, World.ChunkSize];

			for (var z = 0; z < World.ChunkSize; z++)
				for (var y = 0; y < World.ChunkSize; y++)
					for (var x = 0; x < World.ChunkSize; x++)
					{
						var pos = new Vector3(x, y, z);

						// taking into consideration the noise generator
						int worldX = (int) (x + ChunkObject.transform.position.x);
						int worldY = (int) (y + ChunkObject.transform.position.y);
						int worldZ = (int) (z + ChunkObject.transform.position.z);

						if (dataFromFile)
						{
							Blocks[x, y, z] = new Block(_blockData.Matrix[x, y, z], pos, ChunkObject.gameObject, this);
							continue;
						}
						
						Block.BlockType type = DetermineType(worldX, worldY, worldZ);
						GameObject gameObject = type == Block.BlockType.Water 
							? FluidObject.gameObject 
							: ChunkObject.gameObject;

						Blocks[x, y, z] = new Block(type, pos, gameObject, this);
					}

			// chunk just has been created and it is ready to be drawn
			Status = ChunkStatus.NotInitialized;
		}

		void InformSurroundingChunks(int chunkKey)
		{
			// BUG: In future I should encapsulate key arithmetic logic and move it somewhere else

			// front
			SetChunkToBeDrawn(chunkKey + World.ChunkSize);

			// back
			SetChunkToBeDrawn(chunkKey - World.ChunkSize);

			// up
			SetChunkToBeDrawn(chunkKey + World.ChunkSize * 1000);

			// down
			SetChunkToBeDrawn(chunkKey - World.ChunkSize * 1000);

			// left
			SetChunkToBeDrawn(chunkKey + World.ChunkSize * 1000000);

			// right
			SetChunkToBeDrawn(chunkKey - World.ChunkSize * 1000000);

			// BUG: the above does not take into consideration the edge scenario in the middle of the coordinate system
		}

		void SetChunkToBeDrawn(int targetChunkKey)
		{
			Chunk c;
			World.Chunks.TryGetValue(targetChunkKey, out c);

			if (c != null)
				c.Status = ChunkStatus.NeedToBeRedrawn;
		}

		Block.BlockType DetermineType(int worldX, int worldY, int worldZ)
		{
			Block.BlockType type;

			if (worldY <= Utils.GenerateBedrockHeight(worldX, worldZ))
				type = Block.BlockType.Bedrock;
			else if (worldY <= Utils.GenerateStoneHeight(worldX, worldZ))
			{
				if (Utils.FractalFunc(worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability && worldY < DiamondMaxHeight)
					type = Block.BlockType.Diamond;
				else if (Utils.FractalFunc(worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability && worldY < RedstoneMaxHeight)
					type = Block.BlockType.Redstone;
				else
					type = Block.BlockType.Stone;
			}
			else if (worldY == Utils.GenerateHeight(worldX, worldZ))
				type = Utils.FractalFunc(worldX, worldY, worldZ, WoodbaseSmooth, WoodbaseOctaves) < WoodbaseProbability
					? Block.BlockType.Woodbase
					: Block.BlockType.Grass;
			else if (worldY <= Utils.GenerateHeight(worldX, worldZ))
				type = Block.BlockType.Dirt;
			else if (worldY <= WaterLeverl)
				type = Block.BlockType.Water;
			else
				type = Block.BlockType.Air;

			// generate caves
			if (type != Block.BlockType.Water && Utils.FractalFunc(worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
				type = Block.BlockType.Air;

			return type;
		}

		/// <summary>
		/// Destroys Meshes and Colliders
		/// </summary>
		public void Clean()
		{
			// we cannot use normal destroy because it may wait to the next update loop or something which will break the code
			UnityEngine.Object.DestroyImmediate(ChunkObject.GetComponent<MeshFilter>());
			UnityEngine.Object.DestroyImmediate(ChunkObject.GetComponent<MeshRenderer>());
			UnityEngine.Object.DestroyImmediate(ChunkObject.GetComponent<Collider>());
			UnityEngine.Object.DestroyImmediate(FluidObject.GetComponent<MeshFilter>());
			UnityEngine.Object.DestroyImmediate(FluidObject.GetComponent<MeshRenderer>());
		}

		public void CreateMesh()
		{
			if (!treesCreated)
				CreateTrees();

			for (var z = 0; z < World.ChunkSize; z++)
				for (var y = 0; y < World.ChunkSize; y++)
					for (var x = 0; x < World.ChunkSize; x++)
						Blocks[x, y, z].CreateQuads();

			CombineQuads(ChunkObject.gameObject, CubeMaterial);

			// adding collision
			var collider = ChunkObject.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
			collider.sharedMesh = ChunkObject.transform.GetComponent<MeshFilter>().mesh;

			CombineQuads(FluidObject.gameObject, FluidMaterial);
			Status = ChunkStatus.Created;
		}

		void CreateTrees()
		{
			for (int z = 0; z < World.ChunkSize; z++)
				for (int y = 0; y < World.ChunkSize; y++)
					for (int x = 0; x < World.ChunkSize; x++)
					{
						BuildTrees(Blocks[x, y, z], x, y, z);
					}

			treesCreated = true;
		}

		// BUG: Also another check need to be done to prevent trees from spawning too close to each other
		// And just for the record some trees are generated without leaves
		void BuildTrees(Block trunk, int x, int y, int z)
		{
			if (trunk.Type != Block.BlockType.Woodbase) return;

			Block t = trunk.GetBlock(x, y + 1, z);
			if (t != null)
			{
				t.Type = Block.BlockType.Wood;
				Block t1 = t.GetBlock(x, y + 2, z);
				if (t1 != null)
				{
					t1.Type = Block.BlockType.Wood;

					for (int i = -1; i <= 1; i++)
						for (int j = -1; j <= 1; j++)
							for (int k = 3; k <= 4; k++)
							{
								Block t2 = trunk.GetBlock(x + i, y + k, z + j);

								if (t2 != null)
								{
									t2.Type = Block.BlockType.Leaves;
								}
								else return;
							}
					Block t3 = t1.GetBlock(x, y + 5, z);
					if (t3 != null)
					{
						t3.Type = Block.BlockType.Leaves;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj">Any game object that have all of the quads attached to it</param>
		/// <param name="mat"></param>
		void CombineQuads(GameObject obj, Material mat)
		{
			//1. Combine all children meshes
			var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
			var combine = new CombineInstance[meshFilters.Length];
			var i = 0;
			while (i < meshFilters.Length)
			{
				combine[i].mesh = meshFilters[i].sharedMesh;
				combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
				i++;
			}

			//2. Create a new mesh on the parent object
			var mf = (MeshFilter)obj.gameObject.AddComponent(typeof(MeshFilter));
			mf.mesh = new Mesh();

			//3. Add combined meshes on children as the parent's mesh
			mf.mesh.CombineMeshes(combine);

			//4. Create a renderer for the parent
			var renderer = obj.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
			renderer.material = mat;

			//5. Delete all uncombined children
			foreach (Transform quad in ChunkObject.transform)
				UnityEngine.Object.Destroy(quad.gameObject);
		}

		bool Load()
		{
			string chunkFile = World.BuildChunkFileName(ChunkObject.transform.position);
			if (!File.Exists(chunkFile)) return false;

			var bf = new BinaryFormatter();
			FileStream file = File.Open(chunkFile, FileMode.Open);
			_blockData = new BlockData();
			_blockData = (BlockData) bf.Deserialize(file);
			file.Close();

			// Debug.Log("Loading chunk from file: " + chunkFile);
			return true;
		}

		public void Save()
		{
			string chunkFile = World.BuildChunkFileName(ChunkObject.transform.position);

			if (!File.Exists(chunkFile))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));
			}

			var bf = new BinaryFormatter();
			FileStream file = File.Open(chunkFile, FileMode.OpenOrCreate);
			_blockData = new BlockData(Blocks);
			bf.Serialize(file, _blockData);
			file.Close();
			//Debug.Log("Saving chunk from file: " + chunkFile);
		}
	}
}